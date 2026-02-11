# 共享数据库部署指南

本文档介绍如何在公司级别部署共享艺术资源数据库，使多个Unity项目能够共享同一个资源库。

## 架构概述

```
公司共享位置 (\\CompanyServer\SharedAssets\)
├── art_asset_manager.db (共享数据库)
└── assets/ (资源文件)
    ├── textures/
    ├── models/
    └── ...

Unity项目A (C:\Projects\ProjectA\)
├── database-config.json (指向共享数据库)
└── Assets/
    └── ImportedAssets/ (从共享库导入的资源)

Unity项目B (C:\Projects\ProjectB\)
├── database-config.json (指向共享数据库)
└── Assets/
    └── ImportedAssets/ (从共享库导入的资源)
```

## 部署步骤

### 第一步：准备共享存储

1. 在公司文件服务器或网络驱动器上创建共享文件夹：
   ```
   \\CompanyServer\SharedAssets\
   ```

2. 设置适当的权限：
   - 所有开发人员：读取和写入权限
   - 只浏览用户：只读权限

3. 确保网络稳定性和访问速度

### 第二步：初始化共享数据库

在任意一台有权限的电脑上：

```powershell
# 克隆或下载项目代码
cd ArtAssetManager

# 构建项目
dotnet build

# 初始化共享数据库
cd src/ArtAssetManager.Console
dotnet run -- init --path "\\CompanyServer\SharedAssets\art_asset_manager.db"
```

### 第三步：导入初始资源

```powershell
# 导入公司现有的艺术资源
dotnet run -- import --path "\\CompanyServer\SharedAssets\art_asset_manager.db" --source "C:\CompanyAssets\Textures"

# 添加标签
dotnet run -- tag --path "\\CompanyServer\SharedAssets\art_asset_manager.db" --asset-id <id> --tags "ui,button,blue"
```

### 第四步：在Unity项目中配置

#### 方法1：使用安装脚本（推荐）

```powershell
# 在项目根目录运行
.\scripts\install-unity-extension.ps1 `
    -UnityProjectPath "C:\Projects\ProjectA" `
    -SharedDatabase "\\CompanyServer\SharedAssets\art_asset_manager.db" `
    -ProjectName "ProjectA"
```

#### 方法2：手动配置

1. 复制Unity扩展到项目
2. 在Unity项目根目录创建 `database-config.json`：

```json
{
  "DatabasePath": "\\\\CompanyServer\\SharedAssets\\art_asset_manager.db",
  "IsSharedDatabase": true,
  "ProjectName": "ProjectA",
  "ProjectPath": "Assets",
  "ConnectionTimeout": 30,
  "ReadOnly": false
}
```

3. 在Unity中打开 `Window > Art Asset Manager > Database Config`
4. 点击 "Test Connection" 验证
5. 点击 "Save Configuration"

### 第五步：团队使用

每个团队成员在自己的Unity项目中：

1. 安装Unity扩展（使用安装脚本或手动）
2. 配置指向共享数据库
3. 使用各个窗口浏览、导入、替换资源

## 使用场景

### 场景1：浏览和导入资源

1. 打开 `Window > Art Asset Manager > Asset Browser`
2. 搜索或按标签筛选资源
3. 点击 "Import" 导入到Unity项目
4. 资源会被复制到本地，并创建路由记录

### 场景2：上传新的风格化版本

1. 打开 `Window > Art Asset Manager > Style Upload`
2. 选择风格标签（如 cartoon、realistic）
3. 选择要上传的文件
4. 预览匹配结果
5. 批量上传

### 场景3：替换现有资源

1. 打开 `Window > Art Asset Manager > Asset Replacement`
2. 查看当前Unity项目中的资源
3. 选择可用的风格化版本
4. 点击 "Replace" 替换

### 场景4：查看使用历史

1. 在 Asset Replacement 窗口中点击 "View History"
2. 查看资源的替换历史
3. 了解其他项目的使用情况

## 数据隔离

共享数据库通过 `Projects` 表隔离不同Unity项目的数据：

```sql
-- 每个Unity项目有唯一的项目记录
SELECT * FROM Projects WHERE name = 'ProjectA';

-- 路由表记录哪个资源被哪个项目使用
SELECT * FROM UnityRoutes WHERE project_id = '<project_a_id>';

