using System.Runtime.Versioning;
using ArtAssetManager.Core.Database;
using ArtAssetManager.Core.Repositories;
using ArtAssetManager.Core.Services;

namespace ArtAssetManager.Console;

[SupportedOSPlatform("windows")]
class Program
{
    private static string _dbPath = "art_asset_manager.db";
    private static string _connectionString = string.Empty;
    private static AssetService? _assetService;
    private static TagService? _tagService;
    private static ProjectService? _projectService;

    static async Task Main(string[] args)
    {
        System.Console.OutputEncoding = System.Text.Encoding.UTF8;
        System.Console.WriteLine("===========================================");
        System.Console.WriteLine("   艺术资源管理系统 - 控制台验证工具");
        System.Console.WriteLine("===========================================");
        System.Console.WriteLine();

        // 初始化数据库
        await InitializeDatabaseAsync();

        // 主菜单循环
        bool running = true;
        while (running)
        {
            ShowMainMenu();
            var choice = System.Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await ImportAssetMenu();
                        break;
                    case "2":
                        await ViewAssetsMenu();
                        break;
                    case "3":
                        await TagManagementMenu();
                        break;
                    case "4":
                        await ProjectManagementMenu();
                        break;
                    case "5":
                        await ViewDatabaseStats();
                        break;
                    case "0":
                        running = false;
                        System.Console.WriteLine("\n再见！");
                        break;
                    default:
                        System.Console.WriteLine("\n无效选项，请重试。");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\n错误: {ex.Message}");
            }

            if (running)
            {
                System.Console.WriteLine("\n按任意键继续...");
                System.Console.ReadKey();
            }
        }
    }

    static async Task InitializeDatabaseAsync()
    {
        System.Console.WriteLine("正在初始化数据库...");
        
        _connectionString = $"Data Source={_dbPath}";
        var initializer = new DatabaseInitializer(_dbPath);
        await initializer.InitializeAsync();

        _assetService = new AssetService(
            new AssetRepository(_connectionString),
            new FileHashCalculator(),
            new AssetMetadataExtractor()
        );
        _tagService = new TagService(_connectionString);
        _projectService = new ProjectService(_connectionString);

        System.Console.WriteLine($"✓ 数据库初始化完成: {_dbPath}");
        System.Console.WriteLine();
    }

    static void ShowMainMenu()
    {
        System.Console.Clear();
        System.Console.WriteLine("===========================================");
        System.Console.WriteLine("              主菜单");
        System.Console.WriteLine("===========================================");
        System.Console.WriteLine("1. 导入资源");
        System.Console.WriteLine("2. 查看资源");
        System.Console.WriteLine("3. 标签管理");
        System.Console.WriteLine("4. 项目管理");
        System.Console.WriteLine("5. 查看数据库统计");
        System.Console.WriteLine("0. 退出");
        System.Console.WriteLine("===========================================");
        System.Console.Write("请选择: ");
    }

    static async Task ImportAssetMenu()
    {
        System.Console.Clear();
        System.Console.WriteLine("=== 导入资源 ===\n");
        System.Console.Write("请输入文件路径（或拖拽文件到此窗口）: ");
        var filePath = System.Console.ReadLine()?.Trim().Trim('"');

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            System.Console.WriteLine("文件不存在！");
            return;
        }

        System.Console.WriteLine("\n正在导入...");
        var result = await _assetService!.ImportAssetAsync(filePath);

        if (result.IsDuplicate)
        {
            System.Console.WriteLine($"✓ 资源已存在（去重）: {result.AssetId}");
        }
        else
        {
            System.Console.WriteLine($"✓ 资源导入成功！");
            System.Console.WriteLine($"  ID: {result.AssetId}");
            
            var asset = await _assetService.GetAssetAsync(result.AssetId);
            if (asset != null)
            {
                System.Console.WriteLine($"  名称: {asset.Name}");
                System.Console.WriteLine($"  类型: {asset.FileType}");
                System.Console.WriteLine($"  大小: {asset.FileSize / 1024.0:F2} KB");
                if (asset.Width.HasValue && asset.Height.HasValue)
                {
                    System.Console.WriteLine($"  尺寸: {asset.Width}x{asset.Height}");
                }
            }
        }
    }

    static async Task ViewAssetsMenu()
    {
        System.Console.Clear();
        System.Console.WriteLine("=== 查看资源 ===\n");

        var repository = new AssetRepository(_connectionString);
        var assets = await repository.QueryAsync(1, 20);

        if (assets.Count == 0)
        {
            System.Console.WriteLine("暂无资源。");
            return;
        }

        System.Console.WriteLine($"共 {assets.Count} 个资源:\n");
        foreach (var asset in assets)
        {
            System.Console.WriteLine($"[{asset.Id[..8]}...] {asset.Name}");
            System.Console.WriteLine($"  类型: {asset.FileType}, 大小: {asset.FileSize / 1024.0:F2} KB");
            if (asset.Width.HasValue && asset.Height.HasValue)
            {
                System.Console.WriteLine($"  尺寸: {asset.Width}x{asset.Height}");
            }
            System.Console.WriteLine();
        }
    }

    static async Task TagManagementMenu()
    {
        System.Console.Clear();
        System.Console.WriteLine("=== 标签管理 ===\n");
        System.Console.WriteLine("1. 查看所有标签");
        System.Console.WriteLine("2. 创建标签");
        System.Console.WriteLine("3. 为资源添加标签");
        System.Console.WriteLine("4. 按标签查询资源");
        System.Console.WriteLine("0. 返回");
        System.Console.Write("\n请选择: ");

        var choice = System.Console.ReadLine();

        switch (choice)
        {
            case "1":
                await ViewAllTags();
                break;
            case "2":
                await CreateTag();
                break;
            case "3":
                await AddTagToAsset();
                break;
            case "4":
                await QueryAssetsByTag();
                break;
        }
    }

    static async Task ViewAllTags()
    {
        System.Console.Clear();
        System.Console.WriteLine("=== 所有标签 ===\n");

        var tags = await _tagService!.GetAllTagsAsync();
        
        var grouped = tags.GroupBy(t => t.Category);
        foreach (var group in grouped)
        {
            System.Console.WriteLine($"\n【{group.Key}】");
            foreach (var tag in group)
            {
                System.Console.WriteLine($"  [{tag.Id}] {tag.Name} - {tag.Description}");
            }
        }
    }

    static async Task CreateTag()
    {
        System.Console.Clear();
        System.Console.WriteLine("=== 创建标签 ===\n");

        System.Console.Write("标签名称: ");
        var name = System.Console.ReadLine();

        System.Console.Write("分类 (org/style/type/status): ");
        var category = System.Console.ReadLine();

        System.Console.Write("描述: ");
        var description = System.Console.ReadLine();

        System.Console.Write("颜色 (如 #FF0000): ");
        var color = System.Console.ReadLine();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(category))
        {
            System.Console.WriteLine("名称和分类不能为空！");
            return;
        }

        var tagId = await _tagService!.CreateTagAsync(name, category, description, color);
        System.Console.WriteLine($"\n✓ 标签创建成功！ID: {tagId}");
    }

    static async Task AddTagToAsset()
    {
        System.Console.Clear();
        System.Console.WriteLine("=== 为资源添加标签 ===\n");

        System.Console.Write("资源ID: ");
        var assetId = System.Console.ReadLine();

        System.Console.Write("标签ID: ");
        var tagIdStr = System.Console.ReadLine();

        if (string.IsNullOrEmpty(assetId) || !int.TryParse(tagIdStr, out int tagId))
        {
            System.Console.WriteLine("输入无效！");
            return;
        }

        var result = await _tagService!.AddTagToAssetAsync(assetId, tagId);
        if (result)
        {
            System.Console.WriteLine("\n✓ 标签添加成功！");
        }
        else
        {
            System.Console.WriteLine("\n标签可能已存在。");
        }
    }

    static async Task QueryAssetsByTag()
    {
        System.Console.Clear();
        System.Console.WriteLine("=== 按标签查询资源 ===\n");

        System.Console.Write("标签ID: ");
        var tagIdStr = System.Console.ReadLine();

        if (!int.TryParse(tagIdStr, out int tagId))
        {
            System.Console.WriteLine("输入无效！");
            return;
        }

        var assets = await _tagService!.GetAssetsByTagAsync(tagId);
        
        System.Console.WriteLine($"\n找到 {assets.Count} 个资源:\n");
        foreach (var asset in assets)
        {
            System.Console.WriteLine($"[{asset.Id[..8]}...] {asset.Name}");
        }
    }

    static async Task ProjectManagementMenu()
    {
        System.Console.Clear();
        System.Console.WriteLine("=== 项目管理 ===\n");
        System.Console.WriteLine("1. 查看所有项目");
        System.Console.WriteLine("2. 创建项目");
        System.Console.WriteLine("3. 为项目添加资源");
        System.Console.WriteLine("4. 查看项目资源");
        System.Console.WriteLine("0. 返回");
        System.Console.Write("\n请选择: ");

        var choice = System.Console.ReadLine();

        switch (choice)
        {
            case "1":
                await ViewAllProjects();
                break;
            case "2":
                await CreateProject();
                break;
            case "3":
                await AddAssetToProject();
                break;
            case "4":
                await ViewProjectAssets();
                break;
        }
    }

    static async Task ViewAllProjects()
    {
        System.Console.Clear();
        System.Console.WriteLine("=== 所有项目 ===\n");

        var projects = await _projectService!.GetAllProjectsAsync();
        
        if (projects.Count == 0)
        {
            System.Console.WriteLine("暂无项目。");
            return;
        }

        foreach (var project in projects)
        {
            var count = await _projectService.GetProjectAssetCountAsync(project.Id);
            System.Console.WriteLine($"[{project.Id[..8]}...] {project.Name}");
            System.Console.WriteLine($"  Unity路径: {project.UnityPath}");
            System.Console.WriteLine($"  描述: {project.Description}");
            System.Console.WriteLine($"  资源数量: {count}");
            System.Console.WriteLine();
        }
    }

    static async Task CreateProject()
    {
        System.Console.Clear();
        System.Console.WriteLine("=== 创建项目 ===\n");

        System.Console.Write("项目名称: ");
        var name = System.Console.ReadLine();

        System.Console.Write("Unity路径 (如 Assets/MyProject): ");
        var unityPath = System.Console.ReadLine();

        System.Console.Write("描述: ");
        var description = System.Console.ReadLine();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(unityPath))
        {
            System.Console.WriteLine("名称和Unity路径不能为空！");
            return;
        }

        var projectId = await _projectService!.CreateProjectAsync(name, unityPath, description);
        System.Console.WriteLine($"\n✓ 项目创建成功！ID: {projectId}");
    }

    static async Task AddAssetToProject()
    {
        System.Console.Clear();
        System.Console.WriteLine("=== 为项目添加资源 ===\n");

        System.Console.Write("项目ID: ");
        var projectId = System.Console.ReadLine();

        System.Console.Write("资源ID: ");
        var assetId = System.Console.ReadLine();

        System.Console.Write("导入名称 (如 icon.png): ");
        var importName = System.Console.ReadLine();

        System.Console.Write("导入路径 (如 Assets/MyProject/icon.png): ");
        var importPath = System.Console.ReadLine();

        if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(assetId) || 
            string.IsNullOrEmpty(importName) || string.IsNullOrEmpty(importPath))
        {
            System.Console.WriteLine("所有字段都不能为空！");
            return;
        }

        var result = await _projectService!.AddAssetToProjectAsync(projectId, assetId, importName, importPath);
        if (result)
        {
            System.Console.WriteLine("\n✓ 资源添加成功！");
        }
        else
        {
            System.Console.WriteLine("\n资源可能已存在。");
        }
    }

    static async Task ViewProjectAssets()
    {
        System.Console.Clear();
        System.Console.WriteLine("=== 查看项目资源 ===\n");

        System.Console.Write("项目ID: ");
        var projectId = System.Console.ReadLine();

        if (string.IsNullOrEmpty(projectId))
        {
            System.Console.WriteLine("项目ID不能为空！");
            return;
        }

        var assets = await _projectService!.GetProjectAssetsAsync(projectId);
        
        System.Console.WriteLine($"\n项目中有 {assets.Count} 个资源:\n");
        foreach (var asset in assets)
        {
            System.Console.WriteLine($"[{asset.Id[..8]}...] {asset.Name}");
            System.Console.WriteLine($"  类型: {asset.FileType}, 大小: {asset.FileSize / 1024.0:F2} KB");
            System.Console.WriteLine();
        }
    }

    static async Task ViewDatabaseStats()
    {
        System.Console.Clear();
        System.Console.WriteLine("=== 数据库统计 ===\n");

        var assetRepo = new AssetRepository(_connectionString);
        var assetCount = await assetRepo.GetCountAsync();

        var tags = await _tagService!.GetAllTagsAsync();
        var projects = await _projectService!.GetAllProjectsAsync();

        System.Console.WriteLine($"资源总数: {assetCount}");
        System.Console.WriteLine($"标签总数: {tags.Count}");
        System.Console.WriteLine($"项目总数: {projects.Count}");
        System.Console.WriteLine();

        System.Console.WriteLine("标签分类统计:");
        var grouped = tags.GroupBy(t => t.Category);
        foreach (var group in grouped)
        {
            System.Console.WriteLine($"  {group.Key}: {group.Count()}个");
        }
    }
}
