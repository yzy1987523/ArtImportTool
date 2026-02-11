using Dapper.Contrib.Extensions;

namespace ArtAssetManager.Core.Models;

/// <summary>
/// Project实体
/// </summary>
[Table("Projects")]
public class Project
{
    [Key]
    public string Id { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public string UnityPath { get; set; } = string.Empty;
    
    public long CreatedAt { get; set; }
    
    public long UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; }
}
