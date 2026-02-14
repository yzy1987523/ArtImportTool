using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using ArtAssetManager.Core.Models;

namespace ArtAssetManager.Core.Repositories
{
    public class RouteRepository
    {
        private readonly IDbConnection _connection;

        public RouteRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public void CreateRoute(UnityRoute route)
        {
            var sql = @"
                INSERT INTO UnityRoutes (id, asset_id, project_id, unity_guid, unity_path, 
                                        unity_name, original_import_path, is_active, 
                                        created_at, updated_at)
                VALUES (@Id, @AssetId, @ProjectId, @UnityGuid, @UnityPath, 
                        @UnityName, @OriginalImportPath, @IsActive, 
                        @CreatedAt, @UpdatedAt)";

            _connection.Execute(sql, new
            {
                route.Id,
                route.AssetId,
                route.ProjectId,
                route.UnityGuid,
                route.UnityPath,
                route.UnityName,
                route.OriginalImportPath,
                IsActive = route.IsActive ? 1 : 0,
                route.CreatedAt,
                route.UpdatedAt
            });
        }

        public UnityRoute GetRouteById(string id)
        {
            var sql = @"
                SELECT id as Id, asset_id as AssetId, project_id as ProjectId, 
                       unity_guid as UnityGuid, unity_path as UnityPath, 
                       unity_name as UnityName, original_import_path as OriginalImportPath,
                       is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt
                FROM UnityRoutes 
                WHERE id = @Id";

            return _connection.QueryFirstOrDefault<UnityRoute>(sql, new { Id = id })!;
        }

        public UnityRoute GetRouteByGuid(string unityGuid)
        {
            var sql = @"
                SELECT id as Id, asset_id as AssetId, project_id as ProjectId, 
                       unity_guid as UnityGuid, unity_path as UnityPath, 
                       unity_name as UnityName, original_import_path as OriginalImportPath,
                       is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt
                FROM UnityRoutes 
                WHERE unity_guid = @UnityGuid";

            return _connection.QueryFirstOrDefault<UnityRoute>(sql, new { UnityGuid = unityGuid })!;
        }

        public List<UnityRoute> GetRoutesByAssetId(string assetId)
        {
            var sql = @"
                SELECT id as Id, asset_id as AssetId, project_id as ProjectId, 
                       unity_guid as UnityGuid, unity_path as UnityPath, 
                       unity_name as UnityName, original_import_path as OriginalImportPath,
                       is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt
                FROM UnityRoutes 
                WHERE asset_id = @AssetId
                ORDER BY created_at DESC";

            return _connection.Query<UnityRoute>(sql, new { AssetId = assetId }).ToList();
        }

        public List<UnityRoute> GetRoutesByProjectId(string projectId)
        {
            var sql = @"
                SELECT id as Id, asset_id as AssetId, project_id as ProjectId, 
                       unity_guid as UnityGuid, unity_path as UnityPath, 
                       unity_name as UnityName, original_import_path as OriginalImportPath,
                       is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt
                FROM UnityRoutes 
                WHERE project_id = @ProjectId
                ORDER BY created_at DESC";

            return _connection.Query<UnityRoute>(sql, new { ProjectId = projectId }).ToList();
        }

        public void UpdateRoute(UnityRoute route)
        {
            var sql = @"
                UPDATE UnityRoutes 
                SET asset_id = @AssetId, 
                    unity_path = @UnityPath, 
                    unity_name = @UnityName,
                    is_active = @IsActive,
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            _connection.Execute(sql, new
            {
                route.Id,
                route.AssetId,
                route.UnityPath,
                route.UnityName,
                IsActive = route.IsActive ? 1 : 0,
                route.UpdatedAt
            });
        }

        public void CreateHistory(RouteHistory history)
        {
            var sql = @"
                INSERT INTO RouteHistory (id, route_id, old_asset_id, new_asset_id, 
                                         old_unity_path, new_unity_path, action, 
                                         metadata, created_at, created_by)
                VALUES (@Id, @RouteId, @OldAssetId, @NewAssetId, 
                        @OldUnityPath, @NewUnityPath, @Action, 
                        @Metadata, @CreatedAt, @CreatedBy)";

            _connection.Execute(sql, history);
        }

        public List<RouteHistory> GetHistoryByRouteId(string routeId)
        {
            var sql = @"
                SELECT id as Id, route_id as RouteId, old_asset_id as OldAssetId, 
                       new_asset_id as NewAssetId, old_unity_path as OldUnityPath, 
                       new_unity_path as NewUnityPath, action as Action, 
                       metadata as Metadata, created_at as CreatedAt, created_by as CreatedBy
                FROM RouteHistory 
                WHERE route_id = @RouteId
                ORDER BY created_at DESC";

            return _connection.Query<RouteHistory>(sql, new { RouteId = routeId }).ToList();
        }
    }
}
