using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace ArtAssetManager.Unity.Editor
{
    public class StyleUploadWindow : EditorWindow
    {
        private string _dbPath;
        private string _selectedStyleTag = "";
        private List<string> _availableStyles = new List<string>();
        private List<string> _selectedFiles = new List<string>();
        private Vector2 _scrollPosition;
        private string _statusMessage = "";
        private List<MatchResult> _matchResults = new List<MatchResult>();

        [MenuItem("Window/Art Asset Manager/Style Upload")]
        public static void ShowWindow()
        {
            var window = GetWindow<StyleUploadWindow>("Style Upload");
            window.minSize = new Vector2(700, 500);
            window.Show();
        }

        private void OnEnable()
        {
            FindDatabase();
            LoadStyles();
        }

        private void FindDatabase()
        {
            string currentPath = Application.dataPath;
            for (int i = 0; i < 5; i++)
            {
                string testPath = Path.Combine(currentPath, "art_asset_manager.db");
                if (File.Exists(testPath))
                {
                    _dbPath = testPath;
                    _statusMessage = "Database found";
                    return;
                }
                currentPath = Directory.GetParent(currentPath)?.FullName;
                if (currentPath == null) break;
            }
            _statusMessage = "Database not found";
        }

        private void LoadStyles()
        {
            if (string.IsNullOrEmpty(_dbPath)) return;

            _availableStyles.Clear();

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

                if (_availableStyles.Count > 0 && string.IsNullOrEmpty(_selectedStyleTag))
                {
                    _selectedStyleTag = _availableStyles[0];
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load styles: {ex.Message}");
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            GUILayout.Label("Upload Styled Assets", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
            EditorGUILayout.Space();

            // 选择风格
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Style Tag:", GUILayout.Width(80));
            
            if (_availableStyles.Count > 0)
            {
                int selectedIndex = Mathf.Max(0, _availableStyles.IndexOf(_selectedStyleTag));
                int newIndex = EditorGUILayout.Popup(selectedIndex, _availableStyles.ToArray());
                _selectedStyleTag = _availableStyles[newIndex];
            }
            else
            {
                GUILayout.Label("No styles available", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // 选择文件
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Files", GUILayout.Width(120)))
            {
                SelectFiles();
            }
            
            GUILayout.Label($"{_selectedFiles.Count} files selected");
            
            if (_selectedFiles.Count > 0 && GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                _selectedFiles.Clear();
                _matchResults.Clear();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // 文件列表
            if (_selectedFiles.Count > 0)
            {
                DrawFileList();
            }

            EditorGUILayout.Space();

            // 操作按钮
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = _selectedFiles.Count > 0 && !string.IsNullOrEmpty(_selectedStyleTag);
            if (GUILayout.Button("Preview Matches", GUILayout.Height(30)))
            {
                PreviewMatches();
            }

            GUI.enabled = _matchResults.Count > 0;
            if (GUILayout.Button("Upload All", GUILayout.Height(30)))
            {
                UploadAll();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            // 匹配结果
            if (_matchResults.Count > 0)
            {
                EditorGUILayout.Space();
                DrawMatchResults();
            }

            EditorGUILayout.EndVertical();
        }

        private void SelectFiles()
        {
            string path = EditorUtility.OpenFilePanelWithFilters(
                "Select Styled Assets",
                "",
                new[] { "Images", "png,jpg,jpeg", "All Files", "*" }
            );

            if (!string.IsNullOrEmpty(path) && !_selectedFiles.Contains(path))
            {
                _selectedFiles.Add(path);
                _statusMessage = $"Added: {Path.GetFileName(path)}";
            }
        }

        private void DrawFileList()
        {
            GUILayout.Label("Selected Files:", EditorStyles.boldLabel);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(150));
            
            for (int i = _selectedFiles.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(Path.GetFileName(_selectedFiles[i]));
                
                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    _selectedFiles.RemoveAt(i);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void PreviewMatches()
        {
            _matchResults.Clear();

            try
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();

                    foreach (var filePath in _selectedFiles)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(filePath);
                        var match = FindBestMatch(connection, fileName);
                        
                        _matchResults.Add(new MatchResult
                        {
                            FilePath = filePath,
                            FileName = fileName,
                            MatchedAssetId = match?.AssetId,
                            MatchedAssetName = match?.AssetName,
                            Similarity = match?.Similarity ?? 0,
                            IsExactMatch = match?.IsExactMatch ?? false
                        });
                    }
                }

                _statusMessage = $"Preview complete: {_matchResults.Count} files analyzed";
            }
            catch (Exception ex)
            {
                _statusMessage = $"Preview failed: {ex.Message}";
                Debug.LogError($"Failed to preview matches: {ex.Message}");
            }
        }

        private MatchInfo FindBestMatch(SqliteConnection connection, string fileName)
        {
            // 规范化文件名
            var normalized = NormalizeName(fileName);

            using (var command = connection.CreateCommand())
            {
                // 获取所有org资源
                command.CommandText = @"
                    SELECT a.id, a.name
                    FROM ArtAssets a
                    JOIN AssetTags at ON a.id = at.asset_id
                    JOIN Tags t ON at.tag_id = t.id
                    WHERE t.name = 'org' AND a.is_deleted = 0";

                var candidates = new List<(string Id, string Name)>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        candidates.Add((reader.GetString(0), reader.GetString(1)));
                    }
                }

                // 查找最佳匹配
                MatchInfo bestMatch = null;
                int bestDistance = int.MaxValue;

                foreach (var candidate in candidates)
                {
                    var candidateNormalized = NormalizeName(candidate.Name);
                    var distance = LevenshteinDistance(normalized, candidateNormalized);
                    
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        var maxLen = Math.Max(normalized.Length, candidateNormalized.Length);
                        var similarity = 1.0 - (double)distance / maxLen;

                        bestMatch = new MatchInfo
                        {
                            AssetId = candidate.Id,
                            AssetName = candidate.Name,
                            Distance = distance,
                            Similarity = similarity,
                            IsExactMatch = distance == 0
                        };
                    }
                }

                // 如果距离太大，返回null
                if (bestMatch != null && bestMatch.Distance > 3 && !bestMatch.IsExactMatch)
                {
                    return null;
                }

                return bestMatch;
            }
        }

        private string NormalizeName(string fileName)
        {
            var normalized = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
            
            // 移除常见后缀
            var suffixes = new[] { "_cartoon", "_realistic", "_pixel", "_org", "_original" };
            foreach (var suffix in suffixes)
            {
                if (normalized.EndsWith(suffix))
                {
                    normalized = normalized.Substring(0, normalized.Length - suffix.Length);
                }
            }

            return normalized;
        }

        private int LevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source)) return string.IsNullOrEmpty(target) ? 0 : target.Length;
            if (string.IsNullOrEmpty(target)) return source.Length;

            var sourceLength = source.Length;
            var targetLength = target.Length;
            var distance = new int[sourceLength + 1, targetLength + 1];

            for (var i = 0; i <= sourceLength; i++) distance[i, 0] = i;
            for (var j = 0; j <= targetLength; j++) distance[0, j] = j;

            for (var i = 1; i <= sourceLength; i++)
            {
                for (var j = 1; j <= targetLength; j++)
                {
                    var cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                    distance[i, j] = Math.Min(
                        Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                        distance[i - 1, j - 1] + cost);
                }
            }

            return distance[sourceLength, targetLength];
        }

        private void DrawMatchResults()
        {
            GUILayout.Label("Match Results:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            foreach (var result in _matchResults)
            {
                EditorGUILayout.BeginHorizontal();
                
                GUILayout.Label(result.FileName, GUILayout.Width(200));
                
                if (result.MatchedAssetId != null)
                {
                    var color = result.IsExactMatch ? Color.green : 
                               (result.Similarity > 0.7 ? Color.yellow : Color.red);
                    
                    var oldColor = GUI.color;
                    GUI.color = color;
                    GUILayout.Label($"→ {result.MatchedAssetName} ({result.Similarity:P0})", GUILayout.Width(300));
                    GUI.color = oldColor;
                }
                else
                {
                    GUI.color = Color.red;
                    GUILayout.Label("No match found", GUILayout.Width(300));
                    GUI.color = Color.white;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void UploadAll()
        {
            if (!EditorUtility.DisplayDialog("Confirm Upload",
                $"Upload {_matchResults.Count} styled assets?\n\n" +
                $"Style: {_selectedStyleTag}\n\n" +
                $"This will import assets and create style migration records.",
                "Upload", "Cancel"))
            {
                return;
            }

            int successCount = 0;
            int failCount = 0;

            try
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();

                    foreach (var result in _matchResults)
                    {
                        if (result.MatchedAssetId == null)
                        {
                            failCount++;
                            continue;
                        }

                        try
                        {
                            // 导入资源到数据库
                            var assetId = ImportAssetToDatabase(connection, result.FilePath);
                            
                            // 创建风格迁移记录
                            CreateStyleMigration(connection, result.MatchedAssetId, assetId, _selectedStyleTag);
                            
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Failed to upload {result.FileName}: {ex.Message}");
                            failCount++;
                        }
                    }
                }

                _statusMessage = $"Upload complete: {successCount} succeeded, {failCount} failed";
                EditorUtility.DisplayDialog("Upload Complete", 
                    $"Upload complete!\n\nSucceeded: {successCount}\nFailed: {failCount}", 
                    "OK");

                // 清空
                _selectedFiles.Clear();
                _matchResults.Clear();
            }
            catch (Exception ex)
            {
                _statusMessage = $"Upload failed: {ex.Message}";
                EditorUtility.DisplayDialog("Error", $"Upload failed:\n{ex.Message}", "OK");
            }
        }

        private string ImportAssetToDatabase(SqliteConnection connection, string filePath)
        {
            // 这里简化实现，实际应该调用AssetService
            var assetId = Guid.NewGuid().ToString();
            var fileName = Path.GetFileName(filePath);
            var fileType = Path.GetExtension(filePath).TrimStart('.');
            var fileSize = new FileInfo(filePath).Length;
            var fileHash = ComputeFileHash(filePath);
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO ArtAssets (id, name, file_path, file_type, file_size, file_hash, 
                                          created_at, updated_at, is_deleted)
                    VALUES (@Id, @Name, @FilePath, @FileType, @FileSize, @FileHash, 
                            @CreatedAt, @UpdatedAt, 0)";

                command.Parameters.AddWithValue("@Id", assetId);
                command.Parameters.AddWithValue("@Name", fileName);
                command.Parameters.AddWithValue("@FilePath", filePath);
                command.Parameters.AddWithValue("@FileType", fileType);
                command.Parameters.AddWithValue("@FileSize", fileSize);
                command.Parameters.AddWithValue("@FileHash", fileHash);
                command.Parameters.AddWithValue("@CreatedAt", now);
                command.Parameters.AddWithValue("@UpdatedAt", now);

                command.ExecuteNonQuery();
            }

            return assetId;
        }

        private void CreateStyleMigration(SqliteConnection connection, string originalAssetId, 
                                         string styledAssetId, string styleTag)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO StyleMigrations (id, original_asset_id, styled_asset_id, style_tag, 
                                                created_at, created_by)
                    VALUES (@Id, @OriginalAssetId, @StyledAssetId, @StyleTag, @CreatedAt, @CreatedBy)";

                command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                command.Parameters.AddWithValue("@OriginalAssetId", originalAssetId);
                command.Parameters.AddWithValue("@StyledAssetId", styledAssetId);
                command.Parameters.AddWithValue("@StyleTag", styleTag);
                command.Parameters.AddWithValue("@CreatedAt", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                command.Parameters.AddWithValue("@CreatedBy", "Unity Editor");

                command.ExecuteNonQuery();
            }
        }

        private string ComputeFileHash(string filePath)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private class MatchResult
        {
            public string FilePath { get; set; }
            public string FileName { get; set; }
            public string MatchedAssetId { get; set; }
            public string MatchedAssetName { get; set; }
            public double Similarity { get; set; }
            public bool IsExactMatch { get; set; }
        }

        private class MatchInfo
        {
            public string AssetId { get; set; }
            public string AssetName { get; set; }
            public int Distance { get; set; }
            public double Similarity { get; set; }
            public bool IsExactMatch { get; set; }
        }
    }
}
