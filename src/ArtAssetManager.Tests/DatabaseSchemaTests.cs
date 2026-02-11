using ArtAssetManager.Core.Database;
using Xunit;

namespace ArtAssetManager.Tests;

/// <summary>
/// 数据库Schema验证测试
/// </summary>
public class DatabaseSchemaTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly DatabaseInitializer _initializer;

    public DatabaseSchemaTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_artassets_{Guid.NewGuid()}.db");
        _initializer = new DatabaseInitializer(_testDbPath);
    }

    [Fact]
    public async Task Test01_DatabaseInitialization_ShouldCreateAllTables()
    {
        // Arrange & Act
        await _initializer.InitializeAsync();

        // Assert
        var isValid = await _initializer.ValidateSchemaAsync();
        Assert.True(isValid, "数据库Schema验证失败");
    }

    [Fact]
    public async Task Test02_DefaultTags_ShouldBeCreated()
    {
        // Arrange
        await _initializer.InitializeAsync();

        // Act
        var counts = await _initializer.GetTableCountsAsync();

        // Assert
        Assert.True(counts["Tags"] >= 8, $"默认Tag数量不足，期望>=8，实际={counts["Tags"]}");
    }

    [Fact]
    public async Task Test03_TestData_ShouldLoadSuccessfully()
    {
        // Arrange
        await _initializer.InitializeAsync();

        // Act
        await _initializer.LoadTestDataAsync();
        var counts = await _initializer.GetTableCountsAsync();

        // Assert
        Assert.True(counts["ArtAssets"] >= 3, $"测试资源数量不足，期望>=3，实际={counts["ArtAssets"]}");
        Assert.True(counts["Projects"] >= 1, $"测试Project数量不足，期望>=1，实际={counts["Projects"]}");
        Assert.True(counts["AssetTags"] >= 6, $"测试资源Tag关联数量不足，期望>=6，实际={counts["AssetTags"]}");
        Assert.True(counts["ProjectAssets"] >= 2, $"测试Project资源关联数量不足，期望>=2，实际={counts["ProjectAssets"]}");
    }

    [Fact]
    public async Task Test04_TableCounts_ShouldMatchExpectations()
    {
        // Arrange
        await _initializer.InitializeAsync();
        await _initializer.LoadTestDataAsync();

        // Act
        var counts = await _initializer.GetTableCountsAsync();

        // Assert
        Console.WriteLine("\n=== 数据库表统计 ===");
        foreach (var (table, count) in counts)
        {
            Console.WriteLine($"{table}: {count} 条记录");
        }

        Assert.All(counts.Values, count => Assert.True(count >= 0, "表记录数不应为负数"));
    }

    public void Dispose()
    {
        // 强制垃圾回收，释放数据库连接
        GC.Collect();
        GC.WaitForPendingFinalizers();
        
        // 尝试删除文件，忽略错误（测试数据库文件会自动清理）
        try
        {
            if (File.Exists(_testDbPath))
            {
                Thread.Sleep(200); // 等待连接完全释放
                File.Delete(_testDbPath);
            }
        }
        catch (IOException)
        {
            // 忽略文件锁定错误，临时文件会被系统自动清理
        }
    }
}
