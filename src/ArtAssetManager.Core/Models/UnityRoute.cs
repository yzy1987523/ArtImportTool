using System;

namespace ArtAssetManager.Core.Models
{
    public class UnityRoute
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AssetId { get; set; }
        public string ProjectId { get; set; }
        public string UnityGuid { get; set; }
        public string UnityPath { get; set; }
        public string UnityName { get; set; }
        public string OriginalImportPath { get; set; }
        public bool IsActive { get; set; } = true;
        public long CreatedAt { get; set; }
        public long UpdatedAt { get; set; }
    }

    public class RouteHistory
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string RouteId { get; set; }
        public string OldAssetId { get; set; }
        public string NewAssetId { get; set; }
        public string OldUnityPath { get; set; }
        public string NewUnityPath { get; set; }
        public string Action { get; set; } // create, update, replace, delete
        public string Metadata { get; set; }
        public long CreatedAt { get; set; }
        public string CreatedBy { get; set; }
    }
}
