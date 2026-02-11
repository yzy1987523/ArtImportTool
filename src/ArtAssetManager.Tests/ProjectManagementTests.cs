using ArtAssetManager.Core.Database;
using ArtAssetManager.Core.Repositories;
using ArtAssetManager.Core.Services;
using Xunit;
using Xunit.Abstractions;

namespace ArtAssetManager.Tests;

public class ProjectManagementTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _testDbPath;
    private readonly string _connectionString;
    private readonly ProjectService _projectService;
    private readonly AssetService _assetService;

    public ProjectManagementTests(ITestOutputHelper output)
    {
        _output = output;
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_project_{Guid.NewGuid()}.db");
        _connectionString = $"Data Source={_testDbPath}";
        
        // 初始化数据库
        var initializer = new DatabaseInitializer(_testDbPath);
        initializer.InitializeAsync().Wait();
        
        _projectService = new ProjectService(_connectionString);
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
                System.Threading.Thread.Sleep(100);
                File.Delete(_testDbPath);
            }
        }
        catch (IOException)
        {
            // 忽略文件锁定错误
        }
    }

    [Fact]
    public async Task Test01_CreateProject_ShouldSucceed()
    {
        // Arrange & Act
        var projectId = await _projectService.CreateProjectAsync("TestProject", "Assets/TestProject", "测试项目");
        
        // Assert
        Assert.False(string.IsNullOrEmpty(projectId));
        
        var project = await _projectService.GetProjectAsync(projectId);
        Assert.NotNull(project);
        Assert.Equal("TestProject", project.Name);
        Assert.Equal("Assets/TestProject", project.UnityPath);
        Assert.Equal("测试项目", project.Description);
        
        _output.WriteLine($"✓ 项目创建成功: ID={projectId}");
    }

    [Fact]
    public async Task Test02_CreateDuplicateUnityPath_ShouldThrowException()
    {
        // Arrange
        await _projectService.CreateProjectAsync("Project1", "Assets/Shared");
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _projectService.CreateProjectAsync("Project2", "Assets/Shared");
        });
        
        _output.WriteLine("✓ Unity路径重复检测正常");
    }

    [Fact]
    public async Task Test03_GetAllProjects_ShouldReturnAllProjects()
    {
        // Arrange
        await _projectService.CreateProjectAsync("Project1", "Assets/Project1");
        await _projectService.CreateProjectAsync("Project2", "Assets/Project2");
        await _projectService.CreateProjectAsync("Project3", "Assets/Project3");
        
        // Act
        var projects = await _projectService.GetAllProjectsAsync();
        
        // Assert
        Assert.Equal(3, projects.Count);
        Assert.Contains(projects, p => p.Name == "Project1");
        Assert.Contains(projects, p => p.Name == "Project2");
        Assert.Contains(projects, p => p.Name == "Project3");
        
        _output.WriteLine($"✓ 获取所有项目成功: {projects.Count}个项目");
    }

    [Fact]
    public async Task Test04_AddAssetToProject_ShouldSucceed()
    {
        // Arrange - 创建项目和资源
        var projectId = await _projectService.CreateProjectAsync("TestProject", "Assets/TestProject");
        var testImagePath = CreateTestImage();
        var importResult = await _assetService.ImportAssetAsync(testImagePath);
        var assetId = importResult.AssetId;
        
        // Act
        var result = await _projectService.AddAssetToProjectAsync(projectId, assetId, "test.png", "Assets/TestProject/test.png");
        
        // Assert
        Assert.True(result);
        
        var assets = await _projectService.GetProjectAssetsAsync(projectId);
        Assert.Single(assets);
        Assert.Equal(assetId, assets[0].Id);
        
        _output.WriteLine($"✓ 为项目添加资源成功");
    }

    [Fact]
    public async Task Test05_AddMultipleAssetsToProject_ShouldSucceed()
    {
        // Arrange - 创建项目和5个资源
        var projectId = await _projectService.CreateProjectAsync("TestProject", "Assets/TestProject");
        var assetIds = new List<string>();
        
        for (int i = 0; i < 5; i++)
        {
            var testImagePath = CreateTestImage();
            var importResult = await _assetService.ImportAssetAsync(testImagePath);
            assetIds.Add(importResult.AssetId);
        }
        
        // Act - 批量添加资源
        var assetsToAdd = assetIds.Select((id, index) => (
            AssetId: id,
            ImportName: $"asset{index}.png",
            ImportPath: $"Assets/TestProject/asset{index}.png",
            IsOriginal: true
        )).ToList();
        
        var count = await _projectService.AddAssetsToProjectAsync(projectId, assetsToAdd);
        
        // Assert
        Assert.Equal(5, count);
        
        var assets = await _projectService.GetProjectAssetsAsync(projectId);
        Assert.Equal(5, assets.Count);
        
        _output.WriteLine($"✓ 批量添加资源成功: {count}个资源");
    }

    [Fact]
    public async Task Test06_RemoveAssetFromProject_ShouldSucceed()
    {
        // Arrange - 创建项目、资源并关联
        var projectId = await _projectService.CreateProjectAsync("TestProject", "Assets/TestProject");
        var testImagePath = CreateTestImage();
        var importResult = await _assetService.ImportAssetAsync(testImagePath);
        var assetId = importResult.AssetId;
        
        await _projectService.AddAssetToProjectAsync(projectId, assetId, "test.png", "Assets/TestProject/test.png");
        
        // Act
        var result = await _projectService.RemoveAssetFromProjectAsync(projectId, assetId);
        
        // Assert
        Assert.True(result);
        
        var assets = await _projectService.GetProjectAssetsAsync(projectId);
        Assert.Empty(assets);
        
        _output.WriteLine($"✓ 从项目移除资源成功");
    }

    [Fact]
    public async Task Test07_GetProjectAssets_ShouldReturnCorrectAssets()
    {
        // Arrange - 创建3个项目，每个项目关联不同的资源
        var project1Id = await _projectService.CreateProjectAsync("Project1", "Assets/Project1");
        var project2Id = await _projectService.CreateProjectAsync("Project2", "Assets/Project2");
        var project3Id = await _projectService.CreateProjectAsync("Project3", "Assets/Project3");
        
        // 创建6个资源
        var assetIds = new List<string>();
        for (int i = 0; i < 6; i++)
        {
            var testImagePath = CreateTestImage();
            var importResult = await _assetService.ImportAssetAsync(testImagePath);
            assetIds.Add(importResult.AssetId);
        }
        
        // Project1: asset0, asset1
        await _projectService.AddAssetToProjectAsync(project1Id, assetIds[0], "asset0.png", "Assets/Project1/asset0.png");
        await _projectService.AddAssetToProjectAsync(project1Id, assetIds[1], "asset1.png", "Assets/Project1/asset1.png");
        
        // Project2: asset2, asset3, asset4
        await _projectService.AddAssetToProjectAsync(project2Id, assetIds[2], "asset2.png", "Assets/Project2/asset2.png");
        await _projectService.AddAssetToProjectAsync(project2Id, assetIds[3], "asset3.png", "Assets/Project2/asset3.png");
        await _projectService.AddAssetToProjectAsync(project2Id, assetIds[4], "asset4.png", "Assets/Project2/asset4.png");
        
        // Project3: asset5
        await _projectService.AddAssetToProjectAsync(project3Id, assetIds[5], "asset5.png", "Assets/Project3/asset5.png");
        
        // Act
        var project1Assets = await _projectService.GetProjectAssetsAsync(project1Id);
        var project2Assets = await _projectService.GetProjectAssetsAsync(project2Id);
        var project3Assets = await _projectService.GetProjectAssetsAsync(project3Id);
        
        // Assert
        Assert.Equal(2, project1Assets.Count);
        Assert.Equal(3, project2Assets.Count);
        Assert.Single(project3Assets);
        
        Assert.Contains(project1Assets, a => a.Id == assetIds[0]);
        Assert.Contains(project1Assets, a => a.Id == assetIds[1]);
        Assert.Contains(project2Assets, a => a.Id == assetIds[2]);
        Assert.Contains(project2Assets, a => a.Id == assetIds[3]);
        Assert.Contains(project2Assets, a => a.Id == assetIds[4]);
        Assert.Equal(assetIds[5], project3Assets[0].Id);
        
        _output.WriteLine($"✓ 查询项目资源成功: Project1={project1Assets.Count}, Project2={project2Assets.Count}, Project3={project3Assets.Count}");
    }

    [Fact]
    public async Task Test08_GetAssetProjects_ShouldReturnCorrectProjects()
    {
        // Arrange - 创建3个项目和1个资源
        var project1Id = await _projectService.CreateProjectAsync("Project1", "Assets/Project1");
        var project2Id = await _projectService.CreateProjectAsync("Project2", "Assets/Project2");
        var project3Id = await _projectService.CreateProjectAsync("Project3", "Assets/Project3");
        
        var testImagePath = CreateTestImage();
        var importResult = await _assetService.ImportAssetAsync(testImagePath);
        var assetId = importResult.AssetId;
        
        // 将同一个资源关联到Project1和Project2
        await _projectService.AddAssetToProjectAsync(project1Id, assetId, "shared.png", "Assets/Project1/shared.png");
        await _projectService.AddAssetToProjectAsync(project2Id, assetId, "shared.png", "Assets/Project2/shared.png");
        
        // Act
        var projects = await _projectService.GetAssetProjectsAsync(assetId);
        
        // Assert
        Assert.Equal(2, projects.Count);
        Assert.Contains(projects, p => p.Id == project1Id);
        Assert.Contains(projects, p => p.Id == project2Id);
        Assert.DoesNotContain(projects, p => p.Id == project3Id);
        
        _output.WriteLine($"✓ 查询资源所属项目成功: {projects.Count}个项目");
    }

    [Fact]
    public async Task Test09_GetProjectAssetCount_ShouldReturnCorrectCount()
    {
        // Arrange - 创建项目和5个资源
        var projectId = await _projectService.CreateProjectAsync("TestProject", "Assets/TestProject");
        
        for (int i = 0; i < 5; i++)
        {
            var testImagePath = CreateTestImage();
            var importResult = await _assetService.ImportAssetAsync(testImagePath);
            await _projectService.AddAssetToProjectAsync(projectId, importResult.AssetId, $"asset{i}.png", $"Assets/TestProject/asset{i}.png");
        }
        
        // Act
        var count = await _projectService.GetProjectAssetCountAsync(projectId);
        
        // Assert
        Assert.Equal(5, count);
        
        _output.WriteLine($"✓ 获取项目资源数量成功: {count}个资源");
    }

    [Fact]
    public async Task Test10_CompleteWorkflow_ShouldSucceed()
    {
        // 完整工作流测试：创建3个项目，每个项目关联5个资源
        
        // 创建3个项目
        var project1Id = await _projectService.CreateProjectAsync("Project1", "Assets/Project1", "第一个项目");
        var project2Id = await _projectService.CreateProjectAsync("Project2", "Assets/Project2", "第二个项目");
        var project3Id = await _projectService.CreateProjectAsync("Project3", "Assets/Project3", "第三个项目");
        
        // 为每个项目创建并关联5个资源
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        foreach (var projectId in new[] { project1Id, project2Id, project3Id })
        {
            for (int i = 0; i < 5; i++)
            {
                var testImagePath = CreateTestImage();
                var importResult = await _assetService.ImportAssetAsync(testImagePath);
                await _projectService.AddAssetToProjectAsync(projectId, importResult.AssetId, $"asset{i}.png", $"Assets/Project/asset{i}.png");
            }
        }
        
        sw.Stop();
        
        // 验证
        var project1Assets = await _projectService.GetProjectAssetsAsync(project1Id);
        var project2Assets = await _projectService.GetProjectAssetsAsync(project2Id);
        var project3Assets = await _projectService.GetProjectAssetsAsync(project3Id);
        
        Assert.Equal(5, project1Assets.Count);
        Assert.Equal(5, project2Assets.Count);
        Assert.Equal(5, project3Assets.Count);
        
        var allProjects = await _projectService.GetAllProjectsAsync();
        Assert.Equal(3, allProjects.Count);
        
        _output.WriteLine($"✓ 完整工作流测试通过: 3个项目，每个5个资源，耗时 {sw.ElapsedMilliseconds}ms");
    }

    private string CreateTestImage()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"test_project_{Guid.NewGuid()}.png");
        
        // 使用随机数让每个图片都不同
        var random = new Random();
        var randomColor = System.Drawing.Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));
        
        using (var bitmap = new System.Drawing.Bitmap(100, 100))
        using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
        {
            graphics.Clear(randomColor);
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.Black, 2))
            {
                graphics.DrawRectangle(pen, 10, 10, 80, 80);
            }
            bitmap.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
        }
        
        return tempPath;
    }
}
