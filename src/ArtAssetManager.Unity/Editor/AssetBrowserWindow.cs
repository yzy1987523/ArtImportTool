using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Linq;

namespace ArtAssetManager.Unity.Editor
{
    public class AssetBrowserWindow : EditorWindow
    {
        private string _dbPath;
        private Vector2 _scrollPosition;
        private List<AssetItem> _assets = new List<AssetItem>();
        private string _searchText = "";
        private string _selectedTag = "All";
        private List<string> _availableTags = new List<string>();
        private bool _isLoading = false;
        private string _statusMessage = "";

        [MenuItem("Window/Art Asset Manager/Asset Browser")]
        public static void ShowWindow()
        {
            var window = GetWindow<AssetBrowserWindow>("Asset Browser");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private void OnEnable()
        {
            FindDatabase();
            LoadTags();
            LoadAssets();
        }

        private void FindDatabase()
        {
            // 查找数据库文件（向上查找项目根目录）
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
            _statusMessage = "Database not found. Please ensure art_asset_manager.db exists.";
        }

        private void LoadTags()
        {
            if (string.IsNullOrEmpty(_dbPath)) return;

            _availableTags.Clear();
            _availableTags.Add("All");

            try
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT name FROM Tags ORDER BY category, sort_order";
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _availableTags.Add(reader.GetString(0));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load tags: {ex.Message}");
            }
        }

