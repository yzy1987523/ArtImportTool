namespace ArtAssetManager.Core.Models;

/// <summary>
/// 项目资源关联实体
/// </summary>
public class ProjectAsset
{
    public int Id { get; set; }
    
    public string ProjectId { get; set; } = string.Empty;
    
    public string AssetId { get; set; } = string.Empty;
    
    public string ImportName { get; set; } = string.Empty;
    
    public string ImportPath { get; set; } = string.Empty;
    
    public bool IsOriginal { get; set; } = true;
    
    public long CreatedAt { get; set; }
}
