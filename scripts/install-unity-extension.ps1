# Unity扩展安装脚本
# 用法: .\scripts\install-unity-extension.ps1 -UnityProjectPath "C:\Path\To\Your\UnityProject"

param(
    [Parameter(Mandatory=$true)]
    [string]$UnityProjectPath
)

Write-Host "=== Art Asset Manager Unity Extension Installer ===" -ForegroundColor Cyan
Write-Host ""

# 验证Unity项目路径
if (-not (Test-Path $UnityProjectPath)) {
    Write-Host "Error: Unity project path not found: $UnityProjectPath" -ForegroundColor Red
    exit 1
}

$assetsPath = Join-Path $UnityProjectPath "Assets"
if (-not (Test-Path $assetsPath)) {
    Write-Host "Error: Assets folder not found. Is this a valid Unity project?" -ForegroundColor Red
    exit 1
}

Write-Host "Unity Project: $UnityProjectPath" -ForegroundColor Green
Write-Host ""

# 1. 复制Unity扩展文件
Write-Host "[1/3] Copying Unity extension files..." -ForegroundColor Yellow
$sourceExtension = "src\ArtAssetManager.Unity"
$targetExtension = Join-Path $assetsPath "ArtAssetManager.Unity"

if (Test-Path $targetExtension) {
    Write-Host "  Removing existing extension..." -ForegroundColor Gray
    Remove-Item -Path $targetExtension -Recurse -Force
}

Copy-Item -Path $sourceExtension -Destination $targetExtension -Recurse
Write-Host "  Extension files copied successfully" -ForegroundColor Green

# 2. 复制SQLite DLL文件
Write-Host "[2/3] Copying SQLite dependencies..." -ForegroundColor Yellow
$pluginsPath = Join-Path $assetsPath "Plugins"
if (-not (Test-Path $pluginsPath)) {
    New-Item -Path $pluginsPath -ItemType Directory | Out-Null
}

$coreBinPath = "src\ArtAssetManager.Core\bin\Debug\net6.0"
$dlls = @(
    "Microsoft.Data.Sqlite.dll",
    "SQLitePCLRaw.core.dll",
    "SQLitePCLRaw.provider.e_sqlite3.dll",
    "SQLitePCLRaw.batteries_v2.dll"
)

foreach ($dll in $dlls) {
    $sourceDll = Join-Path $coreBinPath $dll
    $targetDll = Join-Path $pluginsPath $dll
    
    if (Test-Path $sourceDll) {
        Copy-Item -Path $sourceDll -Destination $targetDll -Force
        Write-Host "  Copied: $dll" -ForegroundColor Gray
    } else {
        Write-Host "  Warning: $dll not found. Please build ArtAssetManager.Core first." -ForegroundColor Yellow
    }
}

Write-Host "  SQLite dependencies copied successfully" -ForegroundColor Green

# 3. 复制数据库文件
Write-Host "[3/3] Copying database file..." -ForegroundColor Yellow
$sourceDb = "art_asset_manager.db"
$targetDb = Join-Path $UnityProjectPath "art_asset_manager.db"

if (Test-Path $sourceDb) {
    Copy-Item -Path $sourceDb -Destination $targetDb -Force
    Write-Host "  Database copied successfully" -ForegroundColor Green
} else {
    Write-Host "  Warning: Database file not found. Please run the console app first to create it." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Installation Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor White
Write-Host "1. Open your Unity project" -ForegroundColor Gray
Write-Host "2. Wait for Unity to compile the scripts" -ForegroundColor Gray
Write-Host "3. Open the Asset Browser: Window > Art Asset Manager > Asset Browser" -ForegroundColor Gray
Write-Host ""
Write-Host "If you encounter any issues, check the README.md in Assets/ArtAssetManager.Unity/" -ForegroundColor Gray
Write-Host ""
