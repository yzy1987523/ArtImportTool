using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.Sqlite;
using ArtAssetManager.Core.Models;
using ArtAssetManager.Core.Repositories;

namespace ArtAssetManager.Core.Services
{
    public class StyleMigrationService
    {
        private readonly StyleMigrationRepository _migrationRepository;
        private readonly AssetRepository _assetRepository;
        private readonly NameMatchingService _nameMatchingService;

        public StyleMigrationService(IDbConnection connection)
        {
            _migrationRepository = new StyleMigrationRepository(connection);
            _assetRepository = new AssetRepository($"Data Source={((SqliteConnection)connection).DataSource}");
            _nameMatchingService = new NameMatchingService();
        }

        /// <summary>
        /// 上传风格化资源并自动匹配原始资源
        /// </summary>
        public (StyleMigration? Migration, NameMatchingService.MatchResult? Match) UploadStyledAsset(
            string styledAssetId, string styleTag, string? projectId = null)
        {
            var styledAsset = _assetRepository.GetByIdAsync(styledAssetId).Result;
            if (styledAsset == null)
            {
                throw new ArgumentException("Styled asset not found");
            }

            // 获取所有带org标签的资源作为候选
            var orgAssets = _assetRepository.GetByTagAsync(1).Result; // org tag id = 1
            var candidates = orgAssets.Select(a => (a.Id, a.Name)).ToList();

            // 规范化名称并查找最佳匹配
            var normalizedName = _nameMatchingService.NormalizeName(styledAsset.Name);
            var match = _nameMatchingService.FindBestMatch(normalizedName, 
                candidates.Select(c => (c.Id, _nameMatchingService.NormalizeName(c.Name))).ToList());

            if (match == null)
            {
                return (null, null);
            }

            // 创建风格迁移记录
            var migration = new StyleMigration
            {
                OriginalAssetId = match.AssetId,
                StyledAssetId = styledAssetId,
                StyleTag = styleTag,
                ProjectId = projectId,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                CreatedBy = "system"
            };

            _migrationRepository.CreateMigration(migration);

            return (migration, match);
        }

        /// <summary>
        /// 手动创建风格迁移关联
        /// </summary>
        public StyleMigration CreateMigration(string originalAssetId, string styledAssetId, 
                                             string styleTag, string? projectId = null)
        {
            var migration = new StyleMigration
            {
                OriginalAssetId = originalAssetId,
                StyledAssetId = styledAssetId,
                StyleTag = styleTag,
                ProjectId = projectId,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                CreatedBy = "manual"
            };

            _migrationRepository.CreateMigration(migration);
            return migration;
        }

        /// <summary>
        /// 获取资源的所有风格化版本
        /// </summary>
        public List<StyleMigration> GetStyledVersions(string originalAssetId)
        {
            return _migrationRepository.GetMigrationsByOriginalAsset(originalAssetId);
        }

        /// <summary>
        /// 按风格标签获取所有迁移记录
        /// </summary>
        public List<StyleMigration> GetMigrationsByStyle(string styleTag)
        {
            return _migrationRepository.GetMigrationsByStyleTag(styleTag);
        }

        /// <summary>
        /// 获取项目的所有风格迁移
        /// </summary>
        public List<StyleMigration> GetProjectMigrations(string projectId)
        {
            return _migrationRepository.GetMigrationsByProject(projectId);
        }
    }
}
