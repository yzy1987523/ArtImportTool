using System;
using System.Collections.Generic;
using System.Data;
using ArtAssetManager.Core.Models;
using ArtAssetManager.Core.Repositories;

namespace ArtAssetManager.Core.Services
{
    public class RouteService
    {
        private readonly RouteRepository _routeRepository;

        public RouteService(IDbConnection connection)
        {
            _routeRepository = new RouteRepository(connection);
        }

        public UnityRoute CreateRoute(string assetId, string projectId, string unityGuid, 
                                     string unityPath, string unityName)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var route = new UnityRoute
            {
                AssetId = assetId,
                ProjectId = projectId,
                UnityGuid = unityGuid,
                UnityPath = unityPath,
                UnityName = unityName,
                OriginalImportPath = unityPath,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            _routeRepository.CreateRoute(route);

            // 记录历史
            var history = new RouteHistory
            {
                RouteId = route.Id,
                NewAssetId = assetId,
                NewUnityPath = unityPath,
                Action = "create",
                CreatedAt = now,
                CreatedBy = "system"
            };
            _routeRepository.CreateHistory(history);

            return route;
        }

        public UnityRoute GetRouteByGuid(string unityGuid)
        {
            return _routeRepository.GetRouteByGuid(unityGuid);
        }

        public List<UnityRoute> GetRoutesByAssetId(string assetId)
        {
            return _routeRepository.GetRoutesByAssetId(assetId);
        }

        public List<UnityRoute> GetRoutesByProjectId(string projectId)
        {
            return _routeRepository.GetRoutesByProjectId(projectId);
        }

        public void UpdateRoutePath(string routeId, string newPath, string newName)
        {
            var route = _routeRepository.GetRouteById(routeId);
            if (route == null) return;

            var oldPath = route.UnityPath;
            route.UnityPath = newPath;
            route.UnityName = newName;
            route.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            _routeRepository.UpdateRoute(route);

            // 记录历史
            var history = new RouteHistory
            {
                RouteId = routeId,
                OldUnityPath = oldPath,
                NewUnityPath = newPath,
                Action = "update",
                CreatedAt = route.UpdatedAt,
                CreatedBy = "system"
            };
            _routeRepository.CreateHistory(history);
        }

        public void ReplaceAsset(string routeId, string newAssetId)
        {
            var route = _routeRepository.GetRouteById(routeId);
            if (route == null) return;

            var oldAssetId = route.AssetId;
            route.AssetId = newAssetId;
            route.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            _routeRepository.UpdateRoute(route);

            // 记录历史
            var history = new RouteHistory
            {
                RouteId = routeId,
                OldAssetId = oldAssetId,
                NewAssetId = newAssetId,
                Action = "replace",
                CreatedAt = route.UpdatedAt,
                CreatedBy = "system"
            };
            _routeRepository.CreateHistory(history);
        }

        /// <summary>
        /// 批量替换资源
        /// </summary>
        public int BatchReplaceAssets(List<string> routeIds, string newAssetId)
        {
            int count = 0;
            foreach (var routeId in routeIds)
            {
                try
                {
                    ReplaceAsset(routeId, newAssetId);
                    count++;
                }
                catch (Exception)
                {
                    // 继续处理其他资源
                }
            }
            return count;
        }

        /// <summary>
        /// 回滚资源替换（恢复到上一个版本）
        /// </summary>
        public bool RollbackReplace(string routeId)
        {
            var history = _routeRepository.GetHistoryByRouteId(routeId);
            var lastReplace = history.FirstOrDefault(h => h.Action == "replace");

            if (lastReplace == null || string.IsNullOrEmpty(lastReplace.OldAssetId))
            {
                return false;
            }

            ReplaceAsset(routeId, lastReplace.OldAssetId);
            return true;
        }

        public List<RouteHistory> GetRouteHistory(string routeId)
        {
            return _routeRepository.GetHistoryByRouteId(routeId);
        }
    }
}
