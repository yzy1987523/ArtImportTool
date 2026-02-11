using Microsoft.Data.Sqlite;
using ArtAssetManager.Core.Configuration;

namespace ArtAssetManager.Core.Database;

/// <summary>
/// 数据库初始化器
/// </summary>
public class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(string databasePath)
    {
        _connectionString = $"Data Source={databasePath}";
    }

    /// <summary>
    /// 从配置文件创建初始化器
    /// </summary>
    public static DatabaseInitializer FromConfig(string configPath)
    {
        var config = DatabaseConfig.LoadFromFile(configPath);
        var validation = config.Validate();
        
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Invalid database configuration: {validation.ErrorMessage}");
        }

        return new DatabaseInitializer(config.GetFullDatabasePath());
    }

    /// <summary>
    /// 初始化数据库（创建表和索引）
    /// </summary>
    public async Task InitializeAsync()
    {
        // 从项目根目录查找schema.sql
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var projectRoot = FindProjectRoot(baseDir);
        var schemaPath = Path.Combine(projectRoot, "database", "schema.sql");
        
        if (!File.Exists(schemaPath))
        {
            throw new FileNotFoundException($"Schema file not found: {schemaPath}");
        }
        
        var schemaSql = await File.ReadAllTextAsync(schemaPath);

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = schemaSql;
        await command.ExecuteNonQueryAsync();

        Console.WriteLine("✓ 数据库Schema创建成功");
    }

    /// <summary>
    /// 加载测试数据
    /// </summary>
    public async Task LoadTestDataAsync()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var projectRoot = FindProjectRoot(baseDir);
        var testDataPath = Path.Combine(projectRoot, "database", "test_data.sql");
        
        if (!File.Exists(testDataPath))
        {
            throw new FileNotFoundException($"Test data file not found: {testDataPath}");
        }
        
        var testDataSql = await File.ReadAllTextAsync(testDataPath);

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = testDataSql;
        await command.ExecuteNonQueryAsync();

        Console.WriteLine("✓ 测试数据加载成功");
    }

    /// <summary>
    /// 验证数据库结构
    /// </summary>
    public async Task<bool> ValidateSchemaAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var expectedTables = new[]
        {
            "ArtAssets", "Tags", "AssetTags", "Projects", "ProjectAssets",
            "StyleMigrations", "UnityRoutes", "RouteHistory"
        };

        foreach (var tableName in expectedTables)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'";
            var result = await command.ExecuteScalarAsync();

            if (result == null)
            {
                Console.WriteLine($"✗ 表 {tableName} 不存在");
                return false;
            }
        }

        Console.WriteLine("✓ 所有表验证通过");
        return true;
    }

    /// <summary>
    /// 获取表的行数
    /// </summary>
    public async Task<Dictionary<string, int>> GetTableCountsAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var tables = new[] { "ArtAssets", "Tags", "AssetTags", "Projects", "ProjectAssets" };
        var counts = new Dictionary<string, int>();

        foreach (var table in tables)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM {table}";
            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            counts[table] = count;
        }

        return counts;
    }

    /// <summary>
    /// 查找项目根目录（包含.sln文件的目录）
    /// </summary>
    private string FindProjectRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            if (dir.GetFiles("*.sln").Length > 0)
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not find project root directory");
    }
}
