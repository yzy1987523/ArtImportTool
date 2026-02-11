-- 艺术资源管理系统数据库Schema
-- SQLite 3.x
-- 创建日期: 2026-02-11

-- ============================================
-- 1. 资源表 (ArtAssets)
-- ============================================
CREATE TABLE IF NOT EXISTS ArtAssets (
    id TEXT PRIMARY KEY,                    -- UUID
    name TEXT NOT NULL,                     -- 资源名称
    file_path TEXT NOT NULL,                -- 文件存储路径
    file_type TEXT NOT NULL,                -- 文件类型（png, jpg, mp3, wav等）
    file_size INTEGER NOT NULL,             -- 文件大小（字节）
    file_hash TEXT NOT NULL UNIQUE,         -- 文件哈希（SHA256，用于去重）
    width INTEGER,                          -- 图片宽度（仅图片）
    height INTEGER,                         -- 图片高度（仅图片）
    duration INTEGER,                       -- 音频时长（仅音频，毫秒）
    metadata TEXT,                          -- 元数据（JSON格式）
    created_at INTEGER NOT NULL,            -- 创建时间（Unix时间戳）
    updated_at INTEGER NOT NULL,            -- 更新时间（Unix时间戳）
    is_deleted INTEGER DEFAULT 0            -- 是否删除（0=否，1=是）
);

-- 索引
CREATE INDEX IF NOT EXISTS idx_artassets_file_hash ON ArtAssets(file_hash);
CREATE INDEX IF NOT EXISTS idx_artassets_file_type ON ArtAssets(file_type);
CREATE INDEX IF NOT EXISTS idx_artassets_created_at ON ArtAssets(created_at);
CREATE INDEX IF NOT EXISTS idx_artassets_is_deleted ON ArtAssets(is_deleted);

-- ============================================
-- 2. 标签表 (Tags)
-- ============================================
CREATE TABLE IF NOT EXISTS Tags (
    id INTEGER PRIMARY KEY AUTOINCREMENT,   -- 自增ID
    name TEXT NOT NULL UNIQUE,              -- 标签名称
    category TEXT NOT NULL,                 -- 标签分类（org, style, type, status）
    description TEXT,                       -- 标签描述
    color TEXT,                            -- 标签颜色（十六进制）
    sort_order INTEGER DEFAULT 0,          -- 排序顺序
    created_at INTEGER NOT NULL,           -- 创建时间
    updated_at INTEGER NOT NULL            -- 更新时间
);

-- 索引
CREATE INDEX IF NOT EXISTS idx_tags_category ON Tags(category);
CREATE INDEX IF NOT EXISTS idx_tags_sort_order ON Tags(sort_order);

-- ============================================
-- 3. 资源标签关联表 (AssetTags)
-- ============================================
CREATE TABLE IF NOT EXISTS AssetTags (
    id INTEGER PRIMARY KEY AUTOINCREMENT,   -- 自增ID
    asset_id TEXT NOT NULL,                -- 资源ID（外键 → ArtAssets.id）
    tag_id INTEGER NOT NULL,               -- 标签ID（外键 → Tags.id）
    created_at INTEGER NOT NULL,           -- 创建时间
    created_by TEXT,                       -- 创建人
    UNIQUE(asset_id, tag_id),
    FOREIGN KEY (asset_id) REFERENCES ArtAssets(id) ON DELETE CASCADE,
    FOREIGN KEY (tag_id) REFERENCES Tags(id) ON DELETE CASCADE
);

-- 索引
CREATE INDEX IF NOT EXISTS idx_assettags_asset_id ON AssetTags(asset_id);
CREATE INDEX IF NOT EXISTS idx_assettags_tag_id ON AssetTags(tag_id);

-- ============================================
-- 4. 项目表 (Projects)
-- ============================================
CREATE TABLE IF NOT EXISTS Projects (
    id TEXT PRIMARY KEY,                    -- UUID
    name TEXT NOT NULL,                     -- Project名称
    description TEXT,                       -- Project描述
    unity_path TEXT NOT NULL,               -- Unity中的路径
    created_at INTEGER NOT NULL,            -- 创建时间
    updated_at INTEGER NOT NULL,            -- 更新时间
    is_deleted INTEGER DEFAULT 0            -- 是否删除
);

-- 索引
CREATE INDEX IF NOT EXISTS idx_projects_unity_path ON Projects(unity_path);
CREATE INDEX IF NOT EXISTS idx_projects_is_deleted ON Projects(is_deleted);

-- ============================================
-- 5. 项目资源关联表 (ProjectAssets)
-- ============================================
CREATE TABLE IF NOT EXISTS ProjectAssets (
    id INTEGER PRIMARY KEY AUTOINCREMENT,   -- 自增ID
    project_id TEXT NOT NULL,              -- Project ID（外键 → Projects.id）
    asset_id TEXT NOT NULL,                -- 资源ID（外键 → ArtAssets.id）
    import_name TEXT NOT NULL,             -- 导入时的名称
    import_path TEXT NOT NULL,             -- 导入时的路径
    is_original INTEGER DEFAULT 1,         -- 是否为原始资源（1=是，0=否）
    created_at INTEGER NOT NULL,           -- 创建时间
    UNIQUE(project_id, asset_id),
    FOREIGN KEY (project_id) REFERENCES Projects(id) ON DELETE CASCADE,
    FOREIGN KEY (asset_id) REFERENCES ArtAssets(id) ON DELETE CASCADE
);

-- 索引
CREATE INDEX IF NOT EXISTS idx_projectassets_project_id ON ProjectAssets(project_id);
CREATE INDEX IF NOT EXISTS idx_projectassets_asset_id ON ProjectAssets(asset_id);
CREATE INDEX IF NOT EXISTS idx_projectassets_is_original ON ProjectAssets(is_original);

