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
            EditorUtility.DisplayDialog("Import Asset", 
                $"Import functionality will be implemented in Stage 2.2\n\nAsset: {asset.Name}", 
                "OK");
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
