namespace ArtAssetManager.Core.Models;

/// <summary>
/// 资源标签关联实体
/// </summary>
public class AssetTag
{
    public int Id { get; set; }
    
    public string AssetId { get; set; } = string.Empty;
    
    public int TagId { get; set; }
    
    public long CreatedAt { get; set; }
    
    public string? CreatedBy { get; set; }
}
