using System;

namespace ArtAssetManager.Core.Models
{
    public class StyleMigration
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string OriginalAssetId { get; set; } = string.Empty;
        public string StyledAssetId { get; set; } = string.Empty;
        public string StyleTag { get; set; } = string.Empty;
        public string? ProjectId { get; set; }
        public string? Metadata { get; set; }
        public long CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
    }
}
