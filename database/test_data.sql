-- 测试数据
-- 用于验证数据库Schema和基本功能

-- ============================================
-- 测试资源数据
-- ============================================

-- 测试资源1: PNG图片
INSERT INTO ArtAssets (id, name, file_path, file_type, file_size, file_hash, width, height, metadata, created_at, updated_at, is_deleted)
VALUES (
    'test-asset-001',
    'character_idle.png',
    'storage/assets/test-asset-001.png',
    'png',
    102400,
    'a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6',
    512,
    512,
    '{"source": "test", "artist": "test_artist"}',
    strftime('%s', 'now'),
    strftime('%s', 'now'),
    0
);

-- 测试资源2: JPG图片
INSERT INTO ArtAssets (id, name, file_path, file_type, file_size, file_hash, width, height, metadata, created_at, updated_at, is_deleted)
VALUES (
    'test-asset-002',
    'background_forest.jpg',
    'storage/assets/test-asset-002.jpg',
    'jpg',
    204800,
    'b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6a1',
    1920,
    1080,
    '{"source": "test", "artist": "test_artist"}',
    strftime('%s', 'now'),
    strftime('%s', 'now'),
    0
);

-- 测试资源3: MP3音频
INSERT INTO ArtAssets (id, name, file_path, file_type, file_size, file_hash, duration, metadata, created_at, updated_at, is_deleted)
VALUES (
    'test-asset-003',
    'bgm_menu.mp3',
    'storage/assets/test-asset-003.mp3',
    'mp3',
    512000,
    'c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6a1b2',
    120000,
    '{"source": "test", "composer": "test_composer"}',
    strftime('%s', 'now'),
    strftime('%s', 'now'),
    0
);

-- ============================================
-- 测试Project数据
-- ============================================

INSERT INTO Projects (id, name, description, unity_path, created_at, updated_at, is_deleted)
VALUES (
    'test-project-001',
    'Test Game Project',
    'A test project for validation',
    'Assets/ArtAssets/TestProject',
    strftime('%s', 'now'),
    strftime('%s', 'now'),
    0
);

-- ============================================
-- 测试资源Tag关联
-- ============================================

-- character_idle.png -> org, type_character
INSERT INTO AssetTags (asset_id, tag_id, created_at, created_by)
SELECT 'test-asset-001', id, strftime('%s', 'now'), 'test_user'
FROM Tags WHERE name = 'org';

INSERT INTO AssetTags (asset_id, tag_id, created_at, created_by)
SELECT 'test-asset-001', id, strftime('%s', 'now'), 'test_user'
FROM Tags WHERE name = 'type_character';

-- background_forest.jpg -> org, type_scene
INSERT INTO AssetTags (asset_id, tag_id, created_at, created_by)
SELECT 'test-asset-002', id, strftime('%s', 'now'), 'test_user'
FROM Tags WHERE name = 'org';

INSERT INTO AssetTags (asset_id, tag_id, created_at, created_by)
SELECT 'test-asset-002', id, strftime('%s', 'now'), 'test_user'
FROM Tags WHERE name = 'type_scene';

-- bgm_menu.mp3 -> org, type_audio
INSERT INTO AssetTags (asset_id, tag_id, created_at, created_by)
SELECT 'test-asset-003', id, strftime('%s', 'now'), 'test_user'
FROM Tags WHERE name = 'org';

INSERT INTO AssetTags (asset_id, tag_id, created_at, created_by)
SELECT 'test-asset-003', id, strftime('%s', 'now'), 'test_user'
FROM Tags WHERE name = 'type_audio';

-- ============================================
-- 测试Project资源关联
-- ============================================

INSERT INTO ProjectAssets (project_id, asset_id, import_name, import_path, is_original, created_at)
VALUES (
    'test-project-001',
    'test-asset-001',
    'character_idle',
    'Assets/ArtAssets/TestProject/Characters/character_idle.png',
    1,
    strftime('%s', 'now')
);

INSERT INTO ProjectAssets (project_id, asset_id, import_name, import_path, is_original, created_at)
VALUES (
    'test-project-001',
    'test-asset-002',
    'background_forest',
    'Assets/ArtAssets/TestProject/Backgrounds/background_forest.jpg',
    1,
    strftime('%s', 'now')
);
