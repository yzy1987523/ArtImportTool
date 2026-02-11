# ArtAssetManager Unity 扩展

Unity编辑器扩展，用于在Unity中浏览和导入艺术资源。

## 安装步骤

### 1. 复制文件到Unity项目

将整个 `ArtAssetManager.Unity` 文件夹复制到你的Unity项目的 `Assets/` 目录下：

```
YourUnityProject/
  Assets/
    ArtAssetManager.Unity/
      Editor/
        AssetBrowserWindow.cs
      ArtAssetManager.Unity.asmdef
      README.md
```

### 2. 复制SQLite依赖

需要将以下DLL文件复制到Unity项目的 `Assets/Plugins/` 目录：

```
YourUnityProject/
  Assets/
    Plugins/
      Microsoft.Data.Sqlite.dll
      SQLitePCLRaw.core.dll
      SQLitePCLRaw.provider.e_sqlite3.dll
      SQLitePCLRaw.batteries_v2.dll
```

这些DLL可以从 `ArtAssetManager.Core` 项目的输出目录获取：
```
src/ArtAssetManager.Core/bin/Debug/net6.0/
```

### 3. 复制数据库文件

将 `art_asset_manager.db` 文件复制到Unity项目的根目录（与Assets文件夹同级）：

```
YourUnityProject/
  art_asset_manager.db
  Assets/
  ProjectSettings/
  ...
```

或者，也可以放在Unity项目的上级目录，扩展会自动向上查找。

## 使用方法

### 配置数据库

在Unity编辑器中，选择菜单：
```
Window > Art Asset Manager > Database Config
```

#### 本地模式（默认）
- 数据库文件位于Unity项目根目录
- 每个Unity项目有独立的数据库
- 适合单人开发或小型项目

#### 共享模式（公司级）
- 数据库文件位于公司共享位置（网络驱动器或服务器）
- 多个Unity项目共享同一个数据库
- 通过项目名称隔离不同项目的数据
- 适合团队协作和资源复用

配置文件位置：`YourUnityProject/database-config.json`

### 打开资源浏览器

在Unity编辑器中，选择菜单：
```
Window > Art Asset Manager > Asset Browser
```

### 打开资源替换窗口

```
Window > Art Asset Manager > Asset Replacement
```

### 打开风格化上传窗口

```
Window > Art Asset Manager > Style Upload
```

### 功能说明

#### 1. 资源浏览器（Asset Browser）

1. **搜索资源**
   - 在搜索框中输入资源名称进行搜索
   - 支持模糊匹配

2. **按标签筛选**
   - 使用Tag下拉菜单选择标签
   - 只显示包含该标签的资源

3. **查看资源信息**
   - 点击"Info"按钮查看资源详细信息
   - 包括ID、名称、类型、大小、尺寸、创建时间

4. **导入资源**
   - 点击"Import"按钮将资源导入到Unity项目
   - 选择导入路径
   - 自动创建路由表记录

5. **刷新列表**
   - 点击"Refresh"按钮重新加载资源列表

#### 2. 资源替换窗口（Asset Replacement）

1. **查看Unity资源**
   - 显示所有已导入Unity的资源
   - 显示每个资源的可用风格化版本

2. **按风格筛选**
   - 使用风格下拉菜单筛选
   - 只显示特定风格的版本

3. **替换资源**
   - 点击"Replace"按钮替换资源
   - 自动更新Unity文件
   - 自动更新路由表

4. **查看历史**
   - 点击"View History"查看替换历史
   - 显示最近10次操作

#### 3. 风格化上传窗口（Style Upload）

1. **选择风格标签**
   - 从下拉菜单选择风格（cartoon、realistic、pixel等）

2. **选择文件**
   - 点击"Select Files"选择要上传的文件
   - 支持多次选择

3. **预览匹配**
   - 点击"Preview Matches"查看匹配结果
   - 绿色=精确匹配，黄色=高相似度，红色=低相似度

4. **批量上传**
   - 点击"Upload All"批量上传所有文件
   - 自动导入到数据库
   - 自动创建风格迁移记录

## 当前状态

- ✅ 资源浏览窗口（AssetBrowserWindow）
- ✅ 搜索功能
- ✅ 标签筛选
- ✅ 资源信息显示
- ✅ 资源导入功能
- ✅ 路由表维护
- ✅ 资源替换窗口（AssetReplacementWindow）
- ✅ 风格化资源上传（StyleUploadWindow）
- ✅ 批量操作支持
- ✅ 历史记录查看
- ✅ 数据库配置窗口（DatabaseConfigWindow）
- ✅ 共享数据库支持（公司级）
- ✅ 本地/共享模式切换

## 共享数据库部署指南

### 场景1：公司级共享数据库

适用于多个Unity项目需要共享艺术资源的场景。

