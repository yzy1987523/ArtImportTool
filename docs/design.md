# 艺术资源管理系统设计文档

## 1. 系统架构设计

### 1.1 系统架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                      WPF Asset Manager                       │
│                    (独立桌面应用程序)                          │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │ Asset       │  │ Project      │  │ Style        │          │
│  │ Browser     │  │ Manager      │  │ Upload       │          │
│  │ Window      │  │ Window       │  │ Window       │          │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘          │
│         │                 │                 │                   │
│         └─────────────────┴─────────────────┘                   │
│                           │                                     │
│                           ▼                                     │
│              ┌──────────────────────┐                          │
│              │  WPF Application    │                          │
│              │      Layer          │                          │
│              └──────────┬───────────┘                          │
└───────────────────────────┼───────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Application Layer                           │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │   Asset      │  │   Tag        │  │   Project    │          │
│  │   Service    │  │   Service    │  │   Service    │          │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘          │
│         │                 │                 │                   │
│  ┌──────┴───────┐  ┌──────┴───────┐  ┌──────┴───────┐          │
│  │   Style      │  │   Route      │  │   Upload     │          │
│  │   Service    │  │   Service    │  │   Service    │          │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘          │
│         │                 │                 │                   │
│         └─────────────────┴─────────────────┘                   │
│                           │                                     │
│                           ▼                                     │
│              ┌──────────────────────┐                          │
│              │  Business Logic     │                          │
│              └──────────┬───────────┘                          │
└───────────────────────────┼───────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Data Access Layer                          │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │   Asset      │  │   Tag        │  │   Project    │          │
│  │   Repository │  │   Repository │  │   Repository │          │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘          │
│         │                 │                 │                   │
│  ┌──────┴───────┐  ┌──────┴───────┐  ┌──────┴───────┐          │
│  │   Style      │  │   Route      │  │   History    │          │
│  │   Repository │  │   Repository │  │   Repository │          │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘          │
│         │                 │                 │                   │
│         └─────────────────┴─────────────────┘                   │
│                           │                                     │
│                           ▼                                     │
│              ┌──────────────────────┐                          │
│              │  Data Access Object │                          │
│              └──────────┬───────────┘                          │
└───────────────────────────┼───────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Data Storage Layer                         │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                    SQLite Database                        │  │
│  │  (ArtAssets, Tags, AssetTags, Projects,                   │  │
│  │   StyleMigrations, UnityRoutes, RouteHistory)             │  │
│  └──────────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                  File Storage                            │  │
│  │  (Original Assets, Styled Assets, Metadata)              │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Unity Editor Layer                         │
│                    (Unity编辑器扩展)                            │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │ Asset       │  │ Asset        │  │ Route        │          │
│  │ Import      │  │ Replace      │  │ Sync         │          │
│  │ Window      │  │ Window       │  │ Service      │          │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘          │
│         │                 │                 │                   │
│         └─────────────────┴─────────────────┘                   │
│                           │                                     │
│                           ▼                                     │
│              ┌──────────────────────┐                          │
│              │  Unity Integration   │                          │
│              │      Module          │                          │
│              └──────────┬───────────┘                          │
└───────────────────────────┼───────────────────────────────────┘
                            │
                            ▼
                    ┌───────────────┐
                    │  Unity       │
                    │  Project     │
                    └─────────────┘
```

### 1.2 模块划分

#### 1.2.1 WPF资源管理器（独立桌面应用）
- **Asset Browser Window**: 资源浏览窗口
- **Project Manager Window**: Project管理窗口
- **Style Upload Window**: 风格化资源上传窗口
- **Asset Management Service**: 资源管理服务
- **Project Management Service**: Project管理服务
- **Style Upload Service**: 风格化上传服务

#### 1.2.2 Unity编辑器扩展
- **Asset Import Window**: 资源导入窗口
- **Asset Replace Window**: 资源替换窗口
- **Route Sync Service**: 路由同步服务
- **Unity Integration Service**: Unity集成服务

#### 1.2.3 业务逻辑模块（共享）
- **Asset Service**: 资源管理服务
- **Tag Service**: Tag管理服务
- **Project Service**: Project管理服务
- **Style Service**: 风格迁移服务
- **Route Service**: 路由表服务
- **Upload Service**: 上传服务

#### 1.2.3 数据访问模块
- **Asset Repository**: 资源数据访问
- **Tag Repository**: Tag数据访问
- **Project Repository**: Project数据访问
- **Style Repository**: 风格迁移数据访问
- **Route Repository**: 路由表数据访问
- **History Repository**: 历史记录数据访问

### 1.3 技术栈

#### 1.3.1 WPF资源管理器
- **.NET**: .NET 6.0 或更高版本
- **WPF**: Windows Presentation Foundation
- **C#**: C# 10.0
- **IDE**: Visual Studio 2022 / JetBrains Rider

#### 1.3.2 Unity编辑器扩展
- **Unity**: 2021.3 LTS 或更高版本
- **C#**: .NET Standard 2.1
- **IDE**: Visual Studio 2022 / JetBrains Rider

#### 1.3.3 共享业务逻辑层
- **.NET**: .NET Standard 2.1
- **C#**: C# 10.0

#### 1.3.4 数据库
- **开发阶段**: SQLite 3.x
- **生产环境**: PostgreSQL 14.x

#### 1.3.5 数据库ORM
- **Dapper**: 轻量级ORM，用于数据访问
- **Dapper.Contrib**: 扩展功能，简化CRUD操作

#### 1.3.6 UI框架
- **WPF**: Windows Presentation Foundation（资源管理器）
- **Unity Editor GUI**: Unity原生编辑器UI（Unity扩展）
- **IMGUI**: 即时模式GUI框架
- **UI Toolkit**: 可选，用于更复杂的UI需求

#### 1.3.7 工具库
- **Newtonsoft.Json**: JSON序列化
- **System.IO.Compression**: 文件压缩
- **System.Security.Cryptography**: 哈希计算

### 1.4 数据流

#### 1.4.1 资源入库流程
```
用户选择文件
    ↓
计算文件哈希
    ↓
检查是否已存在（去重）
    ↓
生成UUID
    ↓
保存文件到存储
    ↓
插入数据库记录
    ↓
添加默认Tag
    ↓
返回资源ID
```

#### 1.4.2 WPF资源管理器中创建Project流程
```
用户在WPF资源管理器中创建Project
    ↓
设置Project名称和Unity路径
    ↓
保存Project到数据库
    ↓
返回Project ID
```

#### 1.4.3 资源导入Unity流程
```
用户在Unity中选择资源
    ↓
从WPF资源管理器获取Project列表
    ↓
用户选择目标Project
    ↓
检查资源是否已存在（通过文件哈希）
    ↓
如果已存在 → 直接使用已有资源ID
如果不存在 → 创建新资源记录
    ↓
导入资源到Unity目录
    ↓
获取Unity GUID
    ↓
创建路由表记录
    ↓
关联到Project
    ↓
返回导入结果
```

#### 1.4.4 风格化资源上传流程
```
用户在Project中选择上传
    ↓
选择风格化文件
    ↓
计算文件哈希
    ↓
检查是否已存在（去重）
    ↓
