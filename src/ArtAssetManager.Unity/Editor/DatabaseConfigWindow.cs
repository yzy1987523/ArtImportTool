using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using ArtAssetManager.Core.Configuration;

namespace ArtAssetManager.Unity.Editor
{
    public class DatabaseConfigWindow : EditorWindow
    {
        private DatabaseConfig _config;
        private string _configPath;
        private string _statusMessage = "";
        private MessageType _messageType = MessageType.Info;
        private Vector2 _scrollPosition;

        [MenuItem("Window/Art Asset Manager/Database Config")]
        public static void ShowWindow()
        {
            var window = GetWindow<DatabaseConfigWindow>("Database Config");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnEnable()
        {
            _configPath = Path.Combine(Application.dataPath, "..", "database-config.json");
            LoadConfig();
        }

        private void LoadConfig()
        {
            if (File.Exists(_configPath))
            {
                try
                {
                    _config = DatabaseConfig.LoadFromFile(_configPath);
                    _statusMessage = "Configuration loaded successfully";
                    _messageType = MessageType.Info;
                }
                catch (Exception ex)
                {
                    _config = new DatabaseConfig();
                    _statusMessage = $"Failed to load config: {ex.Message}";
                    _messageType = MessageType.Error;
                }
            }
            else
            {
                _config = DatabaseConfig.CreateLocalConfig(
                    Application.productName,
                    Application.dataPath
                );
                _statusMessage = "No config file found. Using default local configuration.";
                _messageType = MessageType.Warning;
            }
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            EditorGUILayout.BeginVertical();

            GUILayout.Label("Database Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 状态消息
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _messageType);
                EditorGUILayout.Space();
            }

            // 配置模式选择
            EditorGUILayout.LabelField("Configuration Mode", EditorStyles.boldLabel);
            bool isShared = EditorGUILayout.Toggle("Shared Database (Company-level)", _config.IsSharedDatabase);
            if (isShared != _config.IsSharedDatabase)
            {
                _config.IsSharedDatabase = isShared;
                if (isShared && _config.DatabasePath == "art_asset_manager.db")
                {
                    _config.DatabasePath = @"\\CompanyServer\SharedAssets\art_asset_manager.db";
                }
            }
            EditorGUILayout.Space();

            // 数据库路径
            EditorGUILayout.LabelField("Database Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _config.DatabasePath = EditorGUILayout.TextField("Database Path", _config.DatabasePath);
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                BrowseDatabasePath();
            }
            EditorGUILayout.EndHorizontal();

            if (_config.IsSharedDatabase)
            {
                EditorGUILayout.HelpBox(
                    "For shared databases, use absolute paths or UNC paths:\n" +
                    "• Absolute: C:\\CompanyAssets\\art_asset_manager.db\n" +
                    "• UNC: \\\\CompanyServer\\SharedAssets\\art_asset_manager.db",
                    MessageType.Info
                );
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "For local databases, use relative paths:\n" +
                    "• art_asset_manager.db (in project root)\n" +
                    "• ../art_asset_manager.db (in parent directory)",
                    MessageType.Info
                );
            }
            EditorGUILayout.Space();

            // 项目信息
            EditorGUILayout.LabelField("Project Settings", EditorStyles.boldLabel);
            _config.ProjectName = EditorGUILayout.TextField("Project Name", _config.ProjectName);
            _config.ProjectPath = EditorGUILayout.TextField("Project Path", _config.ProjectPath);
            EditorGUILayout.Space();

            // 高级设置
            EditorGUILayout.LabelField("Advanced Settings", EditorStyles.boldLabel);
            _config.ConnectionTimeout = EditorGUILayout.IntField("Connection Timeout (seconds)", _config.ConnectionTimeout);
            _config.ReadOnly = EditorGUILayout.Toggle("Read Only Mode", _config.ReadOnly);
            EditorGUILayout.Space();

