using System.ComponentModel.DataAnnotations.Schema;

namespace ArtAssetManager.Core.Models;

/// <summary>
/// 资源实体
/// </summary>
public class Asset
{
    public string Id { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    
    public string FilePath { get; set; } = string.Empty;
    
    public string FileType { get; set; } = string.Empty;
    
    public int FileSize { get; set; }
    
    public string FileHash { get; set; } = string.Empty;
    
    public int? Width { get; set; }
    
    public int? Height { get; set; }
    
    public int? Duration { get; set; }
    
    public string? Metadata { get; set; }
    
    public long CreatedAt { get; set; }
    
    public long UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; }
}