生成UUID
    ↓
保存文件到存储
    ↓
插入数据库记录
    ↓
添加风格Tag
    ↓
通过名称匹配查找org资源
    ↓
创建风格迁移关联记录
    ↓
关联到Project
    ↓
返回上传结果
```

#### 1.4.5 资源替换流程
```
用户选择要替换的资源
    ↓
查询关联的风格化资源
    ↓
用户选择目标风格资源
    ↓
更新路由表指向新资源
    ↓
Unity中替换资源引用
    ↓
记录替换历史
    ↓
返回替换结果
```

## 2. 数据库Schema设计

### 2.1 ArtAssets表（资源表）

```sql
CREATE TABLE ArtAssets (
    id TEXT PRIMARY KEY,                    -- UUID
    name TEXT NOT NULL,                     -- 资源名称
    file_path TEXT NOT NULL,                -- 文件存储路径
    file_type TEXT NOT NULL,                -- 文件类型（png, jpg, mp3, wav等）
    file_size INTEGER NOT NULL,             -- 文件大小（字节）
    file_hash TEXT NOT NULL UNIQUE,         -- 文件哈希（SHA256，用于去重）
    width INTEGER,                          -- 图片宽度（仅图片）
    height INTEGER,                         -- 图片高度（仅图片）
    duration INTEGER,                        -- 音频时长（仅音频，毫秒）
    metadata TEXT,                          -- 元数据（JSON格式）
    created_at INTEGER NOT NULL,            -- 创建时间（Unix时间戳）
    updated_at INTEGER NOT NULL,            -- 更新时间（Unix时间戳）
    is_deleted INTEGER DEFAULT 0            -- 是否删除（0=否，1=是）
);

CREATE INDEX idx_artassets_file_hash ON ArtAssets(file_hash);
CREATE INDEX idx_artassets_file_type ON ArtAssets(file_type);
CREATE INDEX idx_artassets_created_at ON ArtAssets(created_at);
CREATE INDEX idx_artassets_is_deleted ON ArtAssets(is_deleted);
```

### 2.2 Tags表（标签表）

```sql
CREATE TABLE Tags (
    id INTEGER PRIMARY KEY AUTOINCREMENT,   -- 自增ID
    name TEXT NOT NULL UNIQUE,              -- 标签名称
    category TEXT NOT NULL,                  -- 标签分类（org, style, type, status）
    description TEXT,                        -- 标签描述
    color TEXT,                             -- 标签颜色（十六进制）
    sort_order INTEGER DEFAULT 0,           -- 排序顺序
    created_at INTEGER NOT NULL,            -- 创建时间
    updated_at INTEGER NOT NULL             -- 更新时间
);

CREATE INDEX idx_tags_category ON Tags(category);
CREATE INDEX idx_tags_sort_order ON Tags(sort_order);
```

### 2.3 AssetTags表（资源标签关联表）

```sql
CREATE TABLE AssetTags (
    id INTEGER PRIMARY KEY AUTOINCREMENT,   -- 自增ID
    asset_id TEXT NOT NULL,                -- 资源ID（外键 → ArtAssets.id）
    tag_id INTEGER NOT NULL,               -- 标签ID（外键 → Tags.id）
    created_at INTEGER NOT NULL,           -- 创建时间
    created_by TEXT,                       -- 创建人
    UNIQUE(asset_id, tag_id)
);

CREATE INDEX idx_assettags_asset_id ON AssetTags(asset_id);
CREATE INDEX idx_assettags_tag_id ON AssetTags(tag_id);
```

### 2.4 Projects表（项目表）

```sql
CREATE TABLE Projects (
    id TEXT PRIMARY KEY,                    -- UUID
    name TEXT NOT NULL,                     -- Project名称
    description TEXT,                       -- Project描述
    unity_path TEXT NOT NULL,               -- Unity中的路径
    created_at INTEGER NOT NULL,            -- 创建时间
    updated_at INTEGER NOT NULL,            -- 更新时间
    is_deleted INTEGER DEFAULT 0            -- 是否删除
);

CREATE INDEX idx_projects_unity_path ON Projects(unity_path);
CREATE INDEX idx_projects_is_deleted ON Projects(is_deleted);
```

### 2.5 AssetGroups表（资源组表）

```sql
CREATE TABLE AssetGroups (
    id TEXT PRIMARY KEY,                    -- UUID
    name TEXT NOT NULL,                     -- 组名称（如walk, run, idle）
    group_type TEXT NOT NULL,               -- 组类型（sequence=序列帧, single=单资源, collection=集合）
    base_name TEXT NOT NULL,                -- 基础名称（用于序列帧匹配）
    project_id TEXT,                       -- 所属Project ID（外键 → Projects.id）
    frame_count INTEGER DEFAULT 1,          -- 帧数（仅sequence类型）
    start_frame INTEGER DEFAULT 1,          -- 起始帧号（仅sequence类型）
    end_frame INTEGER DEFAULT 1,            -- 结束帧号（仅sequence类型）
    frame_pattern TEXT,                     -- 帧命名模式（如{base_name}_{frame:03d}.png）
    metadata TEXT,                         -- 元数据（JSON格式）
    created_at INTEGER NOT NULL,            -- 创建时间
    updated_at INTEGER NOT NULL,            -- 更新时间
    is_deleted INTEGER DEFAULT 0            -- 是否删除
);

CREATE INDEX idx_assetgroups_project_id ON AssetGroups(project_id);
CREATE INDEX idx_assetgroups_group_type ON AssetGroups(group_type);
CREATE INDEX idx_assetgroups_is_deleted ON AssetGroups(is_deleted);
```

### 2.6 GroupAssets表（组资源关联表）

```sql
CREATE TABLE GroupAssets (
    id INTEGER PRIMARY KEY AUTOINCREMENT,   -- 自增ID
    group_id TEXT NOT NULL,               -- 组ID（外键 → AssetGroups.id）
    asset_id TEXT NOT NULL,                -- 资源ID（外键 → ArtAssets.id）
    frame_number INTEGER,                  -- 帧号（仅sequence类型）
    frame_name TEXT,                      -- 帧名称（如walk_001.png）
    sort_order INTEGER DEFAULT 0,          -- 排序顺序
    is_keyframe INTEGER DEFAULT 0,         -- 是否为关键帧（1=是，0=否）
    created_at INTEGER NOT NULL,           -- 创建时间
    UNIQUE(group_id, asset_id)
);

CREATE INDEX idx_groupassets_group_id ON GroupAssets(group_id);
CREATE INDEX idx_groupassets_asset_id ON GroupAssets(asset_id);
CREATE INDEX idx_groupassets_frame_number ON GroupAssets(frame_number);
CREATE INDEX idx_groupassets_sort_order ON GroupAssets(sort_order);
```

### 2.7 ProjectAssets表（项目资源关联表）

```sql
CREATE TABLE ProjectAssets (
    id INTEGER PRIMARY KEY AUTOINCREMENT,   -- 自增ID
    project_id TEXT NOT NULL,              -- Project ID（外键 → Projects.id）
    asset_id TEXT NOT NULL,                -- 资源ID（外键 → ArtAssets.id）
    import_name TEXT NOT NULL,             -- 导入时的名称
    import_path TEXT NOT NULL,             -- 导入时的路径
    is_original INTEGER DEFAULT 1,         -- 是否为原始资源（1=是，0=否）
    created_at INTEGER NOT NULL,           -- 创建时间
    UNIQUE(project_id, asset_id)
);

