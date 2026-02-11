using System;

namespace ArtAssetManager.Core.Models
{
    public class StyleMigration
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string OriginalAssetId { get; set; }
        public string StyledAssetId { get; set; }
        public string StyleTag { get; set; }
        public string ProjectId { get; set; }
        public string Metadata { get; set; }
        public long CreatedAt { get; set; }
        public string CreatedBy { get; set; }
    }
}
