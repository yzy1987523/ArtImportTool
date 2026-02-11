using ArtAssetManager.Core.Database;
using ArtAssetManager.Core.Models;
using ArtAssetManager.Core.Repositories;
using ArtAssetManager.Core.Services;
using Xunit;
using Xunit.Abstractions;

namespace ArtAssetManager.Tests;

public class TagSystemTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _testDbPath;
    private readonly string _connectionString;
    private readonly TagService _tagService;
    private readonly AssetService _assetService;

    public TagSystemTests(ITestOutputHelper output)
    {
        _output = output;
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_tag_{Guid.NewGuid()}.db");
        _connectionString = $"Data Source={_testDbPath}";
        
        // 初始化数据库
        var initializer = new DatabaseInitializer(_testDbPath);
        initializer.InitializeAsync().Wait();
        
        _tagService = new TagService(_connectionString);
        _assetService = new AssetService(
            new AssetRepository(_connectionString),
            new FileHashCalculator(),
            new AssetMetadataExtractor()
        );
        
        _output.WriteLine($"✓ 数据库Schema创建成功");
    }

    public void Dispose()
    {
        // 强制垃圾回收以释放数据库连接
        GC.Collect();
        GC.WaitForPendingFinalizers();
        
        // 尝试删除测试数据库文件
        try
        {
            if (File.Exists(_testDbPath))
            {
                // 等待一小段时间确保连接完全释放
                System.Threading.Thread.Sleep(100);
                File.Delete(_testDbPath);
            }
        }
        catch (IOException)
        {
            // 忽略文件锁定错误，测试文件会在临时目录中自动清理
        }
    }

    [Fact]
    public async Task Test01_CreateTag_ShouldSucceed()
    {
        // Arrange & Act
        var tagId = await _tagService.CreateTagAsync("test_tag", "test", "测试标签", "#FF0000", 1);
        
        // Assert
        Assert.True(tagId > 0);
        _output.WriteLine($"✓ 标签创建成功: ID={tagId}");
    }

    [Fact]
    public async Task Test02_CreateDuplicateTag_ShouldThrowException()
    {
        // Arrange
        await _tagService.CreateTagAsync("duplicate_tag", "test");
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _tagService.CreateTagAsync("duplicate_tag", "test");
        });
        
        _output.WriteLine("✓ 重复标签检测正常");
    }

    [Fact]
    public async Task Test03_GetAllTags_ShouldReturnDefaultTags()
    {
        // Act
        var tags = await _tagService.GetAllTagsAsync();
        
        // Assert
        Assert.True(tags.Count >= 8); // 至少包含8个默认标签
        Assert.Contains(tags, t => t.Name == "org");
        Assert.Contains(tags, t => t.Name == "style_cartoon");
        Assert.Contains(tags, t => t.Name == "type_ui");
        
        _output.WriteLine($"✓ 获取所有标签成功: {tags.Count}个标签");
    }

    [Fact]
    public async Task Test04_GetTagsByCategory_ShouldReturnCorrectTags()
    {
        // Act
        var styleTags = await _tagService.GetTagsByCategoryAsync("style");
        var typeTags = await _tagService.GetTagsByCategoryAsync("type");
        
        // Assert
        Assert.True(styleTags.Count >= 3); // style_cartoon, style_realistic, style_pixel
        Assert.True(typeTags.Count >= 4); // type_ui, type_character, type_scene, type_audio
        Assert.All(styleTags, tag => Assert.Equal("style", tag.Category));
        Assert.All(typeTags, tag => Assert.Equal("type", tag.Category));
        
        _output.WriteLine($"✓ 按分类获取标签成功: style={styleTags.Count}, type={typeTags.Count}");
    }

    [Fact]
    public async Task Test05_AddTagToAsset_ShouldSucceed()
    {
        // Arrange - 创建测试资源
        var testImagePath = CreateTestImage();
        var importResult = await _assetService.ImportAssetAsync(testImagePath);
        var assetId = importResult.AssetId;
        
        // 获取org标签
        var allTags = await _tagService.GetAllTagsAsync();
        var orgTag = allTags.First(t => t.Name == "org");
        
        // Act
        var result = await _tagService.AddTagToAssetAsync(assetId, orgTag.Id);
        
        // Assert
        Assert.True(result);
        
        var assetTags = await _tagService.GetAssetTagsAsync(assetId);
        Assert.Single(assetTags);
        Assert.Equal("org", assetTags[0].Name);
        
        _output.WriteLine($"✓ 为资源添加标签成功");
    }

    [Fact]
    public async Task Test06_AddMultipleTagsToAsset_ShouldSucceed()
    {
        // Arrange - 创建测试资源
        var testImagePath = CreateTestImage();
        var importResult = await _assetService.ImportAssetAsync(testImagePath);
        var assetId = importResult.AssetId;
        
        // 获取多个标签
        var allTags = await _tagService.GetAllTagsAsync();
        var orgTag = allTags.First(t => t.Name == "org");
        var styleTag = allTags.First(t => t.Name == "style_cartoon");
        var typeTag = allTags.First(t => t.Name == "type_ui");
        
        // Act
        var count = await _tagService.AddTagsToAssetAsync(assetId, new List<int> { orgTag.Id, styleTag.Id, typeTag.Id });
        
        // Assert
        Assert.Equal(3, count);
        
        var assetTags = await _tagService.GetAssetTagsAsync(assetId);
        Assert.Equal(3, assetTags.Count);
        Assert.Contains(assetTags, t => t.Name == "org");
        Assert.Contains(assetTags, t => t.Name == "style_cartoon");
        Assert.Contains(assetTags, t => t.Name == "type_ui");
        
        _output.WriteLine($"✓ 批量添加标签成功: {count}个标签");
    }

    [Fact]
    public async Task Test07_RemoveTagFromAsset_ShouldSucceed()
    {
        // Arrange - 创建测试资源并添加标签
        var testImagePath = CreateTestImage();
        var importResult = await _assetService.ImportAssetAsync(testImagePath);
        var assetId = importResult.AssetId;
        
        var allTags = await _tagService.GetAllTagsAsync();
        var orgTag = allTags.First(t => t.Name == "org");
        await _tagService.AddTagToAssetAsync(assetId, orgTag.Id);
        
        // Act
        var result = await _tagService.RemoveTagFromAssetAsync(assetId, orgTag.Id);
        
        // Assert
        Assert.True(result);
        
        var assetTags = await _tagService.GetAssetTagsAsync(assetId);
        Assert.Empty(assetTags);
        
        _output.WriteLine($"✓ 移除标签成功");
    }

    [Fact]
    public async Task Test08_GetAssetsByTag_ShouldReturnCorrectAssets()
    {
        // Arrange - 创建3个资源，为其中2个添加org标签
        var asset1Path = CreateTestImage();
        var asset2Path = CreateTestImage();
        var asset3Path = CreateTestImage();
        
        var asset1Result = await _assetService.ImportAssetAsync(asset1Path);
        var asset2Result = await _assetService.ImportAssetAsync(asset2Path);
        var asset3Result = await _assetService.ImportAssetAsync(asset3Path);
        
        var asset1Id = asset1Result.AssetId;
        var asset2Id = asset2Result.AssetId;
        var asset3Id = asset3Result.AssetId;
        
        var allTags = await _tagService.GetAllTagsAsync();
        var orgTag = allTags.First(t => t.Name == "org");
        
        await _tagService.AddTagToAssetAsync(asset1Id, orgTag.Id);
        await _tagService.AddTagToAssetAsync(asset2Id, orgTag.Id);
        
        // Act
        var assets = await _tagService.GetAssetsByTagAsync(orgTag.Id);
        
        // Assert
        Assert.Equal(2, assets.Count);
        Assert.Contains(assets, a => a.Id == asset1Id);
        Assert.Contains(assets, a => a.Id == asset2Id);
        Assert.DoesNotContain(assets, a => a.Id == asset3Id);
        
        _output.WriteLine($"✓ 按标签查询资源成功: {assets.Count}个资源");
    }

    [Fact]
    public async Task Test09_GetAssetsByMultipleTags_ShouldReturnCorrectAssets()
    {
        // Arrange - 创建3个资源，设置不同的标签组合
        var asset1Path = CreateTestImage();
        var asset2Path = CreateTestImage();
        var asset3Path = CreateTestImage();
        
        var asset1Result = await _assetService.ImportAssetAsync(asset1Path);
        var asset2Result = await _assetService.ImportAssetAsync(asset2Path);
        var asset3Result = await _assetService.ImportAssetAsync(asset3Path);
        
        var asset1Id = asset1Result.AssetId;
        var asset2Id = asset2Result.AssetId;
        var asset3Id = asset3Result.AssetId;
        
        var allTags = await _tagService.GetAllTagsAsync();
        var orgTag = allTags.First(t => t.Name == "org");
        var styleTag = allTags.First(t => t.Name == "style_cartoon");
        var typeTag = allTags.First(t => t.Name == "type_ui");
        
        // asset1: org + style_cartoon + type_ui
        await _tagService.AddTagsToAssetAsync(asset1Id, new List<int> { orgTag.Id, styleTag.Id, typeTag.Id });
        
        // asset2: org + style_cartoon
        await _tagService.AddTagsToAssetAsync(asset2Id, new List<int> { orgTag.Id, styleTag.Id });
        
        // asset3: org
        await _tagService.AddTagToAssetAsync(asset3Id, orgTag.Id);
        
        // Act - 查询同时包含org和style_cartoon的资源
        var assets = await _tagService.GetAssetsByTagsAsync(new List<int> { orgTag.Id, styleTag.Id });
        
        // Assert
        Assert.Equal(2, assets.Count);
        Assert.Contains(assets, a => a.Id == asset1Id);
        Assert.Contains(assets, a => a.Id == asset2Id);
        Assert.DoesNotContain(assets, a => a.Id == asset3Id);
        
        // Act - 查询同时包含org、style_cartoon和type_ui的资源
        var assetsWithAllTags = await _tagService.GetAssetsByTagsAsync(new List<int> { orgTag.Id, styleTag.Id, typeTag.Id });
        
        // Assert
        Assert.Single(assetsWithAllTags);
        Assert.Equal(asset1Id, assetsWithAllTags[0].Id);
        
        _output.WriteLine($"✓ 多标签组合查询成功: 2标签={assets.Count}个, 3标签={assetsWithAllTags.Count}个");
    }

    [Fact]
    public async Task Test10_BatchOperations_Performance()
    {
        // Arrange - 创建10个资源
        var assetIds = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            var testImagePath = CreateTestImage();
            var importResult = await _assetService.ImportAssetAsync(testImagePath);
            assetIds.Add(importResult.AssetId);
        }
        
        var allTags = await _tagService.GetAllTagsAsync();
        var orgTag = allTags.First(t => t.Name == "org");
        var styleTag = allTags.First(t => t.Name == "style_cartoon");
        
        // Act - 批量添加标签
        var sw = System.Diagnostics.Stopwatch.StartNew();
        foreach (var assetId in assetIds)
        {
            await _tagService.AddTagsToAssetAsync(assetId, new List<int> { orgTag.Id, styleTag.Id });
        }
        sw.Stop();
        
        // Assert
        Assert.True(sw.ElapsedMilliseconds < 5000); // 应该在5秒内完成
        
        // 验证所有资源都有标签
        foreach (var assetId in assetIds)
        {
            var tags = await _tagService.GetAssetTagsAsync(assetId);
            Assert.Equal(2, tags.Count);
        }
        
        _output.WriteLine($"✓ 批量操作性能测试通过: 10个资源添加标签耗时 {sw.ElapsedMilliseconds}ms");
    }

    private string CreateTestImage()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"test_tag_{Guid.NewGuid()}.png");
        
        // 使用随机数让每个图片都不同
        var random = new Random();
        var randomColor = System.Drawing.Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));
        
        using (var bitmap = new System.Drawing.Bitmap(100, 100))
        using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
        {
            graphics.Clear(randomColor); // 使用随机颜色
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.Black, 2))
            {
                graphics.DrawRectangle(pen, 10, 10, 80, 80);
            }
            bitmap.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
        }
        
        return tempPath;
    }
}