CREATE INDEX idx_projectassets_project_id ON ProjectAssets(project_id);
CREATE INDEX idx_projectassets_asset_id ON ProjectAssets(asset_id);
CREATE INDEX idx_projectassets_is_original ON ProjectAssets(is_original);
```

### 2.8 StyleMigrations表（风格迁移表）

```sql
CREATE TABLE StyleMigrations (
    id TEXT PRIMARY KEY,                    -- UUID
    original_asset_id TEXT NOT NULL,       -- 原始资源ID（外键 → ArtAssets.id）
    styled_asset_id TEXT NOT NULL,         -- 风格化资源ID（外键 → ArtAssets.id）
    style_tag TEXT NOT NULL,               -- 风格标签（如style_cartoon）
    project_id TEXT,                       -- 所属Project ID（外键 → Projects.id）
    metadata TEXT,                         -- 元数据（JSON格式）
    created_at INTEGER NOT NULL,           -- 创建时间
    created_by TEXT                        -- 创建人
);

CREATE INDEX idx_stylemigrations_original_asset_id ON StyleMigrations(original_asset_id);
CREATE INDEX idx_stylemigrations_styled_asset_id ON StyleMigrations(styled_asset_id);
CREATE INDEX idx_stylemigrations_style_tag ON StyleMigrations(style_tag);
CREATE INDEX idx_stylemigrations_project_id ON StyleMigrations(project_id);
```

### 2.9 UnityRoutes表（Unity路由表）

```sql
CREATE TABLE UnityRoutes (
    id TEXT PRIMARY KEY,                    -- UUID
    asset_id TEXT NOT NULL,                -- 资源ID（外键 → ArtAssets.id）
    group_id TEXT,                         -- 资源组ID（外键 → AssetGroups.id，可为空）
    project_id TEXT NOT NULL,              -- Project ID（外键 → Projects.id）
    unity_guid TEXT NOT NULL,              -- Unity资源的GUID
    unity_path TEXT NOT NULL,              -- Unity中的路径
    unity_name TEXT NOT NULL,             -- Unity中的名称
    original_import_path TEXT,            -- 首次导入路径
    is_active INTEGER DEFAULT 1,          -- 是否为当前激活的资源
    created_at INTEGER NOT NULL,           -- 创建时间
    updated_at INTEGER NOT NULL,           -- 更新时间
    UNIQUE(unity_guid)
);

CREATE INDEX idx_unityroutes_asset_id ON UnityRoutes(asset_id);
CREATE INDEX idx_unityroutes_group_id ON UnityRoutes(group_id);
CREATE INDEX idx_unityroutes_project_id ON UnityRoutes(project_id);
CREATE INDEX idx_unityroutes_unity_guid ON UnityRoutes(unity_guid);
CREATE INDEX idx_unityroutes_is_active ON UnityRoutes(is_active);
```

### 2.10 RouteHistory表（路由历史表）

```sql
CREATE TABLE RouteHistory (
    id TEXT PRIMARY KEY,                    -- UUID
    route_id TEXT NOT NULL,                -- 路由ID（外键 → UnityRoutes.id）
    old_asset_id TEXT,                     -- 旧资源ID
    new_asset_id TEXT,                     -- 新资源ID
    old_unity_path TEXT,                   -- 旧路径
    new_unity_path TEXT,                   -- 新路径
    action TEXT NOT NULL,                  -- 操作类型（create, update, replace, delete）
    metadata TEXT,                         -- 元数据（JSON格式）
    created_at INTEGER NOT NULL,           -- 创建时间
    created_by TEXT                        -- 操作人
);

CREATE INDEX idx_routehistory_route_id ON RouteHistory(route_id);
CREATE INDEX idx_routehistory_created_at ON RouteHistory(created_at);
CREATE INDEX idx_routehistory_action ON RouteHistory(action);
```

### 2.9 数据库关系图

```
┌──────────────┐
│   Tags       │
│              │
│  id (PK)     │
│  name        │
│  category    │
└──────┬───────┘
       │
       │ 1
       │
       │ N
       │
┌──────▼────────┐         ┌──────────────┐         ┌──────────────┐
│  AssetTags    │         │  ArtAssets   │         │ StyleMigrations│
│              │         │              │         │              │
│  id (PK)     │         │  id (PK)     │◄────────│  id (PK)     │
│  asset_id    │────────►│  name        │         │  original    │
│  tag_id      │         │  file_path   │         │  _asset_id   │
└──────────────┘         │  file_hash   │         │  styled      │
                        └──────┬───────┘         │  _asset_id   │
                               │                 └──────┬───────┘
                               │ 1                      │
                               │                        │ 1
                               │ N                      │ N
                               │                        │
                    ┌──────────▼──────────┐    ┌────────▼────────┐
                    │  UnityRoutes       │    │  ProjectAssets  │
                    │                    │    │                 │
                    │  id (PK)           │    │  id (PK)        │
                    │  asset_id          │    │  project_id     │
                    │  project_id        │    │  asset_id       │
                    │  unity_guid        │    └────────┬────────┘
                    └────────┬───────────┘             │
                             │                         │
                             │ 1                       │ N
                             │                         │
                             │ N                       │
                             │                         │
                    ┌────────▼──────────┐    ┌────────▼────────┐
                    │  RouteHistory     │    │   Projects      │
                    │                   │    │                 │
                    │  id (PK)          │    │  id (PK)        │
                    │  route_id         │    │  name           │
                    └───────────────────┘    │  unity_path     │
                                             └─────────────────┘