-- ============================================
-- 6. 风格迁移表 (StyleMigrations)
-- ============================================
CREATE TABLE IF NOT EXISTS StyleMigrations (
    id TEXT PRIMARY KEY,                    -- UUID
    original_asset_id TEXT NOT NULL,       -- 原始资源ID（外键 → ArtAssets.id）
    styled_asset_id TEXT NOT NULL,         -- 风格化资源ID（外键 → ArtAssets.id）
    style_tag TEXT NOT NULL,               -- 风格标签（如style_cartoon）
    project_id TEXT,                       -- 所属Project ID（外键 → Projects.id）
    metadata TEXT,                         -- 元数据（JSON格式）
    created_at INTEGER NOT NULL,           -- 创建时间
    created_by TEXT,                       -- 创建人
    FOREIGN KEY (original_asset_id) REFERENCES ArtAssets(id) ON DELETE CASCADE,
    FOREIGN KEY (styled_asset_id) REFERENCES ArtAssets(id) ON DELETE CASCADE,
    FOREIGN KEY (project_id) REFERENCES Projects(id) ON DELETE SET NULL
);

-- 索引
CREATE INDEX IF NOT EXISTS idx_stylemigrations_original_asset_id ON StyleMigrations(original_asset_id);
CREATE INDEX IF NOT EXISTS idx_stylemigrations_styled_asset_id ON StyleMigrations(styled_asset_id);
CREATE INDEX IF NOT EXISTS idx_stylemigrations_style_tag ON StyleMigrations(style_tag);
CREATE INDEX IF NOT EXISTS idx_stylemigrations_project_id ON StyleMigrations(project_id);

-- ============================================
-- 7. Unity路由表 (UnityRoutes)
-- ============================================
CREATE TABLE IF NOT EXISTS UnityRoutes (
    id TEXT PRIMARY KEY,                    -- UUID
    asset_id TEXT NOT NULL,                -- 资源ID（外键 → ArtAssets.id）
    project_id TEXT NOT NULL,              -- Project ID（外键 → Projects.id）
    unity_guid TEXT NOT NULL,              -- Unity资源的GUID
    unity_path TEXT NOT NULL,              -- Unity中的路径
    unity_name TEXT NOT NULL,              -- Unity中的名称
    original_import_path TEXT,             -- 首次导入路径
    is_active INTEGER DEFAULT 1,           -- 是否为当前激活的资源
    created_at INTEGER NOT NULL,           -- 创建时间
    updated_at INTEGER NOT NULL,           -- 更新时间
    UNIQUE(unity_guid),
    FOREIGN KEY (asset_id) REFERENCES ArtAssets(id) ON DELETE CASCADE,
    FOREIGN KEY (project_id) REFERENCES Projects(id) ON DELETE CASCADE
);

-- 索引
CREATE INDEX IF NOT EXISTS idx_unityroutes_asset_id ON UnityRoutes(asset_id);
CREATE INDEX IF NOT EXISTS idx_unityroutes_project_id ON UnityRoutes(project_id);
CREATE INDEX IF NOT EXISTS idx_unityroutes_unity_guid ON UnityRoutes(unity_guid);
CREATE INDEX IF NOT EXISTS idx_unityroutes_is_active ON UnityRoutes(is_active);

-- ============================================
-- 8. 路由历史表 (RouteHistory)
-- ============================================
CREATE TABLE IF NOT EXISTS RouteHistory (
    id TEXT PRIMARY KEY,                    -- UUID
    route_id TEXT NOT NULL,                -- 路由ID（外键 → UnityRoutes.id）
    old_asset_id TEXT,                     -- 旧资源ID
    new_asset_id TEXT,                     -- 新资源ID
    old_unity_path TEXT,                   -- 旧路径
    new_unity_path TEXT,                   -- 新路径
    action TEXT NOT NULL,                  -- 操作类型（create, update, replace, delete）
    metadata TEXT,                         -- 元数据（JSON格式）
    created_at INTEGER NOT NULL,           -- 创建时间
    created_by TEXT,                       -- 操作人
    FOREIGN KEY (route_id) REFERENCES UnityRoutes(id) ON DELETE CASCADE
);

-- 索引
CREATE INDEX IF NOT EXISTS idx_routehistory_route_id ON RouteHistory(route_id);
CREATE INDEX IF NOT EXISTS idx_routehistory_created_at ON RouteHistory(created_at);
CREATE INDEX IF NOT EXISTS idx_routehistory_action ON RouteHistory(action);

-- ============================================
-- 初始化默认Tags
-- ============================================
INSERT OR IGNORE INTO Tags (name, category, description, color, sort_order, created_at, updated_at) VALUES
('org', 'org', '原始占位资源', '#3498db', 1, strftime('%s', 'now'), strftime('%s', 'now')),
('style_cartoon', 'style', '卡通风格', '#e74c3c', 10, strftime('%s', 'now'), strftime('%s', 'now')),
('style_realistic', 'style', '写实风格', '#2ecc71', 11, strftime('%s', 'now'), strftime('%s', 'now')),
('style_pixel', 'style', '像素风格', '#9b59b6', 12, strftime('%s', 'now'), strftime('%s', 'now')),
('type_ui', 'type', 'UI资源', '#f39c12', 20, strftime('%s', 'now'), strftime('%s', 'now')),
('type_character', 'type', '角色资源', '#1abc9c', 21, strftime('%s', 'now'), strftime('%s', 'now')),
('type_scene', 'type', '场景资源', '#34495e', 22, strftime('%s', 'now'), strftime('%s', 'now')),
('type_audio', 'type', '音频资源', '#e67e22', 23, strftime('%s', 'now'), strftime('%s', 'now'));
