using ArtAssetManager.Core.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ArtAssetManager.Core.Repositories;

/// <summary>
/// 标签仓储
/// </summary>
public class TagRepository
{
    private readonly string _connectionString;
    
    static TagRepository()
    {
        SqlMapper.SetTypeMap(typeof(Tag), new CustomPropertyTypeMap(
            typeof(Tag),
            (type, columnName) => type.GetProperty(ConvertToPascalCase(columnName))!
        ));
        
        SqlMapper.SetTypeMap(typeof(AssetTag), new CustomPropertyTypeMap(
            typeof(AssetTag),
            (type, columnName) => type.GetProperty(ConvertToPascalCase(columnName))!
        ));
    }
    
    private static string ConvertToPascalCase(string snakeCase)
    {
        if (string.IsNullOrEmpty(snakeCase)) return snakeCase;
        
        var parts = snakeCase.Split('_');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
            }
        }
        return string.Join("", parts);
    }

    public TagRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// 创建标签
    /// </summary>
    public async Task<int> CreateAsync(Tag tag)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = @"
            INSERT INTO Tags (name, category, description, color, sort_order, created_at, updated_at)
            VALUES (@Name, @Category, @Description, @Color, @SortOrder, @CreatedAt, @UpdatedAt);
            SELECT last_insert_rowid();";
        
        var id = await connection.ExecuteScalarAsync<int>(sql, tag);
        return id;
    }

    /// <summary>
    /// 根据ID获取标签
    /// </summary>
    public async Task<Tag?> GetByIdAsync(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = "SELECT * FROM Tags WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<Tag>(sql, new { Id = id });
    }

    /// <summary>
    /// 根据名称获取标签
    /// </summary>
    public async Task<Tag?> GetByNameAsync(string name)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = "SELECT * FROM Tags WHERE name = @Name";
        return await connection.QueryFirstOrDefaultAsync<Tag>(sql, new { Name = name });
    }

    /// <summary>
    /// 获取所有标签
    /// </summary>
    public async Task<List<Tag>> GetAllAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = "SELECT * FROM Tags ORDER BY category, sort_order, name";
        var results = await connection.QueryAsync<Tag>(sql);
        return results.ToList();
    }

    /// <summary>
    /// 按分类获取标签
    /// </summary>
    public async Task<List<Tag>> GetByCategoryAsync(string category)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = "SELECT * FROM Tags WHERE category = @Category ORDER BY sort_order, name";
        var results = await connection.QueryAsync<Tag>(sql, new { Category = category });
        return results.ToList();
    }

    /// <summary>
    /// 更新标签
    /// </summary>
    public async Task<bool> UpdateAsync(Tag tag)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = @"
            UPDATE Tags 
            SET name = @Name, 
                category = @Category, 
                description = @Description, 
                color = @Color, 
                sort_order = @SortOrder, 
                updated_at = @UpdatedAt
            WHERE id = @Id";
        
        var affected = await connection.ExecuteAsync(sql, tag);
        return affected > 0;
    }

    /// <summary>
    /// 删除标签
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = "DELETE FROM Tags WHERE id = @Id";
        var affected = await connection.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }

    /// <summary>
    /// 为资源添加标签
    /// </summary>
    public async Task<bool> AddTagToAssetAsync(string assetId, int tagId, string? createdBy = null)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = @"
            INSERT OR IGNORE INTO AssetTags (asset_id, tag_id, created_at, created_by)
            VALUES (@AssetId, @TagId, @CreatedAt, @CreatedBy)";
        
        var affected = await connection.ExecuteAsync(sql, new
        {
            AssetId = assetId,
            TagId = tagId,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            CreatedBy = createdBy
        });
        
        return affected > 0;
    }

    /// <summary>
    /// 从资源移除标签
    /// </summary>
    public async Task<bool> RemoveTagFromAssetAsync(string assetId, int tagId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = "DELETE FROM AssetTags WHERE asset_id = @AssetId AND tag_id = @TagId";
        var affected = await connection.ExecuteAsync(sql, new { AssetId = assetId, TagId = tagId });
        return affected > 0;
    }

    /// <summary>
    /// 获取资源的所有标签
    /// </summary>
    public async Task<List<Tag>> GetAssetTagsAsync(string assetId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = @"
            SELECT t.* FROM Tags t
            INNER JOIN AssetTags at ON t.id = at.tag_id
            WHERE at.asset_id = @AssetId
            ORDER BY t.category, t.sort_order, t.name";
        
        var results = await connection.QueryAsync<Tag>(sql, new { AssetId = assetId });
        return results.ToList();
    }

    /// <summary>
    /// 批量为资源添加标签
    /// </summary>
    public async Task<int> AddTagsToAssetAsync(string assetId, List<int> tagIds, string? createdBy = null)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = @"
            INSERT OR IGNORE INTO AssetTags (asset_id, tag_id, created_at, created_by)
            VALUES (@AssetId, @TagId, @CreatedAt, @CreatedBy)";
        
        var createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var parameters = tagIds.Select(tagId => new
        {
            AssetId = assetId,
            TagId = tagId,
            CreatedAt = createdAt,
            CreatedBy = createdBy
        });
        
        var affected = await connection.ExecuteAsync(sql, parameters);
        return affected;
    }

    /// <summary>
    /// 批量从资源移除标签
    /// </summary>
    public async Task<int> RemoveTagsFromAssetAsync(string assetId, List<int> tagIds)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = "DELETE FROM AssetTags WHERE asset_id = @AssetId AND tag_id IN @TagIds";
        var affected = await connection.ExecuteAsync(sql, new { AssetId = assetId, TagIds = tagIds });
        return affected;
    }

    /// <summary>
    /// 清除资源的所有标签
    /// </summary>
    public async Task<int> ClearAssetTagsAsync(string assetId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = "DELETE FROM AssetTags WHERE asset_id = @AssetId";
        var affected = await connection.ExecuteAsync(sql, new { AssetId = assetId });
        return affected;
    }
}