```

## 3. API接口设计

### 3.1 资源管理API

#### 3.1.1 创建资源
```csharp
/// <summary>
/// 创建新资源
/// </summary>
/// <param name="request">创建资源请求</param>
/// <returns>创建的资源ID</returns>
public class CreateAssetRequest
{
    public string Name { get; set; }
    public string FilePath { get; set; }
    public string FileType { get; set; }
    public int FileSize { get; set; }
    public string FileHash { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Duration { get; set; }
    public string Metadata { get; set; }
    public List<string> Tags { get; set; }
}

public interface IAssetService
{
    Task<string> CreateAssetAsync(CreateAssetRequest request);
}
```

#### 3.1.2 查询资源
```csharp
/// <summary>
/// 查询资源
/// </summary>
/// <param name="request">查询请求</param>
/// <returns>资源列表</returns>
public class QueryAssetsRequest
{
    public string Keyword { get; set; }
    public List<string> Tags { get; set; }
    public string FileType { get; set; }
    public long? StartTime { get; set; }
    public long? EndTime { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AssetDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string FilePath { get; set; }
    public string FileType { get; set; }
    public int FileSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Duration { get; set; }
    public List<string> Tags { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
}

public interface IAssetService
{
    Task<PagedResult<AssetDto>> QueryAssetsAsync(QueryAssetsRequest request);
}
```

#### 3.1.3 批量导入资源
```csharp
/// <summary>
/// 批量导入资源
/// </summary>
/// <param name="request">批量导入请求</param>
/// <returns>导入结果</returns>
public class BatchImportRequest
{
    public List<ImportFileRequest> Files { get; set; }
    public List<string> DefaultTags { get; set; }
}

public class ImportFileRequest
{
    public string FilePath { get; set; }
    public string Name { get; set; }
}

public class BatchImportResult
{
    public List<string> SuccessIds { get; set; }
    public List<ImportError> Errors { get; set; }
}

public class ImportError
{
    public string FilePath { get; set; }
    public string Message { get; set; }
}

public interface IAssetService
{
    Task<BatchImportResult> BatchImportAsync(BatchImportRequest request);
}
```

### 3.2 Tag管理API

#### 3.2.1 创建Tag
```csharp
/// <summary>
/// 创建Tag
/// </summary>
/// <param name="request">创建Tag请求</param>
/// <returns>Tag ID</returns>
public class CreateTagRequest
{
    public string Name { get; set; }
    public string Category { get; set; }
    public string Description { get; set; }
    public string Color { get; set; }
    public int SortOrder { get; set; }
}

public interface ITagService
{
    Task<int> CreateTagAsync(CreateTagRequest request);
}
```

#### 3.2.2 添加资源Tag
```csharp
/// <summary>
/// 为资源添加Tag
/// </summary>
/// <param name="assetId">资源ID</param>
/// <param name="tagIds">Tag ID列表</param>
public interface ITagService
{
    Task AddAssetTagsAsync(string assetId, List<int> tagIds);
}
```

#### 3.2.3 查询Tags
```csharp
/// <summary>
/// 查询Tags
/// </summary>
/// <param name="category">Tag分类</param>
/// <returns>Tag列表</returns>
public class TagDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public string Description { get; set; }
    public string Color { get; set; }
    public int SortOrder { get; set; }
}

public interface ITagService
{
    Task<List<TagDto>> QueryTagsAsync(string category = null);
}
```

### 3.3 Project管理API

#### 3.3.1 创建Project
```csharp
/// <summary>
/// 创建Project
/// </summary>
/// <param name="request">创建Project请求</param>
/// <returns>Project ID</returns>
public class CreateProjectRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string UnityPath { get; set; }
}

public interface IProjectService
{
    Task<string> CreateProjectAsync(CreateProjectRequest request);
}
```

#### 3.3.2 查询Project
```csharp
/// <summary>
/// 查询Project
/// </summary>
/// <param name="projectId">Project ID</param>
/// <returns>Project详情</returns>
public class ProjectDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string UnityPath { get; set; }
    public List<ProjectAssetDto> Assets { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
}

public class ProjectAssetDto
{
    public string AssetId { get; set; }
    public string AssetName { get; set; }
    public string ImportName { get; set; }
    public string ImportPath { get; set; }
    public bool IsOriginal { get; set; }
    public List<string> Tags { get; set; }
    public List<StyledAssetDto> StyledAssets { get; set; }
}

public class StyledAssetDto
{
    public string AssetId { get; set; }
    public string AssetName { get; set; }
    public string StyleTag { get; set; }
}

public interface IProjectService
{
    Task<ProjectDto> GetProjectAsync(string projectId);
}
```

#### 3.3.3 上传风格化资源
```csharp
/// <summary>
/// 上传风格化资源到Project
/// </summary>
/// <param name="request">上传请求</param>
/// <returns>上传结果</returns>
public class UploadStyledAssetRequest
{
    public string ProjectId { get; set; }
    public string OriginalAssetName { get; set; }  // 通过名称匹配org资源
    public string FilePath { get; set; }
    public string StyleTag { get; set; }
    public string CreatedBy { get; set; }
}

public class UploadStyledAssetResult
{
    public string AssetId { get; set; }
    public string MigrationId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
}

public interface IProjectService
{
    Task<UploadStyledAssetResult> UploadStyledAssetAsync(UploadStyledAssetRequest request);
}
```

### 3.4 资源组管理API

#### 3.4.1 创建资源组
```csharp
/// <summary>
/// 创建资源组（用于序列帧动画等）
/// </summary>
/// <param name="request">创建资源组请求</param>
/// <returns>资源组ID</returns>
public class CreateAssetGroupRequest
{
    public string Name { get; set; }
    public string GroupType { get; set; }  // sequence, single, collection
    public string BaseName { get; set; }
    public string ProjectId { get; set; }
    public int FrameCount { get; set; }
    public int StartFrame { get; set; }
    public int EndFrame { get; set; }
    public string FramePattern { get; set; }
}

public interface IAssetGroupService
{
    Task<string> CreateAssetGroupAsync(CreateAssetGroupRequest request);
}
```

#### 3.4.2 添加资源到组
```csharp
/// <summary>
/// 添加资源到组
/// </summary>
/// <param name="request">添加资源到组请求</param>
public class AddAssetToGroupRequest
{
    public string GroupId { get; set; }
    public string AssetId { get; set; }
    public int? FrameNumber { get; set; }
    public string FrameName { get; set; }
    public int SortOrder { get; set; }
    public bool IsKeyframe { get; set; }
}

public interface IAssetGroupService
{
    Task AddAssetToGroupAsync(AddAssetToGroupRequest request);
}
```

#### 3.4.3 查询资源组
```csharp
/// <summary>
/// 查询资源组
/// </summary>
/// <param name="groupId">资源组ID</param>
/// <returns>资源组详情</returns>
public class AssetGroupDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string GroupType { get; set; }
    public string BaseName { get; set; }
    public string ProjectId { get; set; }
    public int FrameCount { get; set; }
    public int StartFrame { get; set; }
    public int EndFrame { get; set; }
    public string FramePattern { get; set; }
    public List<GroupAssetDto> Assets { get; set; }
    public long CreatedAt { get; set; }
}

public class GroupAssetDto
{
    public string AssetId { get; set; }
    public string AssetName { get; set; }
    public int? FrameNumber { get; set; }
    public string FrameName { get; set; }
    public int SortOrder { get; set; }
    public bool IsKeyframe { get; set; }
    public List<string> Tags { get; set; }
}

public interface IAssetGroupService
{
    Task<AssetGroupDto> GetAssetGroupAsync(string groupId);
}
```

#### 3.4.4 批量导入序列帧
```csharp
/// <summary>
/// 批量导入序列帧并创建资源组
/// </summary>
/// <param name="request">批量导入序列帧请求</param>
/// <returns>导入结果</returns>
public class ImportSequenceFramesRequest
{
    public string ProjectId { get; set; }
    public string GroupName { get; set; }
    public string BaseName { get; set; }
    public List<string> FilePaths { get; set; }
    public string FramePattern { get; set; }
    public List<string> DefaultTags { get; set; }
}

public class ImportSequenceFramesResult
{
    public string GroupId { get; set; }
    public List<string> AssetIds { get; set; }
    public List<ImportError> Errors { get; set; }
}