-- 风格迁移记录也关联到项目
SELECT * FROM StyleMigrations WHERE project_id = '<project_a_id>';
```

配置中的 `ProjectName` 字段用于标识项目：
- 每个Unity项目应使用唯一的项目名称
- 系统会自动创建或查找对应的项目记录
- 所有操作都会关联到当前项目

## 性能优化

### 网络优化

1. 使用高速局域网（千兆或更快）
2. 将数据库放在性能好的服务器上
3. 考虑使用SSD存储

### 查询优化

1. 数据库已创建必要的索引
2. 查询结果限制为100条
3. Unity扩展会缓存查询结果

### 文件传输优化

1. 资源导入时会复制到本地Unity项目
2. 只传输需要的文件
3. 支持批量操作减少网络往返

## 权限管理

### 读写权限

在配置文件中设置：

```json
{
  "ReadOnly": false  // 允许写入
}
```

或

```json
{
  "ReadOnly": true   // 只读模式
}
```

只读模式适合：
- 只需要浏览和导入资源的用户
- 防止误操作
- 临时访问

### 网络权限

在Windows文件服务器上设置：

1. 右键共享文件夹 > 属性 > 共享 > 高级共享
2. 设置权限：
   - 开发人员组：完全控制
   - 只读用户组：读取

## 备份策略

### 定期备份

```powershell
# 每日备份脚本
$source = "\\CompanyServer\SharedAssets\art_asset_manager.db"
$backup = "\\BackupServer\Backups\art_asset_manager_$(Get-Date -Format 'yyyyMMdd').db"
Copy-Item -Path $source -Destination $backup
```

### 版本控制

考虑使用Git LFS或类似工具管理：
- 数据库文件
- 配置文件
- 资源文件（可选）

## 故障排除

### 问题：无法连接到共享数据库

**症状**：Unity窗口显示 "Database not found" 或连接超时

**解决方案**：
1. 检查网络连接：
   ```powershell
   Test-Path "\\CompanyServer\SharedAssets\art_asset_manager.db"
   ```

2. 检查权限：
   - 在文件资源管理器中尝试访问
   - 确认有读写权限

3. 检查配置文件：
   - 确认路径格式正确（使用双反斜杠 `\\\\`）
   - 验证JSON格式

4. 增加超时时间：
   ```json
   {
     "ConnectionTimeout": 60
   }
   ```

### 问题：多个项目数据混淆

**症状**：看到其他项目的路由记录

**解决方案**：
1. 确认每个项目使用不同的 `ProjectName`
2. 检查数据库中的项目记录：
   ```sql
   SELECT * FROM Projects;
   ```
3. 清理错误的路由记录：
   ```sql
   DELETE FROM UnityRoutes WHERE project_id = '<wrong_project_id>';
   ```

### 问题：性能慢

**症状**：查询或导入资源很慢

**解决方案**：
1. 检查网络速度
2. 优化查询（减少结果数量）
3. 使用本地缓存
4. 考虑升级网络设备

### 问题：数据库锁定

**症状**：提示 "database is locked"

**解决方案**：
1. SQLite不支持高并发写入
2. 确保同一时间只有一个用户在写入
3. 考虑使用队列或锁机制
4. 对于高并发场景，考虑升级到PostgreSQL或MySQL

## 迁移到生产环境

### 从本地迁移到共享

1. 备份本地数据库
2. 复制到共享位置
3. 更新所有Unity项目的配置
4. 验证连接和功能

### 从共享迁移到本地

1. 复制共享数据库到本地
2. 更新配置文件：
   ```json
   {
     "DatabasePath": "art_asset_manager.db",
     "IsSharedDatabase": false
   }
   ```
3. 重启Unity编辑器

## 最佳实践

1. **命名规范**：使用清晰的项目名称（如 "Game_MainProject", "Game_DLC1"）
2. **标签管理**：建立统一的标签体系
3. **定期清理**：删除不再使用的资源和路由记录
4. **文档化**：记录资源的用途和来源
5. **权限控制**：根据角色分配适当的权限
6. **监控**：定期检查数据库大小和性能
7. **备份**：每日自动备份，保留多个版本

## 扩展性考虑

### 大规模部署

对于超过100个Unity项目或10000+资源的场景：

1. 考虑使用专业数据库（PostgreSQL、MySQL）
2. 实现缓存层（Redis）
3. 使用CDN分发资源文件
4. 实现搜索引擎（Elasticsearch）

### 云部署

可以将共享数据库部署到云端：

1. Azure Files / AWS EFS
2. 配置VPN或专线
3. 使用云数据库服务
4. 实现API网关

## 支持和反馈

如有问题或建议，请：
1. 查看项目README.md
2. 查看Unity扩展README
3. 提交Issue到项目仓库
