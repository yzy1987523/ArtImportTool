using ArtAssetManager.Core.Models;
using ArtAssetManager.Core.Repositories;

namespace ArtAssetManager.Core.Services;

/// <summary>
/// 标签服务
/// </summary>
public class TagService
{
    private readonly TagRepository _tagRepository;
    private readonly AssetRepository _assetRepository;

    public TagService(string connectionString)
    {
        _tagRepository = new TagRepository(connectionString);
        _assetRepository = new AssetRepository(connectionString);
    }

    /// <summary>
    /// 创建标签
    /// </summary>
    public async Task<int> CreateTagAsync(string name, string category, string? description = null, string? color = null, int sortOrder = 0)
    {
        // 检查标签是否已存在
        var existing = await _tagRepository.GetByNameAsync(name);
        if (existing != null)
        {
            throw new InvalidOperationException($"标签 '{name}' 已存在");
        }

        var tag = new Tag
        {
            Name = name,
            Category = category,
            Description = description,
            Color = color,
            SortOrder = sortOrder,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        return await _tagRepository.CreateAsync(tag);
    }

    /// <summary>
    /// 获取所有标签
    /// </summary>
    public async Task<List<Tag>> GetAllTagsAsync()
    {
        return await _tagRepository.GetAllAsync();
    }

    /// <summary>
    /// 按分类获取标签
    /// </summary>
    public async Task<List<Tag>> GetTagsByCategoryAsync(string category)
    {
        return await _tagRepository.GetByCategoryAsync(category);
    }

    /// <summary>
    /// 为资源添加标签
    /// </summary>
    public async Task<bool> AddTagToAssetAsync(string assetId, int tagId, string? createdBy = null)
    {
        // 验证资源存在
        var asset = await _assetRepository.GetByIdAsync(assetId);
        if (asset == null)
        {
            throw new InvalidOperationException($"资源 '{assetId}' 不存在");
        }

        // 验证标签存在
        var tag = await _tagRepository.GetByIdAsync(tagId);
        if (tag == null)
        {
            throw new InvalidOperationException($"标签ID '{tagId}' 不存在");
        }

        return await _tagRepository.AddTagToAssetAsync(assetId, tagId, createdBy);
    }

    /// <summary>
    /// 批量为资源添加标签
    /// </summary>
    public async Task<int> AddTagsToAssetAsync(string assetId, List<int> tagIds, string? createdBy = null)
    {
        // 验证资源存在
        var asset = await _assetRepository.GetByIdAsync(assetId);
        if (asset == null)
        {
            throw new InvalidOperationException($"资源 '{assetId}' 不存在");
        }

        return await _tagRepository.AddTagsToAssetAsync(assetId, tagIds, createdBy);
    }

    /// <summary>
    /// 从资源移除标签
    /// </summary>
    public async Task<bool> RemoveTagFromAssetAsync(string assetId, int tagId)
    {
        return await _tagRepository.RemoveTagFromAssetAsync(assetId, tagId);
    }

    /// <summary>
    /// 获取资源的所有标签
    /// </summary>
    public async Task<List<Tag>> GetAssetTagsAsync(string assetId)
    {
        return await _tagRepository.GetAssetTagsAsync(assetId);
    }

    /// <summary>
    /// 按标签查询资源（单个标签）
    /// </summary>
    public async Task<List<Asset>> GetAssetsByTagAsync(int tagId)
    {
        return await _assetRepository.GetByTagAsync(tagId);
    }

    /// <summary>
    /// 按标签组合查询资源（AND逻辑：资源必须包含所有指定标签）
    /// </summary>
    public async Task<List<Asset>> GetAssetsByTagsAsync(List<int> tagIds)
    {
        if (tagIds == null || tagIds.Count == 0)
        {
            return new List<Asset>();
        }

        return await _assetRepository.GetByTagsAsync(tagIds);
    }
}
