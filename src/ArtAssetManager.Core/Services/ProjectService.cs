using ArtAssetManager.Core.Models;
using ArtAssetManager.Core.Repositories;

namespace ArtAssetManager.Core.Services;

/// <summary>
/// 项目服务
/// </summary>
public class ProjectService
{
    private readonly ProjectRepository _projectRepository;
    private readonly AssetRepository _assetRepository;

    public ProjectService(string connectionString)
    {
        _projectRepository = new ProjectRepository(connectionString);
        _assetRepository = new AssetRepository(connectionString);
    }

    /// <summary>
    /// 创建项目
    /// </summary>
    public async Task<string> CreateProjectAsync(string name, string unityPath, string? description = null)
    {
        // 检查Unity路径是否已存在
        var existing = await _projectRepository.GetByUnityPathAsync(unityPath);
        if (existing != null)
        {
            throw new InvalidOperationException($"Unity路径 '{unityPath}' 已被项目 '{existing.Name}' 使用");
        }

        var project = new Project
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = description,
            UnityPath = unityPath,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            IsDeleted = false
        };

        return await _projectRepository.CreateAsync(project);
    }

    /// <summary>
    /// 获取项目
    /// </summary>
    public async Task<Project?> GetProjectAsync(string id)
    {
        return await _projectRepository.GetByIdAsync(id);
    }

    /// <summary>
    /// 获取所有项目
    /// </summary>
    public async Task<List<Project>> GetAllProjectsAsync()
    {
        return await _projectRepository.GetAllAsync();
    }

    /// <summary>
    /// 为项目添加资源
    /// </summary>
    public async Task<bool> AddAssetToProjectAsync(string projectId, string assetId, string importName, string importPath, bool isOriginal = true)
    {
        // 验证项目存在
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project == null)
        {
            throw new InvalidOperationException($"项目 '{projectId}' 不存在");
        }

        // 验证资源存在
        var asset = await _assetRepository.GetByIdAsync(assetId);
        if (asset == null)
        {
            throw new InvalidOperationException($"资源 '{assetId}' 不存在");
        }

        return await _projectRepository.AddAssetToProjectAsync(projectId, assetId, importName, importPath, isOriginal);
    }

    /// <summary>
    /// 批量为项目添加资源
    /// </summary>
    public async Task<int> AddAssetsToProjectAsync(string projectId, List<(string AssetId, string ImportName, string ImportPath, bool IsOriginal)> assets)
    {
        // 验证项目存在
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project == null)
        {
            throw new InvalidOperationException($"项目 '{projectId}' 不存在");
        }

        return await _projectRepository.AddAssetsToProjectAsync(projectId, assets);
    }

    /// <summary>
    /// 从项目移除资源
    /// </summary>
    public async Task<bool> RemoveAssetFromProjectAsync(string projectId, string assetId)
    {
        return await _projectRepository.RemoveAssetFromProjectAsync(projectId, assetId);
    }

    /// <summary>
    /// 获取项目中的所有资源
    /// </summary>
    public async Task<List<Asset>> GetProjectAssetsAsync(string projectId)
    {
        return await _projectRepository.GetProjectAssetsAsync(projectId);
    }

    /// <summary>
    /// 获取资源所属的所有项目
    /// </summary>
    public async Task<List<Project>> GetAssetProjectsAsync(string assetId)
    {
        return await _projectRepository.GetAssetProjectsAsync(assetId);
    }

    /// <summary>
    /// 获取项目资源数量
    /// </summary>
    public async Task<int> GetProjectAssetCountAsync(string projectId)
    {
        return await _projectRepository.GetProjectAssetCountAsync(projectId);
    }
}
