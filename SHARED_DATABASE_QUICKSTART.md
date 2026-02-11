# 共享数据库快速入门

## 5分钟快速设置

### 步骤1：准备共享位置（管理员）

在公司服务器上创建共享文件夹：
```
\\CompanyServer\SharedAssets\
```

### 步骤2：初始化数据库（管理员）

```powershell
cd ArtAssetManager
dotnet build
cd src/ArtAssetManager.Console
dotnet run -- init --path "\\CompanyServer\SharedAssets\art_asset_manager.db"
```

### 步骤3：导入资源（管理员）

```powershell
dotnet run -- import --path "\\CompanyServer\SharedAssets\art_asset_manager.db" --source "C:\CompanyAssets"
```

### 步骤4：在Unity项目中安装（每个开发者）

```powershell
cd ArtAssetManager
.\scripts\install-unity-extension.ps1 `
    -UnityProjectPath "C:\YourUnityProject" `
    -SharedDatabase "\\CompanyServer\SharedAssets\art_asset_manager.db" `
    -ProjectName "YourProjectName"
```

### 步骤5：在Unity中使用

1. 打开Unity项目
2. 等待编译完成
3. 打开 `Window > Art Asset Manager > Asset Browser`
4. 开始浏览和导入资源！

## 常用操作

### 浏览资源
`Window > Art Asset Manager > Asset Browser`

### 配置数据库
`Window > Art Asset Manager > Database Config`

### 替换资源
`Window > Art Asset Manager > Asset Replacement`

### 上传风格化版本
`Window > Art Asset Manager > Style Upload`

## 配置文件位置

```
YourUnityProject/
├── database-config.json          (当前配置)
├── database-config.example.json  (本地模式示例)
└── database-config.shared.example.json (共享模式示例)
```

## 本地模式 vs 共享模式

| 特性 | 本地模式 | 共享模式 |
|------|---------|---------|
| 数据库位置 | Unity项目根目录 | 公司共享位置 |
| 适用场景 | 单人开发 | 团队协作 |
| 资源共享 | 否 | 是 |
| 配置复杂度 | 简单 | 中等 |
| 网络依赖 | 无 | 有 |

## 切换模式

### 从本地切换到共享

1. 打开 `Window > Art Asset Manager > Database Config`
2. 点击 "Shared Mode" 按钮
3. 修改数据库路径为共享位置
4. 设置项目名称
5. 点击 "Save Configuration"

### 从共享切换到本地

1. 打开 `Window > Art Asset Manager > Database Config`
2. 点击 "Local Mode" 按钮
3. 点击 "Save Configuration"

## 故障排除

### 问题：无法连接到共享数据库

1. 检查网络连接
2. 在文件资源管理器中访问 `\\CompanyServer\SharedAssets\`
3. 确认有读写权限
4. 检查防火墙设置

### 问题：配置不生效

1. 确认配置文件在Unity项目根目录
2. 检查JSON格式是否正确
3. 重新打开Unity编辑器窗口

### 问题：看到其他项目的数据

1. 确认每个项目使用不同的 `ProjectName`
2. 在Database Config中验证项目名称

## 更多帮助

- 完整文档：`src/ArtAssetManager.Unity/README.md`
- 部署指南：`docs/shared-database-deployment.md`
- 功能总结：`docs/shared-database-feature-summary.md`
