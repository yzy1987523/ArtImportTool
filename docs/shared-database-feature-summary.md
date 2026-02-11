# 共享数据库功能总结

## 概述

成功实现了公司级共享数据库功能，使多个Unity项目能够共享同一个艺术资源数据库。

## 实现的功能

### 1. 数据库配置系统

**DatabaseConfig类** (`src/ArtAssetManager.Core/Configuration/DatabaseConfig.cs`)
- 支持本地/共享模式切换
- 支持绝对路径、相对路径、UNC路径
- 项目名称和路径配置（用于数据隔离）
- 连接超时和只读模式设置
- 配置验证和连接字符串生成
- JSON格式配置文件加载/保存

### 2. Unity编辑器集成

**更新的窗口**：
- `AssetBrowserWindow.cs` - 资源浏览器
- `AssetReplacementWindow.cs` - 资源替换窗口
- `StyleUploadWindow.cs` - 风格化上传窗口

所有窗口现在都：
- 优先从配置文件加载数据库路径
- 显示共享/本地模式状态
- 回退到默认查找逻辑
- 提供友好的错误提示

**新增窗口**：
- `DatabaseConfigWindow.cs` - 数据库配置管理窗口
  - 可视化配置界面
  - 本地/共享模式切换
  - 路径浏览和验证
  - 连接测试功能
  - 快速模板设置

### 3. 数据库初始化器增强

**DatabaseInitializer** (`src/ArtAssetManager.Core/Database/DatabaseInitializer.cs`)
- 新增 `FromConfig()` 静态方法
- 支持从配置文件创建初始化器
- 自动验证配置有效性

### 4. 安装脚本增强

**install-unity-extension.ps1** (`scripts/install-unity-extension.ps1`)
- 支持 `-SharedDatabase` 参数指定共享数据库路径
- 支持 `-ProjectName` 参数指定项目名称
- 自动生成配置文件（本地或共享模式）
- 复制配置示例文件

### 5. 配置文件示例

**database-config.example.json** - 本地模式示例
```json
{
  "DatabasePath": "art_asset_manager.db",
  "IsSharedDatabase": false,
  "ProjectName": "MyUnityProject",
  "ProjectPath": "Assets",
  "ConnectionTimeout": 30,
  "ReadOnly": false
}
```

**database-config.shared.example.json** - 共享模式示例
```json
{
  "DatabasePath": "\\\\CompanyServer\\SharedAssets\\art_asset_manager.db",
  "IsSharedDatabase": true,
  "ProjectName": "MyUnityProject",
  "ProjectPath": "Assets",
  "ConnectionTimeout": 30,
  "ReadOnly": false
}
```

### 6. 文档

**更新的文档**：
- `src/ArtAssetManager.Unity/README.md` - 添加共享数据库使用说明

**新增文档**：
- `docs/shared-database-deployment.md` - 完整的部署指南
  - 架构概述
  - 详细部署步骤
  - 使用场景说明
  - 数据隔离机制
  - 性能优化建议
  - 权限管理
  - 备份策略
  - 故障排除
  - 最佳实践

## 使用方式

### 场景1：本地开发模式

```powershell
# 安装到Unity项目（本地模式）
.\scripts\install-unity-extension.ps1 -UnityProjectPath "C:\Projects\MyGame"
```

### 场景2：共享数据库模式

```powershell
# 安装到Unity项目（共享模式）
.\scripts\install-unity-extension.ps1 `
    -UnityProjectPath "C:\Projects\ProjectA" `
    -SharedDatabase "\\CompanyServer\SharedAssets\art_asset_manager.db" `
    -ProjectName "ProjectA"
```

### 场景3：在Unity中配置

1. 打开 `Window > Art Asset Manager > Database Config`
2. 选择模式（本地/共享）
3. 设置数据库路径
4. 设置项目名称
5. 测试连接
6. 保存配置

## 技术特性

### 数据隔离

- 通过 `Projects` 表区分不同Unity项目
- 每个项目有唯一的项目名称
- 路由表和风格迁移记录都关联到项目
- 确保多项目共享数据库时数据不混淆

### 配置优先级

1. 配置文件 (`database-config.json`)
2. 默认查找逻辑（向上查找5级目录）
3. 错误提示引导用户配置

### 路径支持

- 相对路径：`art_asset_manager.db`
- 绝对路径：`C:\CompanyAssets\art_asset_manager.db`
- UNC路径：`\\CompanyServer\SharedAssets\art_asset_manager.db`

### 验证机制

- 路径格式验证
- 目录存在性检查
- 项目名称必填验证
- 连接测试功能

## 文件清单

### 新增文件
1. `src/ArtAssetManager.Core/Configuration/DatabaseConfig.cs`
2. `src/ArtAssetManager.Unity/Editor/DatabaseConfigWindow.cs`
3. `database-config.example.json`
4. `database-config.shared.example.json`
5. `docs/shared-database-deployment.md`
6. `docs/shared-database-feature-summary.md`

### 修改文件
1. `src/ArtAssetManager.Unity/Editor/AssetBrowserWindow.cs`
2. `src/ArtAssetManager.Unity/Editor/AssetReplacementWindow.cs`
3. `src/ArtAssetManager.Unity/Editor/StyleUploadWindow.cs`
4. `src/ArtAssetManager.Core/Database/DatabaseInitializer.cs`
5. `scripts/install-unity-extension.ps1`
6. `src/ArtAssetManager.Unity/README.md`

## 测试建议

### 单元测试
- DatabaseConfig加载/保存
- 路径验证逻辑
- 连接字符串生成

### 集成测试
- 本地模式安装和使用
- 共享模式安装和使用
- 多项目数据隔离
- 配置文件切换

### 手动测试
1. 本地模式完整流程
2. 共享模式完整流程
3. 配置窗口所有功能
4. 错误处理和提示
5. 多项目并发访问

## 性能考虑

### 网络延迟
- 共享数据库通过网络访问
- 建议使用高速局域网
- 可调整连接超时参数

### 缓存策略
- Unity扩展缓存查询结果
- 资源文件导入后复制到本地
- 减少网络往返次数

### 并发控制
- SQLite支持多读单写
- 适合中小规模团队
- 大规模场景考虑升级到PostgreSQL/MySQL

## 后续优化建议

### 短期优化
1. 添加配置文件热重载
2. 实现配置迁移工具
3. 添加更多配置验证规则
4. 优化错误提示信息

### 中期优化
1. 实现配置版本管理
2. 添加配置同步功能
3. 支持多数据库配置
4. 实现配置加密

### 长期优化
1. 支持云数据库（Azure SQL、AWS RDS）
2. 实现分布式缓存（Redis）
3. 添加API网关层
4. 支持微服务架构

## 兼容性

- Unity 2021.3 LTS 或更高版本
- .NET 6.0 或更高版本
- Windows 10/11
- SQLite 3.x
- 支持UNC路径和网络驱动器

## 已知限制

1. SQLite不支持高并发写入
2. 网络延迟影响性能
3. 配置文件需要手动同步
4. 不支持实时协作编辑

## 总结

成功实现了完整的公司级共享数据库功能，包括：
- 灵活的配置系统
- 友好的Unity编辑器集成
- 完善的文档和示例
- 便捷的安装脚本
- 数据隔离机制

该功能使团队能够：
- 共享艺术资源库
- 避免资源重复
- 统一资源管理
- 提高协作效率