#### 步骤1：准备共享位置
在公司网络驱动器或文件服务器上创建共享文件夹：
```
\\CompanyServer\SharedAssets\
```

#### 步骤2：初始化共享数据库
在任意一台电脑上运行控制台工具初始化数据库：
```powershell
cd src/ArtAssetManager.Console
dotnet run -- init --path "\\CompanyServer\SharedAssets\art_asset_manager.db"
```

#### 步骤3：导入资源到共享数据库
```powershell
dotnet run -- import --path "\\CompanyServer\SharedAssets\art_asset_manager.db" --source "C:\ArtAssets"
```

#### 步骤4：在Unity项目中配置
在每个Unity项目中：
1. 打开 `Window > Art Asset Manager > Database Config`
2. 选择 "Shared Database (Company-level)"
3. 设置数据库路径：`\\CompanyServer\SharedAssets\art_asset_manager.db`
4. 设置项目名称（用于隔离不同项目）：如 "ProjectA"、"ProjectB"
5. 点击 "Test Connection" 验证连接
6. 点击 "Save Configuration"

#### 步骤5：使用共享资源
现在所有Unity项目都可以：
- 浏览共享资源库
- 导入资源到各自项目
- 上传新的风格化版本
- 查看其他项目的资源使用情况

### 场景2：本地开发模式

适用于单人开发或不需要共享的场景。

#### 步骤1：初始化本地数据库
```powershell
cd YourUnityProject
dotnet run --project ../ArtAssetManager.Console -- init
```

#### 步骤2：在Unity中使用
1. 打开 `Window > Art Asset Manager > Database Config`
2. 使用默认的本地配置
3. 或点击 "Local Mode" 快速设置

### 配置文件示例

#### 本地模式配置
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

#### 共享模式配置
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

### 权限管理

#### 读写权限
- 默认所有用户都有读写权限
- 可以通过设置 `ReadOnly: true` 限制为只读模式
- 适合只需要浏览和导入资源的用户

#### 网络权限
确保所有用户对共享文件夹有适当的权限：
- 读取权限：可以浏览和导入资源
- 写入权限：可以上传新资源和创建风格化版本

### 性能优化

#### 网络延迟
- 共享数据库通过网络访问，可能有延迟
- 建议使用高速局域网
- 可以调整 `ConnectionTimeout` 参数

#### 缓存策略
- Unity扩展会缓存查询结果
- 点击 "Refresh" 按钮更新缓存
- 资源文件导入后会复制到本地Unity项目

### 故障排除

#### 问题：无法连接到共享数据库

**解决方案**：
1. 检查网络连接和共享文件夹权限
2. 确认UNC路径格式正确：`\\Server\Share\file.db`
3. 尝试在文件资源管理器中直接访问该路径
4. 检查防火墙设置

#### 问题：多个项目数据混淆

**解决方案**：
1. 确保每个Unity项目使用不同的 `ProjectName`
2. 检查路由表中的 `project_id` 字段
3. 使用数据库配置窗口的 "Test Connection" 验证配置

#### 问题：配置文件不生效

**解决方案**：
1. 确认配置文件位于Unity项目根目录：`YourUnityProject/database-config.json`
2. 检查JSON格式是否正确
3. 重新打开Unity编辑器窗口
4. 查看Unity Console中的错误信息

## 技术说明

### 数据库连接

扩展使用 `Microsoft.Data.Sqlite` 直接连接SQLite数据库。数据库查找逻辑：
1. 从Unity项目的 `Application.dataPath`（Assets目录）开始
2. 向上查找最多5级目录
3. 查找名为 `art_asset_manager.db` 的文件

### 性能优化

- 查询结果限制为100条
- 使用索引优化查询性能
- 支持按标签和名称筛选

### 兼容性

- Unity 2021.3 LTS 或更高版本
- .NET Standard 2.1
- SQLite 3.x

## 故障排除

### 问题：窗口显示"Database not found"

**解决方案**：
1. 确认 `art_asset_manager.db` 文件存在
2. 确认文件位置在Unity项目根目录或上级目录
3. 检查文件权限

### 问题：编译错误"找不到Microsoft.Data.Sqlite"

**解决方案**：
1. 确认已复制所有SQLite DLL到 `Assets/Plugins/`
2. 检查 `.asmdef` 文件中的 `precompiledReferences` 配置
3. 重启Unity编辑器

### 问题：资源列表为空

**解决方案**：
1. 使用控制台工具导入一些测试资源
2. 点击"Refresh"按钮刷新列表
3. 检查数据库中是否有数据

## 下一步开发

- [ ] 添加资源预览功能（缩略图）
- [ ] 添加分页功能
- [ ] 添加排序功能
- [ ] 添加多选功能
- [ ] 添加拖拽导入功能
- [ ] 性能优化和压力测试

