using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Xunit;
using ArtAssetManager.Core.Database;
using ArtAssetManager.Core.Services;
using ArtAssetManager.Core.Repositories;

namespace ArtAssetManager.Tests
{
    public class PerformanceTests : IDisposable
    {
        private readonly string _testDbPath;
        private readonly SqliteConnection _connection;
        private readonly AssetService _assetService;
        private readonly TagService _tagService;
        private readonly ProjectService _projectService;
        private readonly RouteService _routeService;

        public PerformanceTests()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"perf_test_{Guid.NewGuid()}.db");
            _connection = new SqliteConnection($"Data Source={_testDbPath}");
            _connection.Open();

            DatabaseInitializer.Initialize(_connection);

            _assetService = new AssetService(_connection);
            _tagService = new TagService(_connection);
            _projectService = new ProjectService(_connection);
            _routeService = new RouteService(_connection);
        }

        [Fact]
        public void Test_Query1000Records_Performance()
        {
            // Arrange: 创建1000条资源记录
            var testFiles = CreateTestFiles(1000);
            foreach (var file in testFiles)
            {
                _assetService.ImportAsset(file);
            }

            // Act: 查询所有资源
            var stopwatch = Stopwatch.StartNew();
            var repository = new AssetRepository(_connection);
            var assets = repository.GetAllAssets(limit: 1000);
            stopwatch.Stop();

            // Assert: 查询时间应小于500ms
            Assert.True(stopwatch.ElapsedMilliseconds < 500, 
                $"Query took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
            Assert.Equal(1000, assets.Count);

            // Cleanup
            CleanupTestFiles(testFiles);
        }

        [Fact]
        public void Test_BatchImport100Assets_Performance()
        {
            // Arrange: 创建100个测试文件
            var testFiles = CreateTestFiles(100);

            // Act: 批量导入
            var stopwatch = Stopwatch.StartNew();
            var results = _assetService.BatchImportAssets(testFiles);
            stopwatch.Stop();

            // Assert: 导入时间应小于30秒
            Assert.True(stopwatch.ElapsedMilliseconds < 30000, 
                $"Import took {stopwatch.ElapsedMilliseconds}ms, expected < 30000ms");
            Assert.Equal(100, results.Count(r => r.Success));

            // Cleanup
            CleanupTestFiles(testFiles);
        }

        [Fact]
        public void Test_TagOperations10Assets_Performance()
        {
            // Arrange: 创建10个资源和1个标签
            var testFiles = CreateTestFiles(10);
            var assetIds = new List<string>();
            foreach (var file in testFiles)
            {
                var result = _assetService.ImportAsset(file);
                assetIds.Add(result.Asset.Id);
            }

            var tag = _tagService.CreateTag("perf_test", "type", "Performance test tag");

            // Act: 为10个资源添加标签
            var stopwatch = Stopwatch.StartNew();
            foreach (var assetId in assetIds)
            {
                _tagService.AddTagToAsset(assetId, tag.Id);
            }
            stopwatch.Stop();

            // Assert: 操作时间应小于5秒
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
                $"Tag operations took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");

            // Cleanup
            CleanupTestFiles(testFiles);
        }

        [Fact]
        public void Test_RouteTableUpdate_Performance()
        {
            // Arrange: 创建资源和路由
            var testFile = CreateTestFiles(1).First();
            var asset = _assetService.ImportAsset(testFile).Asset;
            var project = _projectService.CreateProject("PerfTest", "Performance test project", "Assets/PerfTest");
            var route = _routeService.CreateRoute(asset.Id, project.Id, Guid.NewGuid().ToString(), 
                "Assets/Test.png", "Test.png");

            // Act: 更新路由路径
            var stopwatch = Stopwatch.StartNew();
            _routeService.UpdateRoutePath(route.Id, "Assets/NewPath/Test.png", "Test.png");
            stopwatch.Stop();

            // Assert: 更新时间应小于1秒
            Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
                $"Route update took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");

            // Cleanup
            CleanupTestFiles(new[] { testFile });
        }

        [Fact]
        public void Test_BatchReplace10Assets_Performance()
        {
            // Arrange: 创建10个资源和路由
            var testFiles = CreateTestFiles(10);
            var routeIds = new List<string>();
            var project = _projectService.CreateProject("BatchTest", "Batch test project", "Assets/BatchTest");

            foreach (var file in testFiles)
            {
                var asset = _assetService.ImportAsset(file).Asset;
                var route = _routeService.CreateRoute(asset.Id, project.Id, Guid.NewGuid().ToString(), 
                    $"Assets/{asset.Name}", asset.Name);
                routeIds.Add(route.Id);
            }

            // 创建新资源用于替换
            var newFile = CreateTestFiles(1).First();
            var newAsset = _assetService.ImportAsset(newFile).Asset;

            // Act: 批量替换
            var stopwatch = Stopwatch.StartNew();
            var count = _routeService.BatchReplaceAssets(routeIds, newAsset.Id);
            stopwatch.Stop();

            // Assert: 批量替换时间应小于5秒
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
                $"Batch replace took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
            Assert.Equal(10, count);

            // Cleanup
            CleanupTestFiles(testFiles.Concat(new[] { newFile }));
        }

        [Fact]
        public void Test_ComplexQuery_Performance()
        {
            // Arrange: 创建100个资源，添加多个标签
            var testFiles = CreateTestFiles(100);
            var tag1 = _tagService.CreateTag("tag1", "type", "Tag 1");
            var tag2 = _tagService.CreateTag("tag2", "type", "Tag 2");

            for (int i = 0; i < testFiles.Count; i++)
            {
                var asset = _assetService.ImportAsset(testFiles[i]).Asset;
                _tagService.AddTagToAsset(asset.Id, tag1.Id);
                if (i % 2 == 0)
                {
                    _tagService.AddTagToAsset(asset.Id, tag2.Id);
                }
            }

            // Act: 复杂查询（多标签组合）
            var stopwatch = Stopwatch.StartNew();
            var repository = new AssetRepository(_connection);
            var assets = repository.GetAssetsByTags(new[] { tag1.Id, tag2.Id });
            stopwatch.Stop();

            // Assert: 查询时间应小于500ms
            Assert.True(stopwatch.ElapsedMilliseconds < 500, 
                $"Complex query took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
            Assert.Equal(50, assets.Count); // 50个资源有两个标签

            // Cleanup
            CleanupTestFiles(testFiles);
        }

        [Fact]
        public void Test_NameMatching_Performance()
        {
            // Arrange: 创建100个资源
            var testFiles = CreateTestFiles(100);
            var candidates = new List<(string Id, string Name)>();

            foreach (var file in testFiles)
            {
                var asset = _assetService.ImportAsset(file).Asset;
                candidates.Add((asset.Id, asset.Name));
            }

            var nameMatchingService = new NameMatchingService();

            // Act: 执行100次名称匹配
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                var targetName = $"test_file_{i}.png";
                nameMatchingService.FindBestMatch(targetName, candidates);
            }
            stopwatch.Stop();

            // Assert: 100次匹配应小于5秒
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
                $"100 name matches took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");

            // Cleanup
            CleanupTestFiles(testFiles);
        }

        [Fact]
        public void Test_ConcurrentOperations_Safety()
        {
            // Arrange: 创建测试文件
            var testFiles = CreateTestFiles(10);
            var exceptions = new List<Exception>();

            // Act: 并发导入资源
            System.Threading.Tasks.Parallel.ForEach(testFiles, file =>
            {
                try
                {
                    _assetService.ImportAsset(file);
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });

            // Assert: 不应有异常
            Assert.Empty(exceptions);

            // 验证所有资源都已导入
            var repository = new AssetRepository(_connection);
            var assets = repository.GetAllAssets(limit: 100);
            Assert.True(assets.Count >= 10);

            // Cleanup
            CleanupTestFiles(testFiles);
        }

        [Fact]
        public void Test_MemoryUsage_LargeDataset()
        {
            // Arrange: 记录初始内存
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var initialMemory = GC.GetTotalMemory(false);

            // Act: 创建和导入1000个资源
            var testFiles = CreateTestFiles(1000);
            foreach (var file in testFiles)
            {
                _assetService.ImportAsset(file);
            }

            // 查询所有资源
            var repository = new AssetRepository(_connection);
            var assets = repository.GetAllAssets(limit: 1000);

            // 记录最终内存
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var finalMemory = GC.GetTotalMemory(false);

            var memoryIncrease = (finalMemory - initialMemory) / 1024.0 / 1024.0; // MB

            // Assert: 内存增长应小于100MB
            Assert.True(memoryIncrease < 100, 
                $"Memory increased by {memoryIncrease:F2}MB, expected < 100MB");

            // Cleanup
            CleanupTestFiles(testFiles);
        }

        private List<string> CreateTestFiles(int count)
        {
            var files = new List<string>();
            var tempDir = Path.Combine(Path.GetTempPath(), $"perf_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            for (int i = 0; i < count; i++)
            {
                var filePath = Path.Combine(tempDir, $"test_file_{i}.png");
                
                // 创建一个小的测试文件（1KB）
                var content = new byte[1024];
                new Random().NextBytes(content);
                File.WriteAllBytes(filePath, content);
                
                files.Add(filePath);
            }

            return files;
        }

        private void CleanupTestFiles(IEnumerable<string> files)
        {
            var directories = files.Select(f => Path.GetDirectoryName(f)).Distinct();
            foreach (var dir in directories)
            {
                try
                {
                    if (Directory.Exists(dir))
                    {
                        Directory.Delete(dir, true);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();

            try
            {
                if (File.Exists(_testDbPath))
                {
                    File.Delete(_testDbPath);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
