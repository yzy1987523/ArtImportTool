using ArtAssetManager.Core.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ArtAssetManager.Core.Repositories;

/// <summary>
/// 资源仓储
/// </summary>
public class AssetRepository
{
    private readonly string _connectionString;
    
    static AssetRepository()
    {
        // 配置Dapper列名映射：snake_case -> PascalCase
        SqlMapper.SetTypeMap(typeof(Asset), new CustomPropertyTypeMap(
            typeof(Asset),
            (type, columnName) => type.GetProperty(ConvertToPascalCase(columnName))
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

    public AssetRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// 创建资源
    /// </summary>
    public async Task<string> CreateAsync(Asset asset)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = @"
            INSERT INTO ArtAssets (id, name, file_path, file_type, file_size, file_hash, 
                                   width, height, duration, metadata, created_at, updated_at, is_deleted)
            VALUES (@Id, @Name, @FilePath, @FileType, @FileSize, @FileHash, 
                    @Width, @Height, @Duration, @Metadata, @CreatedAt, @UpdatedAt, @IsDeleted)";
        
        await connection.ExecuteAsync(sql, asset);
        return asset.Id;
    }

    /// <summary>
    /// 根据ID获取资源
    /// </summary>
    public async Task<Asset?> GetByIdAsync(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = "SELECT * FROM ArtAssets WHERE id = @Id AND is_deleted = 0";
        return await connection.QueryFirstOrDefaultAsync<Asset>(sql, new { Id = id });
    }

    /// <summary>
    /// 根据文件哈希获取资源
    /// </summary>
    public async Task<Asset?> GetByFileHashAsync(string fileHash)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = "SELECT * FROM ArtAssets WHERE file_hash = @FileHash AND is_deleted = 0 LIMIT 1";
        return await connection.QueryFirstOrDefaultAsync<Asset>(sql, new { FileHash = fileHash });
    }

    /// <summary>
    /// 批量根据文件哈希获取资源
    /// </summary>
    public async Task<List<Asset>> GetByFileHashesAsync(List<string> fileHashes)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = "SELECT * FROM ArtAssets WHERE file_hash IN @FileHashes AND is_deleted = 0";
        var results = await connection.QueryAsync<Asset>(sql, new { FileHashes = fileHashes });
        return results.ToList();
    }

    /// <summary>
    /// 查询资源列表
    /// </summary>
    public async Task<List<Asset>> QueryAsync(int page = 1, int pageSize = 20)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var offset = (page - 1) * pageSize;
        var sql = @"
            SELECT * FROM ArtAssets 
            WHERE is_deleted = 0 
            ORDER BY created_at DESC 
            LIMIT @PageSize OFFSET @Offset";
        
        var results = await connection.QueryAsync<Asset>(sql, new { PageSize = pageSize, Offset = offset });
        return results.ToList();
    }

    /// <summary>
    /// 获取资源总数
    /// </summary>
    public async Task<int> GetCountAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = "SELECT COUNT(*) FROM ArtAssets WHERE is_deleted = 0";
        return await connection.ExecuteScalarAsync<int>(sql);
    }

    /// <summary>
    /// 更新资源
    /// </summary>
    public async Task<bool> UpdateAsync(Asset asset)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = @"
            UPDATE ArtAssets 
            SET name = @Name, 
                file_path = @FilePath, 
                file_type = @FileType, 
                file_size = @FileSize, 
                file_hash = @FileHash,
                width = @Width, 
                height = @Height, 
                duration = @Duration, 
                metadata = @Metadata, 
                updated_at = @UpdatedAt, 
                is_deleted = @IsDeleted
            WHERE id = @Id";
        
        var affected = await connection.ExecuteAsync(sql, asset);
        return affected > 0;
    }

    /// <summary>
    /// 删除资源（软删除）
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = "UPDATE ArtAssets SET is_deleted = 1, updated_at = @UpdatedAt WHERE id = @Id";
        var affected = await connection.ExecuteAsync(sql, new { Id = id, UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() });
        return affected > 0;
    }
}