            // 验证和预览
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            var validation = _config.Validate();
            if (validation.IsValid)
            {
                EditorGUILayout.HelpBox("✓ Configuration is valid", MessageType.Info);
                EditorGUILayout.LabelField("Full Database Path:", _config.GetFullDatabasePath());
                EditorGUILayout.LabelField("Connection String:", _config.GetConnectionString());
            }
            else
            {
                EditorGUILayout.HelpBox($"✗ {validation.ErrorMessage}", MessageType.Error);
            }
            EditorGUILayout.Space();

            // 操作按钮
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = validation.IsValid;
            if (GUILayout.Button("Save Configuration", GUILayout.Height(30)))
            {
                SaveConfig();
            }
            GUI.enabled = true;

            if (GUILayout.Button("Reset to Default", GUILayout.Height(30)))
            {
                ResetToDefault();
            }

            if (GUILayout.Button("Test Connection", GUILayout.Height(30)))
            {
                TestConnection();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // 快速设置模板
            EditorGUILayout.LabelField("Quick Setup Templates", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Local Mode"))
            {
                _config = DatabaseConfig.CreateLocalConfig(
                    Application.productName,
                    Application.dataPath
                );
                _statusMessage = "Switched to local mode";
                _messageType = MessageType.Info;
            }

            if (GUILayout.Button("Shared Mode"))
            {
                _config = DatabaseConfig.CreateSharedConfig(
                    @"\\CompanyServer\SharedAssets\art_asset_manager.db",
                    Application.productName,
                    Application.dataPath
                );
                _statusMessage = "Switched to shared mode (update the path)";
                _messageType = MessageType.Warning;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void BrowseDatabasePath()
        {
            string path = EditorUtility.OpenFilePanel(
                "Select Database File",
                "",
                "db"
            );

            if (!string.IsNullOrEmpty(path))
            {
                _config.DatabasePath = path;
            }
        }

        private void SaveConfig()
        {
            try
            {
                _config.SaveToFile(_configPath);
                _statusMessage = $"Configuration saved successfully to {_configPath}";
                _messageType = MessageType.Info;
                
                // 刷新其他窗口
                RefreshOtherWindows();
            }
            catch (Exception ex)
            {
                _statusMessage = $"Failed to save configuration: {ex.Message}";
                _messageType = MessageType.Error;
            }
        }

        private void ResetToDefault()
        {
            if (EditorUtility.DisplayDialog(
                "Reset Configuration",
                "Are you sure you want to reset to default configuration?",
                "Yes", "No"))
            {
                _config = DatabaseConfig.CreateLocalConfig(
                    Application.productName,
                    Application.dataPath
                );
                _statusMessage = "Configuration reset to default";
                _messageType = MessageType.Info;
            }
        }

        private void TestConnection()
        {
            try
            {
                var dbPath = _config.GetFullDatabasePath();
                
                if (!File.Exists(dbPath))
                {
                    _statusMessage = $"Database file not found: {dbPath}";
                    _messageType = MessageType.Error;
                    return;
                }

                using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(_config.GetConnectionString()))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table'";
                        var tableCount = Convert.ToInt32(command.ExecuteScalar());
                        
                        _statusMessage = $"✓ Connection successful! Found {tableCount} tables in database.";
                        _messageType = MessageType.Info;
                    }
                }
            }
            catch (Exception ex)
            {
                _statusMessage = $"Connection failed: {ex.Message}";
                _messageType = MessageType.Error;
            }
        }

        private void RefreshOtherWindows()
        {
            // 通知其他窗口重新加载数据库配置
            var browserWindow = GetWindow<AssetBrowserWindow>(false, "", false);
            if (browserWindow != null)
            {
                browserWindow.Close();
            }

            var replacementWindow = GetWindow<AssetReplacementWindow>(false, "", false);
            if (replacementWindow != null)
            {
                replacementWindow.Close();
            }

            var styleWindow = GetWindow<StyleUploadWindow>(false, "", false);
            if (styleWindow != null)
            {
                styleWindow.Close();
            }
        }
    }
}
