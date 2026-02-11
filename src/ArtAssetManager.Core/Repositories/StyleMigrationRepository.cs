using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using ArtAssetManager.Core.Models;

namespace ArtAssetManager.Core.Repositories
{
    public class StyleMigrationRepository
    {
        private readonly IDbConnection _connection;

        public StyleMigrationRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public void CreateMigration(StyleMigration migration)
        {
            var sql = @"
                INSERT INTO StyleMigrations (id, original_asset_id, styled_asset_id, style_tag, 
                                            project_id, metadata, created_at, created_by)
                VALUES (@Id, @OriginalAssetId, @StyledAssetId, @StyleTag, 
                        @ProjectId, @Metadata, @CreatedAt, @CreatedBy)";

            _connection.Execute(sql, migration);
        }

        public StyleMigration GetMigrationById(string id)
        {
            var sql = @"
                SELECT id as Id, original_asset_id as OriginalAssetId, 
                       styled_asset_id as StyledAssetId, style_tag as StyleTag,
                       project_id as ProjectId, metadata as Metadata, 
                       created_at as CreatedAt, created_by as CreatedBy
                FROM StyleMigrations 
                WHERE id = @Id";

            return _connection.QueryFirstOrDefault<StyleMigration>(sql, new { Id = id });
        }

        public List<StyleMigration> GetMigrationsByOriginalAsset(string originalAssetId)
        {
            var sql = @"
                SELECT id as Id, original_asset_id as OriginalAssetId, 
                       styled_asset_id as StyledAssetId, style_tag as StyleTag,
                       project_id as ProjectId, metadata as Metadata, 
                       created_at as CreatedAt, created_by as CreatedBy
                FROM StyleMigrations 
                WHERE original_asset_id = @OriginalAssetId
                ORDER BY created_at DESC";

            return _connection.Query<StyleMigration>(sql, new { OriginalAssetId = originalAssetId }).ToList();
        }

        public List<StyleMigration> GetMigrationsByStyleTag(string styleTag)
        {
            var sql = @"
                SELECT id as Id, original_asset_id as OriginalAssetId, 
                       styled_asset_id as StyledAssetId, style_tag as StyleTag,
                       project_id as ProjectId, metadata as Metadata, 
                       created_at as CreatedAt, created_by as CreatedBy
                FROM StyleMigrations 
                WHERE style_tag = @StyleTag
                ORDER BY created_at DESC";

            return _connection.Query<StyleMigration>(sql, new { StyleTag = styleTag }).ToList();
        }

        public List<StyleMigration> GetMigrationsByProject(string projectId)
        {
            var sql = @"
                SELECT id as Id, original_asset_id as OriginalAssetId, 
                       styled_asset_id as StyledAssetId, style_tag as StyleTag,
                       project_id as ProjectId, metadata as Metadata, 
                       created_at as CreatedAt, created_by as CreatedBy
                FROM StyleMigrations 
                WHERE project_id = @ProjectId
                ORDER BY created_at DESC";

            return _connection.Query<StyleMigration>(sql, new { ProjectId = projectId }).ToList();
        }
    }
}
