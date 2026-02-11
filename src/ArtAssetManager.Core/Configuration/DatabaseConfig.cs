using System;
using System.IO;
using System.Text.Json;

namespace ArtAssetManager.Core.Configuration
{
    /// <summary>
    /// 数据库配置
    /// </summary>
    public class DatabaseConfig
    {
        /// <summary>
        /// 数据库路径（支持绝对路径、相对路径、UNC路径）
        /// </summary>
        public string DatabasePath { get; set; } = "art_asset_manager.db";

        /// <summary>
        /// 是否为共享数据库（公司级）
        /// </summary>
        public bool IsSharedDatabase { get; set; } = false;

        /// <summary>
        /// 当前Unity项目名称（用于隔离不同项目的数据）
        /// </summary>
        public string ProjectName { get; set; } = "DefaultProject";

        /// <summary>
        /// 当前Unity项目路径
        /// </summary>
        public string ProjectPath { get; set; } = "Assets";

        /// <summary>
        /// 连接超时（秒）
        /// </summary>
        public int ConnectionTimeout { get; set; } = 30;

        /// <summary>
        /// 是否启用只读模式
        /// </summary>
        public bool ReadOnly { get; set; } = false;

        /// <summary>
        /// 从配置文件加载
        /// </summary>
        public static DatabaseConfig LoadFromFile(string configPath)
        {
            if (!File.Exists(configPath))
            {
                return new DatabaseConfig();
            }

            try
            {
                var json = File.ReadAllText(configPath);
                return JsonSerializer.Deserialize<DatabaseConfig>(json) ?? new DatabaseConfig();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load database config: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 保存到配置文件
        /// </summary>
        public void SaveToFile(string configPath)
        {
            try
            {
                var directory = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save database config: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取完整的数据库路径
        /// </summary>
        public string GetFullDatabasePath()
        {
            // 如果是绝对路径或UNC路径，直接返回
            if (Path.IsPathRooted(DatabasePath) || DatabasePath.StartsWith(@"\\"))
            {
                return DatabasePath;
            }

            // 相对路径，相对于当前工作目录
            return Path.GetFullPath(DatabasePath);
        }

        /// <summary>
        /// 获取连接字符串
        /// </summary>
        public string GetConnectionString()
        {
            var dbPath = GetFullDatabasePath();
            var connectionString = $"Data Source={dbPath};";

            if (ConnectionTimeout > 0)
            {
                connectionString += $"Connection Timeout={ConnectionTimeout};";
            }

            if (ReadOnly)
            {
                connectionString += "Mode=ReadOnly;";
            }

            return connectionString;
        }

        /// <summary>
        /// 验证配置
        /// </summary>
        public (bool IsValid, string ErrorMessage) Validate()
        {
            if (string.IsNullOrWhiteSpace(DatabasePath))
            {
                return (false, "Database path is required");
            }

            if (string.IsNullOrWhiteSpace(ProjectName))
            {
                return (false, "Project name is required");
            }

            if (IsSharedDatabase)
            {
                var dbPath = GetFullDatabasePath();
                
                // 检查是否为UNC路径或网络路径
                if (!dbPath.StartsWith(@"\\") && !Path.IsPathRooted(dbPath))
                {
                    return (false, "Shared database should use absolute path or UNC path");
                }

                // 检查目录是否存在
                var directory = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    return (false, $"Database directory does not exist: {directory}");
                }
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// 创建默认配置（本地模式）
        /// </summary>
        public static DatabaseConfig CreateLocalConfig(string projectName, string projectPath)
        {
            return new DatabaseConfig
            {
                DatabasePath = "art_asset_manager.db",
                IsSharedDatabase = false,
                ProjectName = projectName,
                ProjectPath = projectPath
            };
        }

        /// <summary>
        /// 创建共享配置（公司级）
        /// </summary>
        public static DatabaseConfig CreateSharedConfig(string sharedDbPath, string projectName, string projectPath)
        {
            return new DatabaseConfig
            {
                DatabasePath = sharedDbPath,
                IsSharedDatabase = true,
                ProjectName = projectName,
                ProjectPath = projectPath
            };
        }
    }
}
