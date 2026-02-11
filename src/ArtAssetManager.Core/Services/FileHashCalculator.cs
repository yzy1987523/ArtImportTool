using System.Security.Cryptography;

namespace ArtAssetManager.Core.Services;

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
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        using var sha256 = SHA256.Create();
        await using var stream = File.OpenRead(filePath);
        
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
            try
            {
                var hash = await CalculateHashAsync(path);
                return new { Path = path, Hash = hash, Success = true, Error = (string?)null };
            }
            catch (Exception ex)
            {
                return new { Path = path, Hash = string.Empty, Success = false, Error = ex.Message };
            }
        });
        
        var completed = await Task.WhenAll(tasks);
        foreach (var item in completed)
        {
            if (item.Success)
            {
                results[item.Path] = item.Hash;
            }
        }
        
        return results;
    }
}
