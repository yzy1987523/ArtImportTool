using Dapper.Contrib.Extensions;

namespace ArtAssetManager.Core.Models;

/// <summary>
/// Tag实体
/// </summary>
[Table("Tags")]
public class Tag
{
    [Key]
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string Category { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public string? Color { get; set; }
    
    public int SortOrder { get; set; }
    
    public long CreatedAt { get; set; }
    
    public long UpdatedAt { get; set; }
}
