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

### 打开资源浏览器

在Unity编辑器中，选择菜单：
```
Window > Art Asset Manager > Asset Browser
```

### 功能说明

1. **搜索资源**
   - 在搜索框中输入资源名称进行搜索
   - 支持模糊匹配

2. **按标签筛选**
   - 使用Tag下拉菜单选择标签
   - 只显示包含该标签的资源

3. **查看资源信息**
   - 点击"Info"按钮查看资源详细信息
   - 包括ID、名称、类型、大小、尺寸、创建时间

4. **导入资源**（待实现）
   - 点击"Import"按钮将资源导入到Unity项目
   - 当前版本会显示提示信息

5. **刷新列表**
   - 点击"Refresh"按钮重新加载资源列表

## 当前状态

- ✅ 资源浏览窗口
- ✅ 搜索功能
- ✅ 标签筛选
- ✅ 资源信息显示
- ⏳ 资源导入功能（Stage 2.2）
- ⏳ 路由表维护（Stage 2.3）
- ⏳ 风格化资源上传（Stage 2.4）

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

- [ ] 实现资源导入功能（Stage 2.2）
- [ ] 实现路由表维护（Stage 2.3）
- [ ] 实现风格化资源上传（Stage 2.4）
- [ ] 添加资源预览功能
- [ ] 添加批量导入功能
- [ ] 添加项目管理界面