public interface IAssetGroupService
{
    Task<ImportSequenceFramesResult> ImportSequenceFramesAsync(ImportSequenceFramesRequest request);
}
```

### 3.5 风格迁移API

#### 3.5.1 创建风格迁移关联
```csharp
/// <summary>
/// 创建风格迁移关联
/// </summary>
/// <param name="request">创建关联请求</param>
/// <returns>迁移ID</returns>
public class CreateStyleMigrationRequest
{
    public string OriginalAssetId { get; set; }
    public string StyledAssetId { get; set; }
    public string StyleTag { get; set; }
    public string ProjectId { get; set; }
    public string CreatedBy { get; set; }
}

public interface IStyleService
{
    Task<string> CreateStyleMigrationAsync(CreateStyleMigrationRequest request);
}
```

#### 3.5.2 查询风格迁移
```csharp
/// <summary>
/// 查询资源的风格迁移
/// </summary>
/// <param name="originalAssetId">原始资源ID</param>
/// <returns>风格迁移列表</returns>
public class StyleMigrationDto
{
    public string Id { get; set; }
    public string OriginalAssetId { get; set; }
    public string StyledAssetId { get; set; }
    public string StyledAssetName { get; set; }
    public string StyleTag { get; set; }
    public string ProjectId { get; set; }
    public long CreatedAt { get; set; }
}

public interface IStyleService
{
    Task<List<StyleMigrationDto>> QueryStyleMigrationsAsync(string originalAssetId);
}
```

### 3.5 Unity导入API

#### 3.5.1 获取Project列表
```csharp
/// <summary>
/// 获取所有Project列表
/// </summary>
/// <returns>Project列表</returns>
public class ProjectListDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string UnityPath { get; set; }
    public int AssetCount { get; set; }
    public long CreatedAt { get; set; }
}

public interface IUnityImportService
{
    Task<List<ProjectListDto>> GetProjectsAsync();
}
```

#### 3.5.2 导入资源到Unity
```csharp
/// <summary>
/// 导入资源到Unity
/// </summary>
/// <param name="request">导入请求</param>
/// <returns>导入结果</returns>
public class ImportToUnityRequest
{
    public string ProjectId { get; set; }
    public string FilePath { get; set; }
    public string ImportName { get; set; }
    public string UnityPath { get; set; }
}