        private void LoadAssets()
        {
            if (string.IsNullOrEmpty(_dbPath))
            {
                _assets.Clear();
                return;
            }

            _isLoading = true;
            _assets.Clear();

            try
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        // 构建查询
                        string query = @"
                            SELECT DISTINCT a.id, a.name, a.file_type, a.file_size, 
                                   a.width, a.height, a.created_at
                            FROM ArtAssets a
                            WHERE a.is_deleted = 0";

                        // 添加标签筛选
                        if (_selectedTag != "All")
                        {
                            query += @"
                                AND EXISTS (
                                    SELECT 1 FROM AssetTags at
                                    JOIN Tags t ON at.tag_id = t.id
                                    WHERE at.asset_id = a.id AND t.name = @tagName
                                )";
                        }

                        // 添加搜索筛选
                        if (!string.IsNullOrWhiteSpace(_searchText))
                        {
                            query += " AND a.name LIKE @searchText";
                        }

                        query += " ORDER BY a.created_at DESC LIMIT 100";

                        command.CommandText = query;
                        
                        if (_selectedTag != "All")
                        {
                            command.Parameters.AddWithValue("@tagName", _selectedTag);
                        }
                        
                        if (!string.IsNullOrWhiteSpace(_searchText))
                        {
                            command.Parameters.AddWithValue("@searchText", $"%{_searchText}%");
                        }

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _assets.Add(new AssetItem
                                {
                                    Id = reader.GetString(0),
                                    Name = reader.GetString(1),
                                    FileType = reader.GetString(2),
                                    FileSize = reader.GetInt64(3),
                                    Width = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                                    Height = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                                    CreatedAt = reader.GetInt64(6)
                                });
                            }
                        }
                    }
                }
                _statusMessage = $"Loaded {_assets.Count} assets";
            }
            catch (Exception ex)
            {
                _statusMessage = $"Error: {ex.Message}";
                Debug.LogError($"Failed to load assets: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            // 标题
            GUILayout.Label("Art Asset Browser", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 状态栏
            EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
            EditorGUILayout.Space();

            // 搜索和筛选区域
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.Label("Search:", GUILayout.Width(60));
            string newSearchText = EditorGUILayout.TextField(_searchText);
            if (newSearchText != _searchText)
            {
                _searchText = newSearchText;
                LoadAssets();
            }

            GUILayout.Space(20);
            
            GUILayout.Label("Tag:", GUILayout.Width(40));
            int selectedIndex = Mathf.Max(0, _availableTags.IndexOf(_selectedTag));
            int newIndex = EditorGUILayout.Popup(selectedIndex, _availableTags.ToArray(), GUILayout.Width(150));
            if (newIndex != selectedIndex)
            {
                _selectedTag = _availableTags[newIndex];
                LoadAssets();
            }

            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                LoadTags();
                LoadAssets();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // 资源列表
            if (_isLoading)
            {
                GUILayout.Label("Loading...", EditorStyles.centeredGreyMiniLabel);
            }
            else if (_assets.Count == 0)
            {
                GUILayout.Label("No assets found", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                DrawAssetList();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAssetList()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // 表头
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Name", EditorStyles.toolbarButton, GUILayout.Width(250));
            GUILayout.Label("Type", EditorStyles.toolbarButton, GUILayout.Width(80));
            GUILayout.Label("Size", EditorStyles.toolbarButton, GUILayout.Width(100));
            GUILayout.Label("Dimensions", EditorStyles.toolbarButton, GUILayout.Width(120));
            GUILayout.Label("Created", EditorStyles.toolbarButton, GUILayout.Width(150));
            GUILayout.Label("Actions", EditorStyles.toolbarButton, GUILayout.FlexibleSpace());
            EditorGUILayout.EndHorizontal();

            // 资源行
            foreach (var asset in _assets)
            {
                EditorGUILayout.BeginHorizontal();
                
                GUILayout.Label(asset.Name, GUILayout.Width(250));
                GUILayout.Label(asset.FileType.ToUpper(), GUILayout.Width(80));
                GUILayout.Label(FormatFileSize(asset.FileSize), GUILayout.Width(100));
                
                string dimensions = asset.Width > 0 ? $"{asset.Width}x{asset.Height}" : "-";
                GUILayout.Label(dimensions, GUILayout.Width(120));
                
                GUILayout.Label(FormatDateTime(asset.CreatedAt), GUILayout.Width(150));
                
                if (GUILayout.Button("Import", GUILayout.Width(80)))
                {
                    ImportAsset(asset);
                }
                
                if (GUILayout.Button("Info", GUILayout.Width(60)))
                {
                    ShowAssetInfo(asset);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void ImportAsset(AssetItem asset)
        {
            // 选择导入路径
            string defaultPath = "Assets/ImportedAssets";
            string importPath = EditorUtility.SaveFilePanelInProject(
                "Import Asset",
                asset.Name,
                asset.FileType,
                "Select where to import the asset",
                defaultPath
            );

            if (string.IsNullOrEmpty(importPath))
            {
                return; // 用户取消
            }

            try
            {
                // 获取资源文件路径
                string sourceFilePath = GetAssetFilePath(asset.Id);
                if (string.IsNullOrEmpty(sourceFilePath) || !File.Exists(sourceFilePath))
                {
                    EditorUtility.DisplayDialog("Error", "Source file not found", "OK");
                    return;
                }

                // 复制文件到Unity项目
                string targetPath = Path.Combine(Application.dataPath, "..", importPath);
                File.Copy(sourceFilePath, targetPath, true);

                // 刷新资源数据库
                AssetDatabase.Refresh();

                // 获取Unity GUID
                string unityGuid = AssetDatabase.AssetPathToGUID(importPath);

                // 创建路由表记录
                CreateRouteRecord(asset.Id, unityGuid, importPath, asset.Name);

                _statusMessage = $"Asset imported successfully: {importPath}";
                EditorUtility.DisplayDialog("Success", 
                    $"Asset imported successfully!\n\nPath: {importPath}\nGUID: {unityGuid}", 
                    "OK");
            }
            catch (Exception ex)
            {
                _statusMessage = $"Import failed: {ex.Message}";
                EditorUtility.DisplayDialog("Error", $"Failed to import asset:\n{ex.Message}", "OK");
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

        private void CreateRouteRecord(string assetId, string unityGuid, string unityPath, string unityName)
        {
            try
            {
                // 获取或创建默认项目
                string projectId = GetOrCreateDefaultProject();

                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        var routeId = Guid.NewGuid().ToString();
                        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                        command.CommandText = @"
                            INSERT INTO UnityRoutes (id, asset_id, project_id, unity_guid, unity_path, 
                                                    unity_name, original_import_path, is_active, 
                                                    created_at, updated_at)
                            VALUES (@Id, @AssetId, @ProjectId, @UnityGuid, @UnityPath, 
                                    @UnityName, @OriginalImportPath, 1, @CreatedAt, @UpdatedAt)";

                        command.Parameters.AddWithValue("@Id", routeId);
                        command.Parameters.AddWithValue("@AssetId", assetId);
                        command.Parameters.AddWithValue("@ProjectId", projectId);
                        command.Parameters.AddWithValue("@UnityGuid", unityGuid);
                        command.Parameters.AddWithValue("@UnityPath", unityPath);
                        command.Parameters.AddWithValue("@UnityName", unityName);
                        command.Parameters.AddWithValue("@OriginalImportPath", unityPath);
                        command.Parameters.AddWithValue("@CreatedAt", now);
                        command.Parameters.AddWithValue("@UpdatedAt", now);

                        command.ExecuteNonQuery();

                        // 记录历史
                        CreateRouteHistory(connection, routeId, assetId, unityPath, "create", now);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create route record: {ex.Message}");
            }
        }

        private string GetOrCreateDefaultProject()
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        // 查找默认项目
                        command.CommandText = "SELECT id FROM Projects WHERE name = @Name LIMIT 1";
                        command.Parameters.AddWithValue("@Name", "UnityImport");
                        var result = command.ExecuteScalar();

                        if (result != null)
                        {
                            return result.ToString();
                        }

                        // 创建默认项目
                        var projectId = Guid.NewGuid().ToString();
                        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                        command.CommandText = @"
                            INSERT INTO Projects (id, name, description, unity_path, created_at, updated_at, is_deleted)
                            VALUES (@Id, @Name, @Description, @UnityPath, @CreatedAt, @UpdatedAt, 0)";

                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@Id", projectId);
                        command.Parameters.AddWithValue("@Name", "UnityImport");
                        command.Parameters.AddWithValue("@Description", "Default project for Unity imports");
                        command.Parameters.AddWithValue("@UnityPath", "Assets/ImportedAssets");
                        command.Parameters.AddWithValue("@CreatedAt", now);
                        command.Parameters.AddWithValue("@UpdatedAt", now);

                        command.ExecuteNonQuery();
                        return projectId;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get or create default project: {ex.Message}");
                return Guid.NewGuid().ToString();
            }
        }

        private void CreateRouteHistory(SqliteConnection connection, string routeId, string assetId, 
                                       string unityPath, string action, long timestamp)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO RouteHistory (id, route_id, new_asset_id, new_unity_path, 
                                             action, created_at, created_by)
                    VALUES (@Id, @RouteId, @NewAssetId, @NewUnityPath, @Action, @CreatedAt, @CreatedBy)";

                command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                command.Parameters.AddWithValue("@RouteId", routeId);
                command.Parameters.AddWithValue("@NewAssetId", assetId);
                command.Parameters.AddWithValue("@NewUnityPath", unityPath);
                command.Parameters.AddWithValue("@Action", action);
                command.Parameters.AddWithValue("@CreatedAt", timestamp);
                command.Parameters.AddWithValue("@CreatedBy", "Unity Editor");

                command.ExecuteNonQuery();
            }
        }

        private void ShowAssetInfo(AssetItem asset)
        {
            string info = $"Asset Information\n\n" +
                         $"ID: {asset.Id}\n" +
                         $"Name: {asset.Name}\n" +
                         $"Type: {asset.FileType}\n" +
                         $"Size: {FormatFileSize(asset.FileSize)}\n" +
                         $"Dimensions: {asset.Width}x{asset.Height}\n" +
                         $"Created: {FormatDateTime(asset.CreatedAt)}";
            
            EditorUtility.DisplayDialog("Asset Info", info, "OK");
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }

        private string FormatDateTime(long unixTimestamp)
        {
            var dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).LocalDateTime;
            return dateTime.ToString("yyyy-MM-dd HH:mm");
        }

        private class AssetItem
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string FileType { get; set; }
            public long FileSize { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public long CreatedAt { get; set; }
        }
    }
}
