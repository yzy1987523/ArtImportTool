using ArtAssetManager.Core.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ArtAssetManager.Core.Repositories;

/// <summary>
/// 项目仓储
/// </summary>
public class ProjectRepository
{
    private readonly string _connectionString;
    
    static ProjectRepository()
    {
        // 配置Dapper列名映射：snake_case -> PascalCase
        SqlMapper.SetTypeMap(typeof(Project), new CustomPropertyTypeMap(
            typeof(Project),
            (type, columnName) => type.GetProperty(ConvertToPascalCase(columnName))
        ));
        
        SqlMapper.SetTypeMap(typeof(ProjectAsset), new CustomPropertyTypeMap(
            typeof(ProjectAsset),
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

    public ProjectRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// 创建项目
    /// </summary>
    public async Task<string> CreateAsync(Project project)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = @"
            INSERT INTO Projects (id, name, description, unity_path, created_at, updated_at, is_deleted)
            VALUES (@Id, @Name, @Description, @UnityPath, @CreatedAt, @UpdatedAt, @IsDeleted)";
        
        await connection.ExecuteAsync(sql, project);
        return project.Id;
    }

    /// <summary>
    /// 根据ID获取项目
    /// </summary>
    public async Task<Project?> GetByIdAsync(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = "SELECT * FROM Projects WHERE id = @Id AND is_deleted = 0";
        return await connection.QueryFirstOrDefaultAsync<Project>(sql, new { Id = id });
    }

    /// <summary>
    /// 根据Unity路径获取项目
    /// </summary>
    public async Task<Project?> GetByUnityPathAsync(string unityPath)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = "SELECT * FROM Projects WHERE unity_path = @UnityPath AND is_deleted = 0";
        return await connection.QueryFirstOrDefaultAsync<Project>(sql, new { UnityPath = unityPath });
    }

    /// <summary>
    /// 获取所有项目
    /// </summary>
    public async Task<List<Project>> GetAllAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = "SELECT * FROM Projects WHERE is_deleted = 0 ORDER BY created_at DESC";
        var results = await connection.QueryAsync<Project>(sql);
        return results.ToList();
    }

    /// <summary>
    /// 更新项目
    /// </summary>
    public async Task<bool> UpdateAsync(Project project)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = @"
            UPDATE Projects 
            SET name = @Name, 
                description = @Description, 
                unity_path = @UnityPath, 
                updated_at = @UpdatedAt, 
                is_deleted = @IsDeleted
            WHERE id = @Id";
        
        var affected = await connection.ExecuteAsync(sql, project);
        return affected > 0;
    }

    /// <summary>
    /// 删除项目（软删除）
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = "UPDATE Projects SET is_deleted = 1, updated_at = @UpdatedAt WHERE id = @Id";
        var affected = await connection.ExecuteAsync(sql, new { Id = id, UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() });
        return affected > 0;
    }

    /// <summary>
    /// 为项目添加资源
    /// </summary>
    public async Task<bool> AddAssetToProjectAsync(string projectId, string assetId, string importName, string importPath, bool isOriginal = true)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = @"
            INSERT OR IGNORE INTO ProjectAssets (project_id, asset_id, import_name, import_path, is_original, created_at)
            VALUES (@ProjectId, @AssetId, @ImportName, @ImportPath, @IsOriginal, @CreatedAt)";
        
        var affected = await connection.ExecuteAsync(sql, new
        {
            ProjectId = projectId,
            AssetId = assetId,
            ImportName = importName,
            ImportPath = importPath,
            IsOriginal = isOriginal,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });
        
        return affected > 0;
    }

    /// <summary>
    /// 从项目移除资源
    /// </summary>
    public async Task<bool> RemoveAssetFromProjectAsync(string projectId, string assetId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = "DELETE FROM ProjectAssets WHERE project_id = @ProjectId AND asset_id = @AssetId";
        var affected = await connection.ExecuteAsync(sql, new { ProjectId = projectId, AssetId = assetId });
        return affected > 0;
    }

    /// <summary>
    /// 获取项目中的所有资源
    /// </summary>
    public async Task<List<Asset>> GetProjectAssetsAsync(string projectId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = @"
            SELECT a.* FROM ArtAssets a
            INNER JOIN ProjectAssets pa ON a.id = pa.asset_id
            WHERE pa.project_id = @ProjectId AND a.is_deleted = 0
            ORDER BY pa.created_at DESC";
        
        var results = await connection.QueryAsync<Asset>(sql, new { ProjectId = projectId });
        return results.ToList();
    }

    /// <summary>
    /// 获取资源所属的所有项目
    /// </summary>
    public async Task<List<Project>> GetAssetProjectsAsync(string assetId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = @"
            SELECT p.* FROM Projects p
            INNER JOIN ProjectAssets pa ON p.id = pa.project_id
            WHERE pa.asset_id = @AssetId AND p.is_deleted = 0
            ORDER BY pa.created_at DESC";
        
        var results = await connection.QueryAsync<Project>(sql, new { AssetId = assetId });
        return results.ToList();
    }

    /// <summary>
    /// 批量为项目添加资源
    /// </summary>
    public async Task<int> AddAssetsToProjectAsync(string projectId, List<(string AssetId, string ImportName, string ImportPath, bool IsOriginal)> assets)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = @"
            INSERT OR IGNORE INTO ProjectAssets (project_id, asset_id, import_name, import_path, is_original, created_at)
            VALUES (@ProjectId, @AssetId, @ImportName, @ImportPath, @IsOriginal, @CreatedAt)";
        
        var createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var parameters = assets.Select(a => new
        {
            ProjectId = projectId,
            AssetId = a.AssetId,
            ImportName = a.ImportName,
            ImportPath = a.ImportPath,
            IsOriginal = a.IsOriginal,
            CreatedAt = createdAt
        });
        
        var affected = await connection.ExecuteAsync(sql, parameters);
        return affected;
    }

    /// <summary>
    /// 获取项目资源数量
    /// </summary>
    public async Task<int> GetProjectAssetCountAsync(string projectId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = "SELECT COUNT(*) FROM ProjectAssets WHERE project_id = @ProjectId";
        return await connection.ExecuteScalarAsync<int>(sql, new { ProjectId = projectId });
    }
}
