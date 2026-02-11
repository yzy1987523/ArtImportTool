using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace ArtAssetManager.Unity.Editor
{
    public class AssetReplacementWindow : EditorWindow
    {
        private string _dbPath;
        private Vector2 _scrollPosition;
        private List<RouteItem> _routes = new List<RouteItem>();
        private Dictionary<string, List<StyleVersion>> _styleVersions = new Dictionary<string, List<StyleVersion>>();
        private string _statusMessage = "";
        private string _selectedStyleTag = "All";
        private List<string> _availableStyles = new List<string>();

        [MenuItem("Window/Art Asset Manager/Asset Replacement")]
        public static void ShowWindow()
        {
            var window = GetWindow<AssetReplacementWindow>("Asset Replacement");
            window.minSize = new Vector2(900, 600);
            window.Show();
        }

        private void OnEnable()
        {
            FindDatabase();
            LoadStyles();
            LoadRoutes();
        }

        private void FindDatabase()
        {
            // 优先从配置文件加载
            string configPath = Path.Combine(Application.dataPath, "..", "database-config.json");
            if (File.Exists(configPath))
            {
                try
                {
                    var config = ArtAssetManager.Core.Configuration.DatabaseConfig.LoadFromFile(configPath);
                    var validation = config.Validate();
                    if (validation.IsValid)
                    {
                        _dbPath = config.GetFullDatabasePath();
                        _statusMessage = config.IsSharedDatabase 
                            ? $"Shared database: {_dbPath} (Project: {config.ProjectName})"
                            : $"Local database: {_dbPath}";
                        return;
                    }
                    else
                    {
                        _statusMessage = $"Config validation failed: {validation.ErrorMessage}";
                    }
                }
                catch (Exception ex)
                {
                    _statusMessage = $"Failed to load config: {ex.Message}";
                }
            }

            // 回退到默认查找逻辑
            string currentPath = Application.dataPath;
            for (int i = 0; i < 5; i++)
            {
                string testPath = Path.Combine(currentPath, "art_asset_manager.db");
                if (File.Exists(testPath))
                {
                    _dbPath = testPath;
                    _statusMessage = $"Database found: {_dbPath}";
                    return;
                }
                currentPath = Directory.GetParent(currentPath)?.FullName;
                if (currentPath == null) break;
            }
            _statusMessage = "Database not found. Use Window > Art Asset Manager > Database Config to configure.";
        }

        private void LoadStyles()
        {
            if (string.IsNullOrEmpty(_dbPath)) return;

            _availableStyles.Clear();
            _availableStyles.Add("All");

            try
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT name FROM Tags WHERE category = 'style' ORDER BY sort_order";
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _availableStyles.Add(reader.GetString(0));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load styles: {ex.Message}");
            }
        }

        private void LoadRoutes()
        {
            if (string.IsNullOrEmpty(_dbPath)) return;

            _routes.Clear();
            _styleVersions.Clear();

            try
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT r.id, r.asset_id, r.unity_path, r.unity_name, 
                                   a.name as asset_name, a.file_type
                            FROM UnityRoutes r
                            JOIN ArtAssets a ON r.asset_id = a.id
                            WHERE r.is_active = 1
                            ORDER BY r.created_at DESC
                            LIMIT 50";

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var route = new RouteItem
                                {
                                    RouteId = reader.GetString(0),
                                    AssetId = reader.GetString(1),
                                    UnityPath = reader.GetString(2),
                                    UnityName = reader.GetString(3),
                                    AssetName = reader.GetString(4),
                                    FileType = reader.GetString(5)
                                };
                                _routes.Add(route);
                            }
                        }
                    }

                    // 加载每个资源的风格化版本
                    foreach (var route in _routes)
                    {
                        LoadStyleVersions(connection, route.AssetId);
                    }
                }

                _statusMessage = $"Loaded {_routes.Count} routes";
            }
            catch (Exception ex)
            {
                _statusMessage = $"Error: {ex.Message}";
                Debug.LogError($"Failed to load routes: {ex.Message}");
            }
        }

        private void LoadStyleVersions(SqliteConnection connection, string assetId)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT sm.id, sm.styled_asset_id, sm.style_tag, 
                           a.name, a.file_type
                    FROM StyleMigrations sm
                    JOIN ArtAssets a ON sm.styled_asset_id = a.id
                    WHERE sm.original_asset_id = @AssetId";

                command.Parameters.AddWithValue("@AssetId", assetId);

                var versions = new List<StyleVersion>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        versions.Add(new StyleVersion
                        {
                            MigrationId = reader.GetString(0),
                            StyledAssetId = reader.GetString(1),
                            StyleTag = reader.GetString(2),
                            AssetName = reader.GetString(3),
                            FileType = reader.GetString(4)
                        });
                    }
                }

                if (versions.Count > 0)
                {
                    _styleVersions[assetId] = versions;
                }
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            GUILayout.Label("Asset Replacement Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
            EditorGUILayout.Space();

            // 筛选和刷新
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.Label("Filter by Style:", GUILayout.Width(100));
            int selectedIndex = Mathf.Max(0, _availableStyles.IndexOf(_selectedStyleTag));
            int newIndex = EditorGUILayout.Popup(selectedIndex, _availableStyles.ToArray(), GUILayout.Width(150));
            if (newIndex != selectedIndex)
            {
                _selectedStyleTag = _availableStyles[newIndex];
            }

            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                LoadStyles();
                LoadRoutes();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // 资源列表
            if (_routes.Count == 0)
            {
                GUILayout.Label("No routes found", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                DrawRouteList();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRouteList()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var route in _routes)
            {
                // 检查是否有风格化版本
                if (!_styleVersions.ContainsKey(route.AssetId))
                {
                    continue;
                }

                var versions = _styleVersions[route.AssetId];

                // 应用风格筛选
                if (_selectedStyleTag != "All")
                {
                    versions = versions.Where(v => v.StyleTag == _selectedStyleTag).ToList();
                    if (versions.Count == 0) continue;
                }

                EditorGUILayout.BeginVertical("box");

                // 当前资源信息
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"Unity: {route.UnityPath}", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"Current: {route.AssetName} ({route.FileType})", GUILayout.Width(400));
                
                if (GUILayout.Button("View History", GUILayout.Width(100)))
                {
                    ShowHistory(route.RouteId);
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // 风格化版本
                GUILayout.Label("Available Styles:", EditorStyles.miniBoldLabel);
                foreach (var version in versions)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    
                    GUILayout.Label($"• {version.StyleTag}: {version.AssetName}", GUILayout.Width(400));
                    
                    if (GUILayout.Button("Replace", GUILayout.Width(80)))
                    {
                        ReplaceAsset(route, version);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();
        }

        private void ReplaceAsset(RouteItem route, StyleVersion version)
        {
            if (!EditorUtility.DisplayDialog("Confirm Replacement",
                $"Replace asset in Unity?\n\n" +
                $"Unity Path: {route.UnityPath}\n" +
                $"Current: {route.AssetName}\n" +
                $"New: {version.AssetName} ({version.StyleTag})\n\n" +
                $"This will update the file and Unity references.",
                "Replace", "Cancel"))
            {
                return;
            }

            try
            {
                // 获取新资源的文件路径
                string newFilePath = GetAssetFilePath(version.StyledAssetId);
                if (string.IsNullOrEmpty(newFilePath) || !File.Exists(newFilePath))
                {
                    EditorUtility.DisplayDialog("Error", "New asset file not found", "OK");
                    return;
                }

                // 替换Unity中的文件
                string unityFilePath = Path.Combine(Application.dataPath, "..", route.UnityPath);
                File.Copy(newFilePath, unityFilePath, true);

                // 刷新资源数据库
                AssetDatabase.Refresh();

                // 更新路由表
                UpdateRouteAsset(route.RouteId, version.StyledAssetId);

                _statusMessage = $"Asset replaced successfully: {route.UnityPath}";
                EditorUtility.DisplayDialog("Success", 
                    $"Asset replaced successfully!\n\nPath: {route.UnityPath}", 
                    "OK");

                // 重新加载
                LoadRoutes();
            }
            catch (Exception ex)
            {
                _statusMessage = $"Replacement failed: {ex.Message}";
                EditorUtility.DisplayDialog("Error", $"Failed to replace asset:\n{ex.Message}", "OK");
            }
        }

        private string GetAssetFilePath(string assetId)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT file_path FROM ArtAssets WHERE id = @Id";
                        command.Parameters.AddWithValue("@Id", assetId);
                        var result = command.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get asset file path: {ex.Message}");
                return null;
            }
        }

        private void UpdateRouteAsset(string routeId, string newAssetId)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    
                    // 获取旧的asset_id
                    string oldAssetId = null;
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT asset_id FROM UnityRoutes WHERE id = @Id";
                        command.Parameters.AddWithValue("@Id", routeId);
                        oldAssetId = command.ExecuteScalar()?.ToString();
                    }

                    // 更新路由
                    using (var command = connection.CreateCommand())
                    {
                        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        command.CommandText = @"
                            UPDATE UnityRoutes 
                            SET asset_id = @AssetId, updated_at = @UpdatedAt
                            WHERE id = @Id";

                        command.Parameters.AddWithValue("@Id", routeId);
                        command.Parameters.AddWithValue("@AssetId", newAssetId);
                        command.Parameters.AddWithValue("@UpdatedAt", now);
                        command.ExecuteNonQuery();

                        // 记录历史
                        command.CommandText = @"
                            INSERT INTO RouteHistory (id, route_id, old_asset_id, new_asset_id, 
                                                     action, created_at, created_by)
                            VALUES (@HistoryId, @RouteId, @OldAssetId, @NewAssetId, 
                                    @Action, @CreatedAt, @CreatedBy)";

                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@HistoryId", Guid.NewGuid().ToString());
                        command.Parameters.AddWithValue("@RouteId", routeId);
                        command.Parameters.AddWithValue("@OldAssetId", oldAssetId);
                        command.Parameters.AddWithValue("@NewAssetId", newAssetId);
                        command.Parameters.AddWithValue("@Action", "replace");
                        command.Parameters.AddWithValue("@CreatedAt", now);
                        command.Parameters.AddWithValue("@CreatedBy", "Unity Editor");
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update route: {ex.Message}");
                throw;
            }
        }

        private void ShowHistory(string routeId)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT action, created_at, created_by
                            FROM RouteHistory
                            WHERE route_id = @RouteId
                            ORDER BY created_at DESC
                            LIMIT 10";

                        command.Parameters.AddWithValue("@RouteId", routeId);

                        var history = new System.Text.StringBuilder();
                        history.AppendLine("Route History (Last 10 actions):\n");

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var action = reader.GetString(0);
                                var timestamp = reader.GetInt64(1);
                                var createdBy = reader.GetString(2);
                                var dateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;

                                history.AppendLine($"• {action.ToUpper()} - {dateTime:yyyy-MM-dd HH:mm} by {createdBy}");
                            }
                        }

                        EditorUtility.DisplayDialog("Route History", history.ToString(), "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to load history:\n{ex.Message}", "OK");
            }
        }

        private class RouteItem
        {
            public string RouteId { get; set; }
            public string AssetId { get; set; }
            public string UnityPath { get; set; }
            public string UnityName { get; set; }
            public string AssetName { get; set; }
            public string FileType { get; set; }
        }

        private class StyleVersion
        {
            public string MigrationId { get; set; }
            public string StyledAssetId { get; set; }
            public string StyleTag { get; set; }
            public string AssetName { get; set; }
            public string FileType { get; set; }
        }
    }
}
