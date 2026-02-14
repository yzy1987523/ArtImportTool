using System.Runtime.Versioning;
using ArtAssetManager.Core.Database;
using ArtAssetManager.Core.Repositories;
using ArtAssetManager.Core.Services;
using Xunit;

namespace ArtAssetManager.Tests;

/// <summary>
/// 资源导入测试
/// </summary>
[SupportedOSPlatform("windows")]
public class AssetImportTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly string _testFilesPath;
    private readonly DatabaseInitializer _initializer;
    private readonly AssetService _assetService;

    public AssetImportTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_artassets_{Guid.NewGuid()}.db");
        _testFilesPath = Path.Combine(Path.GetTempPath(), $"test_files_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testFilesPath);

        _initializer = new DatabaseInitializer(_testDbPath);

        var connectionString = $"Data Source={_testDbPath}";
        var repository = new AssetRepository(connectionString);
        var hashCalculator = new FileHashCalculator();
        var metadataExtractor = new AssetMetadataExtractor();

        _assetService = new AssetService(repository, hashCalculator, metadataExtractor);  
    }

    [Fact]
    public async Task Test01_FileHashCalculator_ShouldCalculateCorrectHash()
    {
        var testFile = CreateTestImageFile("test_image.png", 100, 100);
        var calculator = new FileHashCalculator();

        var hash1 = await calculator.CalculateHashAsync(testFile);
        var hash2 = await calculator.CalculateHashAsync(testFile);

        Assert.NotNull(hash1);
        Assert.NotEmpty(hash1);
        Assert.Equal(64, hash1.Length);
        Assert.Equal(hash1, hash2);

        Console.WriteLine($"✓ 文件哈希计算成功: {hash1.Substring(0, 16)}...");
    }

    [Fact]
    public async Task Test02_AssetMetadataExtractor_ShouldExtractImageMetadata()
    {
        var testFile = CreateTestImageFile("test_image.png", 512, 256);
        var extractor = new AssetMetadataExtractor();

        var metadata = await extractor.ExtractMetadataAsync(testFile);

        Assert.Equal("test_image", metadata.Name);
        Assert.Equal("png", metadata.FileType);
        Assert.Equal(512, metadata.Width);
        Assert.Equal(256, metadata.Height);
        Assert.Null(metadata.Duration);
        Assert.True(metadata.FileSize > 0);

        Console.WriteLine($"✓ 图片元数据提取成功: {metadata.Width}x{metadata.Height}, {metadata.FileSize} bytes");
    }

    [Fact]
    public async Task Test03_AssetImport_ShouldImportNewAsset()
    {
        await _initializer.InitializeAsync();
        var testFile = CreateTestImageFile("character_idle.png", 256, 256);

        var result = await _assetService.ImportAssetAsync(testFile);

        Assert.True(result.IsNew);
        Assert.False(result.IsDuplicate);
        Assert.NotEmpty(result.AssetId);

        Console.WriteLine($"✓ 资源导入成功: ID={result.AssetId}");
    }

    [Fact]
    public async Task Test04_AssetImport_ShouldDetectDuplicate()
    {
        await _initializer.InitializeAsync();
        var testFile = CreateTestImageFile("duplicate_test.png", 128, 128);

        var result1 = await _assetService.ImportAssetAsync(testFile);
        var result2 = await _assetService.ImportAssetAsync(testFile);

        Assert.True(result1.IsNew);
        Assert.False(result1.IsDuplicate);

        Assert.False(result2.IsNew);
        Assert.True(result2.IsDuplicate);
        Assert.Equal(result1.AssetId, result2.AssetId);

        Console.WriteLine($"✓ 去重功能正常: 相同文件被识别为重复");
    }

    [Fact]
    public async Task Test05_BatchImport_ShouldImportMultipleAssets()
    {
        await _initializer.InitializeAsync();
        var testFiles = new List<string>
        {
            CreateTestImageFile("asset_01.png", 100, 100),
            CreateTestImageFile("asset_02.png", 200, 200),
            CreateTestImageFile("asset_03.png", 300, 300),
            CreateTestImageFile("asset_01.png", 100, 100),
        };

        var startTime = DateTime.Now;
        var result = await _assetService.BatchImportAsync(testFiles);
        var duration = (DateTime.Now - startTime).TotalMilliseconds;

        Assert.Equal(4, result.TotalCount);
        Assert.Equal(3, result.SuccessCount);
        Assert.Equal(1, result.DuplicateCount);
        Assert.Equal(0, result.FailureCount);
        Assert.Equal(3, result.SuccessIds.Count);

        Console.WriteLine($"✓ 批量导入成功: {result.SuccessCount}个新资源, {result.DuplicateCount}个重复, 耗时{duration}ms");
    }

    [Fact]
    public async Task Test06_BatchImport_Performance_100Files()
    {
        await _initializer.InitializeAsync();
        var testFiles = new List<string>();

        Console.WriteLine("正在创建100个测试文件...");
        for (int i = 0; i < 100; i++)
        {
            testFiles.Add(CreateTestImageFile($"perf_test_{i:D3}.png", 64, 64));
        }

        Console.WriteLine("开始批量导入...");
        var startTime = DateTime.Now;
        var result = await _assetService.BatchImportAsync(testFiles);
        var duration = (DateTime.Now - startTime).TotalSeconds;

        Assert.Equal(100, result.TotalCount);
        Assert.Equal(100, result.SuccessCount);
        Assert.Equal(0, result.DuplicateCount);
        Assert.True(duration < 30, $"批量导入100张图片应该 < 30秒，实际耗时 {duration:F2} 秒");

        Console.WriteLine($"✓ 性能测试通过: 100张图片导入耗时 {duration:F2}秒 (目标 < 30秒)");
    }

    private string CreateTestImageFile(string fileName, int width, int height)
    {
        var filePath = Path.Combine(_testFilesPath, fileName);

        using var bitmap = new System.Drawing.Bitmap(width, height);
        using var graphics = System.Drawing.Graphics.FromImage(bitmap);

        var random = new Random(fileName.GetHashCode());
        var color = System.Drawing.Color.FromArgb(random.Next(256), random.Next(256), random.Next(256)); 
        graphics.Clear(color);

        using var pen = new System.Drawing.Pen(System.Drawing.Color.White, 2);
        graphics.DrawRectangle(pen, 10, 10, width - 20, height - 20);

        bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
        return filePath;
    }

    public void Dispose()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();

        try
        {
            if (File.Exists(_testDbPath))
            {
                Thread.Sleep(200);
                File.Delete(_testDbPath);
            }
        }
        catch (IOException)
        {
        }

        try
        {
            if (Directory.Exists(_testFilesPath))
            {
                Directory.Delete(_testFilesPath, true);
            }
        }
        catch (IOException)
        {
        }
    }
}