public class ImportToUnityResult
{
    public string AssetId { get; set; }
    public string RouteId { get; set; }
    public bool IsExisting { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
}

public interface IUnityImportService
{
    Task<ImportToUnityResult> ImportToUnityAsync(ImportToUnityRequest request);
}
```

### 3.6 路由表API

#### 3.6.1 创建路由
```csharp
/// <summary>
/// 创建路由
/// </summary>
/// <param name="request">创建路由请求</param>
/// <returns>路由ID</returns>
public class CreateRouteRequest
{
    public string AssetId { get; set; }
    public string GroupId { get; set; }  // 资源组ID（可为空）
    public string ProjectId { get; set; }
    public string UnityGuid { get; set; }
    public string UnityPath { get; set; }
    public string UnityName { get; set; }
}

public interface IRouteService
{
    Task<string> CreateRouteAsync(CreateRouteRequest request);
}
```

#### 3.6.2 更新路由
```csharp
/// <summary>
/// 更新路由（替换资源）
/// </summary>
/// <param name="request">更新路由请求</param>
public class UpdateRouteRequest
{
    public string RouteId { get; set; }
    public string NewAssetId { get; set; }
    public string NewUnityPath { get; set; }
    public string NewUnityName { get; set; }
    public string UpdatedBy { get; set; }
}

public interface IRouteService
{
    Task UpdateRouteAsync(UpdateRouteRequest request);
}
```

#### 3.6.3 查询路由
```csharp
/// <summary>
/// 查询路由
/// </summary>
/// <param name="projectId">Project ID</param>
/// <returns>路由列表</returns>
public class RouteDto
{
    public string Id { get; set; }
    public string AssetId { get; set; }
    public string AssetName { get; set; }
    public string ProjectId { get; set; }
    public string UnityGuid { get; set; }
    public string UnityPath { get; set; }
    public string UnityName { get; set; }
    public bool IsActive { get; set; }
    public List<string> Tags { get; set; }
}

public interface IRouteService
{
    Task<List<RouteDto>> QueryRoutesAsync(string projectId);
}
```

#### 3.6.4 同步路由路径
```csharp
/// <summary>
/// 同步Unity资源路径变更到路由表
/// </summary>
/// <param name="unityGuid">Unity GUID</param>
/// <param name="newPath">新路径</param>
/// <param name="newName">新名称</param>
public interface IRouteService
{
    Task SyncRoutePathAsync(string unityGuid, string newPath, string newName);
}
```

## 4. 数据模型

### 4.1 核心实体类

```csharp
/// <summary>
/// 资源实体
/// </summary>
public class Asset
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string FilePath { get; set; }
    public string FileType { get; set; }
    public int FileSize { get; set; }
    public string FileHash { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Duration { get; set; }
    public string Metadata { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

/// <summary>
/// Tag实体
/// </summary>
public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public string Description { get; set; }
    public string Color { get; set; }
    public int SortOrder { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
}

/// <summary>
/// 资源Tag关联实体
/// </summary>
public class AssetTag
{
    public int Id { get; set; }
    public string AssetId { get; set; }
    public int TagId { get; set; }
    public long CreatedAt { get; set; }
    public string CreatedBy { get; set; }
}

/// <summary>
/// Project实体
/// </summary>
public class Project
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string UnityPath { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

/// <summary>
/// Project资源关联实体
/// </summary>
public class ProjectAsset
{
    public int Id { get; set; }
    public string ProjectId { get; set; }
    public string AssetId { get; set; }
    public string ImportName { get; set; }
    public string ImportPath { get; set; }
    public bool IsOriginal { get; set; }
    public long CreatedAt { get; set; }
}

/// <summary>
/// 资源组实体（用于序列帧动画等）
/// </summary>
public class AssetGroup
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string GroupType { get; set; }
    public string BaseName { get; set; }
    public string ProjectId { get; set; }
    public int FrameCount { get; set; }
    public int StartFrame { get; set; }
    public int EndFrame { get; set; }
    public string FramePattern { get; set; }
    public string Metadata { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

/// <summary>
/// 组资源关联实体
/// </summary>
public class GroupAsset
{
    public int Id { get; set; }
    public string GroupId { get; set; }
    public string AssetId { get; set; }
    public int? FrameNumber { get; set; }
    public string FrameName { get; set; }
    public int SortOrder { get; set; }
    public bool IsKeyframe { get; set; }
    public long CreatedAt { get; set; }
}

/// <summary>
/// 风格迁移实体
/// </summary>
public class StyleMigration
{
    public string Id { get; set; }
    public string OriginalAssetId { get; set; }
    public string StyledAssetId { get; set; }
    public string StyleTag { get; set; }
    public string ProjectId { get; set; }
    public string Metadata { get; set; }
    public long CreatedAt { get; set; }
    public string CreatedBy { get; set; }
}

/// <summary>
/// Unity路由实体
/// </summary>
public class UnityRoute
{
    public string Id { get; set; }
    public string AssetId { get; set; }
    public string ProjectId { get; set; }
    public string UnityGuid { get; set; }
    public string UnityPath { get; set; }
    public string UnityName { get; set; }
    public string OriginalImportPath { get; set; }
    public bool IsActive { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
}

/// <summary>
/// 路由历史实体
/// </summary>
public class RouteHistory
{
    public string Id { get; set; }
    public string RouteId { get; set; }
    public string OldAssetId { get; set; }
    public string NewAssetId { get; set; }
    public string OldUnityPath { get; set; }
    public string NewUnityPath { get; set; }
    public string Action { get; set; }
    public string Metadata { get; set; }
    public long CreatedAt { get; set; }
    public string CreatedBy { get; set; }
}
```

### 4.2 分页结果

```csharp
/// <summary>
/// 分页结果
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
```

## 5. 核心算法

### 5.1 资源名称匹配算法

```csharp
/// <summary>
/// 资源名称匹配算法
/// </summary>
public class AssetNameMatcher
{
    /// <summary>
    /// 通过名称匹配查找org资源
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="styledAssetName">风格化资源名称</param>
    /// <returns>匹配的org资源ID</returns>
    public async Task<string> MatchOriginalAssetAsync(string projectId, string styledAssetName)
    {
        // 1. 规范化名称（去除扩展名、空格、特殊字符）
        var normalizedName = NormalizeAssetName(styledAssetName);
        
        // 2. 查询Project中的所有org资源
        var projectAssets = await _projectRepository.GetProjectAssetsAsync(projectId);
        var originalAssets = projectAssets.Where(a => a.IsOriginal).ToList();
        
        // 3. 精确匹配
        var exactMatch = originalAssets.FirstOrDefault(a => 
            NormalizeAssetName(a.AssetName) == normalizedName);
        if (exactMatch != null)
        {
            return exactMatch.AssetId;
        }
        
        // 4. 模糊匹配（Levenshtein距离）
        var bestMatch = FindBestFuzzyMatch(originalAssets, normalizedName);
        if (bestMatch != null && bestMatch.Similarity > 0.8)
        {
            return bestMatch.AssetId;
        }
        
        // 5. 返回null，表示未找到匹配
        return null;
    }
    
    /// <summary>
    /// 规范化资源名称
    /// </summary>
    private string NormalizeAssetName(string name)
    {
        // 去除扩展名
        name = Path.GetFileNameWithoutExtension(name);
        
        // 转换为小写
        name = name.ToLower();
        
        // 去除空格和特殊字符
        name = Regex.Replace(name, @"[\s\-_]+", "");
        
        return name;
    }
    
    /// <summary>
    /// 查找最佳模糊匹配
    /// </summary>
    private (string AssetId, double Similarity) FindBestFuzzyMatch(
        List<ProjectAsset> assets, string targetName)
    {
        var bestMatch = assets
            .Select(a => new
            {
                Asset = a,
                Similarity = CalculateSimilarity(NormalizeAssetName(a.AssetName), targetName)
            })
            .OrderByDescending(x => x.Similarity)
            .FirstOrDefault();
        
        if (bestMatch != null && bestMatch.Similarity > 0.8)
        {
            return (bestMatch.Asset.AssetId, bestMatch.Similarity);
        }
        
        return (null, 0);
    }
    
    /// <summary>
    /// 计算字符串相似度（基于Levenshtein距离）
    /// </summary>
    private double CalculateSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0;
        
        int distance = LevenshteinDistance(s1, s2);
        int maxLength = Math.Max(s1.Length, s2.Length);
        
        return 1.0 - (double)distance / maxLength;
    }
    
    /// <summary>
    /// 计算Levenshtein距离
    /// </summary>
    private int LevenshteinDistance(string s1, string s2)
    {
        int[,] d = new int[s1.Length + 1, s2.Length + 1];
        
        for (int i = 0; i <= s1.Length; i++)
            d[i, 0] = i;
        
        for (int j = 0; j <= s2.Length; j++)
            d[0, j] = j;
        
        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        
        return d[s1.Length, s2.Length];
    }
}
```
### 5.2 资源组匹配算法

```csharp
/// <summary>
/// 资源组匹配算法（用于序列帧动画）
/// </summary>
public class AssetGroupMatcher
{
    /// <summary>
    /// 通过资源组匹配查找org资源组
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="styledGroupName">风格化资源组名称</param>
    /// <returns>匹配的org资源组ID</returns>
    public async Task<string> MatchOriginalGroupAsync(string projectId, string styledGroupName)
    {
        // 1. 规范化组名称
        var normalizedGroupName = NormalizeGroupName(styledGroupName);
        
        // 2. 查询Project中的所有资源组
        var groups = await _assetGroupRepository.GetByProjectAsync(projectId);
        var originalGroups = groups.Where(g => g.GroupType == "sequence").ToList();
        
        // 3. 精确匹配
        var exactMatch = originalGroups.FirstOrDefault(g => 
            NormalizeGroupName(g.Name) == normalizedGroupName);
        if (exactMatch != null)
        {
            return exactMatch.Id;
        }
        
        // 4. 模糊匹配
        var bestMatch = FindBestFuzzyGroupMatch(originalGroups, normalizedGroupName);
        if (bestMatch != null && bestMatch.Similarity > 0.8)
        {
            return bestMatch.GroupId;
        }
        
        return null;
    }
    
    /// <summary>
    /// 规范化组名称
    /// </summary>
    private string NormalizeGroupName(string name)
    {
        // 去除空格和特殊字符
        name = Regex.Replace(name, @"[\s\-_]+", "");
        
        // 转换为小写
        return name.ToLower();
    }
    
    /// <summary>
    /// 查找最佳模糊匹配
    /// </summary>
    private (string GroupId, double Similarity) FindBestFuzzyGroupMatch(
        List<AssetGroup> groups, string targetName)
    {
        var bestMatch = groups
            .Select(g => new
            {
                Group = g,
                Similarity = CalculateSimilarity(NormalizeGroupName(g.Name), targetName)
            })
            .OrderByDescending(x => x.Similarity)
            .FirstOrDefault();
        
        if (bestMatch != null && bestMatch.Similarity > 0.8)
        {
            return (bestMatch.Group.Id, bestMatch.Similarity);
        }
        
        return (null, 0);
    }
    
    /// <summary>
    /// 计算字符串相似度
    /// </summary>
    private double CalculateSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0;
        
        int distance = LevenshteinDistance(s1, s2);
        int maxLength = Math.Max(s1.Length, s2.Length);
        
        return 1.0 - (double)distance / maxLength;
    }
    
    /// <summary>
    /// 计算Levenshtein距离
    /// </summary>
    private int LevenshteinDistance(string s1, string s2)
    {
        int[,] d = new int[s1.Length + 1, s2.Length + 1];
        
        for (int i = 0; i <= s1.Length; i++)
            d[i, 0] = i;
        
        for (int j = 0; j <= s2.Length; j++)
            d[0, j] = j;
        
        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        
        return d[s1.Length, s2.Length];
    }
}
```
### 5.3 资源类型识别算法

```csharp
/// <summary>
/// 资源类型识别结果
/// </summary>
public enum AssetType
{
    Single,      // 单张图片
    Sequence,    // 序列帧动画
    Collection    // 资源集合
}

/// <summary>
/// 资源类型识别结果
/// </summary>
public class AssetTypeRecognitionResult
{
    public AssetType Type { get; set; }
    public double Confidence { get; set; }
    public string Reason { get; set; }
    public List<string> FileNames { get; set; }
    public SequenceInfo SequenceInfo { get; set; }
}

/// <summary>
/// 序列帧信息
/// </summary>
public class SequenceInfo
{
    public string BaseName { get; set; }
    public int FrameCount { get; set; }
    public int StartFrame { get; set; }
    public int EndFrame { get; set; }
    public string FramePattern { get; set; }
    public bool IsContinuous { get; set; }
}

/// <summary>
/// 资源类型识别器
/// </summary>
public class AssetTypeRecognizer
{
    /// <summary>
    /// 识别资源类型
    /// </summary>
    /// <param name="filePaths">文件路径列表</param>
    /// <returns>识别结果</returns>
    public AssetTypeRecognitionResult RecognizeAssetType(List<string> filePaths)
    {
        if (filePaths == null || filePaths.Count == 0)
        {
            return new AssetTypeRecognitionResult
            {
                Type = AssetType.Single,
                Confidence = 1.0,
                Reason = "无文件",
                FileNames = new List<string>()
            };
        }
        
        // 1. 分析文件命名模式
        var namingAnalysis = AnalyzeNamingPattern(filePaths);
        
        // 2. 基于文件数量判断
        var countAnalysis = AnalyzeFileCount(filePaths.Count, namingAnalysis);
        
        // 3. 基于数字连续性判断
        var continuityAnalysis = AnalyzeNumberContinuity(filePaths, namingAnalysis);
        
        // 4. 综合判断
        return MakeDecision(filePaths, namingAnalysis, countAnalysis, continuityAnalysis);
    }
    
    /// <summary>
    /// 分析文件命名模式
    /// </summary>
    private NamingAnalysisResult AnalyzeNamingPattern(List<string> filePaths)
    {
        var fileNames = filePaths.Select(Path.GetFileNameWithoutExtension).ToList();
        
        // 提取所有数字
        var numbers = fileNames
            .SelectMany(f => Regex.Matches(f, @"\d+"))
            .Select(m => int.Parse(m.Value))
            .ToList();
        
        // 分析数字模式
        var hasNumbers = numbers.Count > 0;
        var uniqueNumbers = numbers.Distinct().Count();
        var minNumber = numbers.Min();
        var maxNumber = numbers.Max();
        
        // 分析前缀
        var prefixes = fileNames
            .Select(f => Regex.Replace(f, @"_\d+$", ""))
            .Distinct()
            .ToList();
        
        return new NamingAnalysisResult
        {
            HasNumbers = hasNumbers,
            UniqueNumberCount = uniqueNumbers,
            MinNumber = minNumber,
            MaxNumber = maxNumber,
            NumberRange = maxNumber - minNumber + 1,
            Prefixes = prefixes,
            PrefixCount = prefixes.Count,
            CommonPrefix = prefixes.Count == 1 ? prefixes.First() : null
        };
    }
    
    /// <summary>
    /// 基于文件数量判断
    /// </summary>
    private FileCountAnalysisResult AnalyzeFileCount(int fileCount, NamingAnalysisResult namingAnalysis)
    {
        if (fileCount == 1)
        {
            return new FileCountAnalysisResult
            {
                Type = AssetType.Single,
                Confidence = 0.9,
                Reason = "单文件"
            };
        }
        
        if (fileCount == 2)
        {
            return new FileCountAnalysisResult
            {
                Type = AssetType.Single,
                Confidence = 0.7,
                Reason = "两文件，可能是单资源"
            };
        }
        
        // 3个及以上才可能是序列帧
        return new FileCountAnalysisResult
        {
            Type = AssetType.Sequence,
            Confidence = 0.3,
            Reason = "多文件，可能是序列帧"
        };
    }
    
    /// <summary>
    /// 基于数字连续性判断
    /// </summary>
    private ContinuityAnalysisResult AnalyzeNumberContinuity(
        List<string> filePaths, NamingAnalysisResult namingAnalysis)
    {
        if (!namingAnalysis.HasNumbers || namingAnalysis.UniqueNumberCount < 3)
        {
            return new ContinuityAnalysisResult
            {
                Type = AssetType.Single,
                Confidence = 0.8,
                Reason = "数字不足或不连续"
            };
        }
        
        // 检测数字连续性
        var numbers = filePaths
            .Select(Path.GetFileNameWithoutExtension)
            .SelectMany(f => Regex.Matches(f, @"\d+"))
            .Select(m => int.Parse(m.Value))
            .OrderBy(n => n)
            .ToList();
        
        var continuousCount = 0;
        for (int i = 1; i < numbers.Count; i++)
        {
            if (numbers[i] == numbers[i - 1] + 1)
            {
                continuousCount++;
            }
        }
        
        var continuityRatio = (double)continuousCount / (numbers.Count - 1);
        
        if (continuityRatio >= 0.8)
        {
            return new ContinuityAnalysisResult
            {
                Type = AssetType.Sequence,
                Confidence = 0.9,
                Reason = $"数字连续性高({continuityRatio:P2})"
            };
        }
        
        return new ContinuityAnalysisResult
        {
            Type = AssetType.Single,
            Confidence = 0.6,
            Reason = $"数字连续性低({continuityRatio:P2})"
        };
    }
    
    /// <summary>
    /// 综合判断
    /// </summary>
    private AssetTypeRecognitionResult MakeDecision(
        List<string> filePaths,
        NamingAnalysisResult namingAnalysis,
        FileCountAnalysisResult countAnalysis,
        ContinuityAnalysisResult continuityAnalysis)
    {
        // 权重计算
        var scores = new Dictionary<AssetType, double>();
        scores[countAnalysis.Type] = countAnalysis.Confidence * 0.4;
        scores[continuityAnalysis.Type] = continuityAnalysis.Confidence * 0.4;
        scores[namingAnalysis.CommonPrefix != null ? AssetType.Sequence : AssetType.Single] = 
            namingAnalysis.CommonPrefix != null ? 0.2 : 0.0;
        
        // 计算总分
        var finalScores = scores.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value);
        
        // 选择最高分的类型
        var bestType = finalScores.OrderByDescending(kvp => kvp.Value).First().Key;
        var confidence = finalScores[bestType];
        
        // 构建序列帧信息
        SequenceInfo sequenceInfo = null;
        if (bestType == AssetType.Sequence && namingAnalysis.CommonPrefix != null)
        {
            sequenceInfo = new SequenceInfo
            {
                BaseName = namingAnalysis.CommonPrefix,
                FrameCount = namingAnalysis.UniqueNumberCount,
                StartFrame = namingAnalysis.MinNumber,
                EndFrame = namingAnalysis.MaxNumber,
                FramePattern = $"{namingAnalysis.CommonPrefix}_{{0:D{GetDigitCount(namingAnalysis.MaxNumber)}}}.png",
                IsContinuous = continuityAnalysis.Type == AssetType.Sequence
            };
        }
        
        return new AssetTypeRecognitionResult
        {
            Type = bestType,
            Confidence = confidence,
            Reason = $"数量:{countAnalysis.Reason}, 连续性:{continuityAnalysis.Reason}, 前缀:{(namingAnalysis.CommonPrefix ?? "无")}",
            FileNames = filePaths.Select(Path.GetFileName).ToList(),
            SequenceInfo = sequenceInfo
        };
    }
    
    /// <summary>
    /// 获取数字位数
    /// </summary>
    private int GetDigitCount(int number)
    {
        return number.ToString().Length;
    }
}

/// <summary>
/// 命名分析结果
/// </summary>
private class NamingAnalysisResult
{
    public bool HasNumbers { get; set; }
    public int UniqueNumberCount { get; set; }
    public int MinNumber { get; set; }
    public int MaxNumber { get; set; }
    public int NumberRange { get; set; }
    public List<string> Prefixes { get; set; }
    public int PrefixCount { get; set; }
    public string CommonPrefix { get; set; }
}

/// <summary>
/// 文件数量分析结果
/// </summary>
private class FileCountAnalysisResult
{
    public AssetType Type { get; set; }
    public double Confidence { get; set; }
    public string Reason { get; set; }
}

/// <summary>
/// 连续性分析结果
/// </summary>
private class ContinuityAnalysisResult
{
    public AssetType Type { get; set; }
    public double Confidence { get; set; }
    public string Reason { get; set; }
}
```

### 5.4 文件哈希计算

```csharp
/// <summary>
/// 路由表维护服务
/// </summary>
public class RouteMaintenanceService
{
    /// <summary>
    /// 同步Unity资源路径变更
    /// </summary>
    public async Task SyncAssetPathChangeAsync(string unityGuid, string newPath, string newName)
    {
        // 1. 查找对应的路由记录
        var route = await _routeRepository.GetByUnityGuidAsync(unityGuid);
        if (route == null)
        {
            _logger.LogWarning($"Route not found for Unity GUID: {unityGuid}");
            return;
        }
        
        // 2. 记录变更历史
        var history = new RouteHistory
        {
            Id = Guid.NewGuid().ToString(),
            RouteId = route.Id,
            OldUnityPath = route.UnityPath,
            NewUnityPath = newPath,
            Action = "update_path",
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            CreatedBy = "System"
        };
        await _historyRepository.CreateAsync(history);
        
        // 3. 更新路由记录
        route.UnityPath = newPath;
        route.UnityName = newName;
        route.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await _routeRepository.UpdateAsync(route);
        
        _logger.LogInformation($"Route updated for Unity GUID: {unityGuid}, New Path: {newPath}");
    }
    
    /// <summary>
    /// 替换资源
    /// </summary>
    public async Task ReplaceAssetAsync(string routeId, string newAssetId, string updatedBy)
    {
        // 1. 查找路由记录
        var route = await _routeRepository.GetByIdAsync(routeId);
        if (route == null)
        {
            throw new NotFoundException($"Route not found: {routeId}");
        }
        
        // 2. 查找新资源
        var newAsset = await _assetRepository.GetByIdAsync(newAssetId);
        if (newAsset == null)
        {
            throw new NotFoundException($"Asset not found: {newAssetId}");
        }
        
        // 3. 记录变更历史
        var history = new RouteHistory
        {
            Id = Guid.NewGuid().ToString(),
            RouteId = routeId,
            OldAssetId = route.AssetId,
            NewAssetId = newAssetId,
            Action = "replace_asset",
            Metadata = JsonSerializer.Serialize(new
            {
                OldAssetName = route.AssetName,
                NewAssetName = newAsset.Name
            }),
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            CreatedBy = updatedBy
        };
        await _historyRepository.CreateAsync(history);
        
        // 4. 更新路由记录
        var oldAssetId = route.AssetId;
        route.AssetId = newAssetId;
        route.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await _routeRepository.UpdateAsync(route);
        
        // 5. 在Unity中替换资源引用
        await _unityAssetReplacer.ReplaceAssetReferenceAsync(
            route.UnityGuid, 
            newAsset.FilePath);
        
        _logger.LogInformation($"Asset replaced in route {routeId}: {oldAssetId} -> {newAssetId}");
    }
}
```

### 5.3 文件哈希计算

```csharp
/// <summary>
/// 文件哈希计算器
/// </summary>
public class FileHashCalculator
{
    /// <summary>
    /// 计算文件SHA256哈希
    /// </summary>
    public async Task<string> CalculateHashAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        
        var hashBytes = await sha256.ComputeHashAsync(stream);
        var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        
        return hashString;
    }
    
    /// <summary>
    /// 批量计算文件哈希
    /// </summary>
    public async Task<Dictionary<string, string>> CalculateHashesAsync(List<string> filePaths)
    {
        var results = new Dictionary<string, string>();
        var tasks = filePaths.Select(async path =>
        {
            var hash = await CalculateHashAsync(path);
            return new { Path = path, Hash = hash };
        });
        
        var completed = await Task.WhenAll(tasks);
        foreach (var item in completed)
        {
            results[item.Path] = item.Hash;
        }
        
        return results;
    }
}
```

### 5.4 资源去重算法

```csharp
/// <summary>
/// 资源去重服务
/// </summary>
public class AssetDeduplicationService
{
    /// <summary>
    /// 检查资源是否已存在
    /// </summary>
    public async Task<Asset> CheckDuplicateAsync(string fileHash)
    {
        return await _assetRepository.GetByFileHashAsync(fileHash);
    }
    
    /// <summary>
    /// 批量去重
    /// </summary>
    public async Task<Dictionary<string, Asset>> BatchCheckDuplicatesAsync(List<string> fileHashes)
    {
        var existingAssets = await _assetRepository.GetByFileHashesAsync(fileHashes);
        
        return existingAssets.ToDictionary(a => a.FileHash);
    }
}
```

## 6. 配置管理

### 6.1 数据库配置

```csharp
/// <summary>
/// 数据库配置
/// </summary>
public class DatabaseConfig
{
    public string ConnectionString { get; set; }
    public string DatabaseType { get; set; }  // "SQLite" or "PostgreSQL"
    public int CommandTimeout { get; set; } = 30;
    public bool EnableLogging { get; set; } = true;
}
```

### 6.2 文件存储配置

```csharp
/// <summary>
/// 文件存储配置
/// </summary>
public class FileStorageConfig
{
    public string BasePath { get; set; }
    public string AssetsFolder { get; set; } = "Assets";
    public string StyledFolder { get; set; } = "Styled";
    public int MaxFileSize { get; set; } = 100 * 1024 * 1024;  // 100MB
    public List<string> AllowedExtensions { get; set; } = new List<string>
    {
        ".png", ".jpg", ".jpeg", ".psd", ".tga", ".bmp",
        ".mp3", ".wav", ".ogg", ".aac"
    };
}
```

### 6.3 Unity集成配置

```csharp
/// <summary>
/// Unity集成配置
/// </summary>
public class UnityIntegrationConfig
{
    public bool EnableAutoSync { get; set; } = true;
    public int SyncInterval { get; set; } = 5;  // 秒
    public bool EnablePathMonitoring { get; set; } = true;
    public string DefaultImportPath { get; set; } = "Assets/ArtAssets";
}
```
