using System.Security.Cryptography;
using System.Text;

namespace DeoVRDeeplink.Utilities;

/// <summary>
/// 提供用于生成和验证视频流传输安全且具有有效期的 HMAC 签名的实用方法。
/// </summary>
public static class SignatureValidator
{
    /// <summary>
    /// 通过拼接源参数生成用于签名的原始载荷字符串。
    /// </summary>
    private static string GetSignaturePayload(object movieId, object mediaSourceId, long expiry) 
        => $"{movieId}:{mediaSourceId}:{expiry}";

    /// <summary>
    /// 使用指定的密钥计算提供数据的 HMAC-SHA256 哈希值。
    /// </summary>
    private static string SignUrl(string data, string secret) 
        => Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(data)));

    /// <summary>
    /// 为视频流请求生成完整的 HMAC-SHA256 签名。
    /// </summary>
    public static string GenerateSignature(object movieId, object mediaSourceId, long expiry, string secret) 
        => SignUrl(GetSignaturePayload(movieId, mediaSourceId, expiry), secret);

    /// <summary>
    /// 判断给定的 Unix 时间戳相比当前 UTC 时间是否已过期。
    /// </summary>
    public static bool IsExpired(long expiry) 
        => DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiry;

    /// <summary>
    /// 验证传入的签名，并输出期望的签名以便于日志记录。
    /// </summary>
    public static bool TryValidateSignature(
        object movieId, 
        object mediaSourceId, 
        long expiry, 
        string signature, 
        string secret, 
        out string expectedSignature) 
        => string.Equals(signature, expectedSignature = GenerateSignature(movieId, mediaSourceId, expiry, secret), StringComparison.OrdinalIgnoreCase);
}
