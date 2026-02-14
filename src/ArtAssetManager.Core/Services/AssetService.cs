using System.Runtime.Versioning;
using ArtAssetManager.Core.Models;
using ArtAssetManager.Core.Repositories;

namespace ArtAssetManager.Core.Services;

/// <summary>
/// 资源服务
/// </summary>
[SupportedOSPlatform("windows")]
public class AssetService
{
    private readonly AssetRepository _assetRepository;
    private readonly FileHashCalculator _hashCalculator;
    private readonly AssetMetadataExtractor _metadataExtractor;

    public AssetService(
        AssetRepository assetRepository,
        FileHashCalculator hashCalculator,
        AssetMetadataExtractor metadataExtractor)
    {
        _assetRepository = assetRepository;
        _hashCalculator = hashCalculator;
        _metadataExtractor = metadataExtractor;
    }

    /// <summary>
    /// 导入单个资源
    /// </summary>
    public async Task<ImportAssetResult> ImportAssetAsync(string filePath)
    {
        // 1. 计算文件哈希
        var fileHash = await _hashCalculator.CalculateHashAsync(filePath);

        // 2. 检查是否已存在（去重）
        var existingAsset = await _assetRepository.GetByFileHashAsync(fileHash);
        if (existingAsset != null)
        {
            return new ImportAssetResult
            {
                AssetId = existingAsset.Id,
                IsNew = false,
                IsDuplicate = true,
                Message = $"Asset already exists with ID: {existingAsset.Id}"
            };
        }

        // 3. 提取元数据
        var metadata = await _metadataExtractor.ExtractMetadataAsync(filePath);

        // 4. 创建资源记录
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var asset = new Asset
        {
            Id = Guid.NewGuid().ToString(),
            Name = metadata.Name,
            FilePath = metadata.FilePath,
            FileType = metadata.FileType,
            FileSize = (int)metadata.FileSize,
            FileHash = fileHash,
            Width = metadata.Width,
            Height = metadata.Height,
            Duration = metadata.Duration,
            Metadata = metadata.Metadata,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };

        // 5. 保存到数据库
        var assetId = await _assetRepository.CreateAsync(asset);

        return new ImportAssetResult
        {
            AssetId = assetId,
            IsNew = true,
            IsDuplicate = false,
            Message = "Asset imported successfully"
        };
    }

    /// <summary>
    /// 批量导入资源
    /// </summary>
    public async Task<BatchImportResult> BatchImportAsync(List<string> filePaths)
    {
        var result = new BatchImportResult
        {
            TotalCount = filePaths.Count,
            SuccessCount = 0,
            DuplicateCount = 0,
            FailureCount = 0,
            SuccessIds = new List<string>(),
            Errors = new List<ImportError>()
        };

        foreach (var filePath in filePaths)
        {
            try
            {
                var importResult = await ImportAssetAsync(filePath);
                
                if (importResult.IsDuplicate)
                {
                    result.DuplicateCount++;
                }
                else if (importResult.IsNew)
                {
                    result.SuccessCount++;
                    result.SuccessIds.Add(importResult.AssetId);
                }
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.Errors.Add(new ImportError
                {
                    FilePath = filePath,
                    Message = ex.Message
                });
            }
        }

        return result;
    }

    /// <summary>
    /// 检查资源是否存在
    /// </summary>
    public async Task<bool> ExistsAsync(string fileHash)
    {
        var asset = await _assetRepository.GetByFileHashAsync(fileHash);
        return asset != null;
    }

    /// <summary>
    /// 获取资源
    /// </summary>
    public async Task<Asset?> GetAssetAsync(string id)
    {
        return await _assetRepository.GetByIdAsync(id);
    }
}

/// <summary>
/// 导入资源结果
/// </summary>
public class ImportAssetResult
{
    public string AssetId { get; set; } = string.Empty;
    public bool IsNew { get; set; }
    public bool IsDuplicate { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 批量导入结果
/// </summary>
public class BatchImportResult
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int DuplicateCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> SuccessIds { get; set; } = new();
    public List<ImportError> Errors { get; set; } = new();
}

/// <summary>
/// 导入错误
/// </summary>
public class ImportError
{
    public string FilePath { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
