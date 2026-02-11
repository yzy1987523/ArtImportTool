namespace ArtAssetManager.Core.Models;

/// <summary>
/// 项目实体
/// </summary>
public class Project
{
    public string Id { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public string UnityPath { get; set; } = string.Empty;
    
    public long CreatedAt { get; set; }
    
    public long UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; }
}
