using System.Drawing;

namespace ArtAssetManager.Core.Services;

/// <summary>
/// 资源元数据提取器
/// </summary>
public class AssetMetadataExtractor
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff", ".tif", ".webp", ".psd"
    };

    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".ogg", ".aac", ".flac", ".m4a", ".wma"
    };

    /// <summary>
    /// 提取资源元数据
    /// </summary>
    public async Task<AssetMetadata> ExtractMetadataAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var fileInfo = new FileInfo(filePath);
        var extension = fileInfo.Extension.ToLower();
        
        var metadata = new AssetMetadata
        {
            Name = Path.GetFileNameWithoutExtension(filePath),
            FilePath = filePath,
            FileType = extension.TrimStart('.'),
            FileSize = fileInfo.Length
        };

        // 提取图片元数据
        if (ImageExtensions.Contains(extension))
        {
            await ExtractImageMetadataAsync(filePath, metadata);
        }
        // 提取音频元数据
        else if (AudioExtensions.Contains(extension))
        {
            await ExtractAudioMetadataAsync(filePath, metadata);
        }

        return metadata;
    }

    /// <summary>
    /// 提取图片元数据
    /// </summary>
    private async Task ExtractImageMetadataAsync(string filePath, AssetMetadata metadata)
    {
        await Task.Run(() =>
        {
            try
            {
                using var image = Image.FromFile(filePath);
                metadata.Width = image.Width;
                metadata.Height = image.Height;
            }
            catch (Exception ex)
            {
                // 如果无法读取图片信息，记录错误但不抛出异常
                metadata.Metadata = $"{{\"error\": \"Failed to extract image metadata: {ex.Message}\"}}";
            }
        });
    }

    /// <summary>
    /// 提取音频元数据
    /// </summary>
    private async Task ExtractAudioMetadataAsync(string filePath, AssetMetadata metadata)
    {
        await Task.Run(() =>
        {
            try
            {
                // 简单实现：通过文件大小估算时长
                // 实际项目中应使用专门的音频库（如 NAudio）
                var fileSize = new FileInfo(filePath).Length;
                
                // 假设平均比特率为 128kbps
                var estimatedDurationMs = (int)((fileSize * 8) / (128 * 1000.0) * 1000);
                metadata.Duration = estimatedDurationMs;
                
                metadata.Metadata = $"{{\"estimated\": true, \"bitrate\": \"128kbps\"}}";
            }
            catch (Exception ex)
            {
                metadata.Metadata = $"{{\"error\": \"Failed to extract audio metadata: {ex.Message}\"}}";
            }
        });
    }

    /// <summary>
    /// 批量提取元数据
    /// </summary>
    public async Task<List<AssetMetadata>> ExtractMetadataBatchAsync(List<string> filePaths)
    {
        var tasks = filePaths.Select(ExtractMetadataAsync);
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }
}

/// <summary>
/// 资源元数据
/// </summary>
public class AssetMetadata
{
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Duration { get; set; }
    public string? Metadata { get; set; }
}
