using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace DeoVRDeeplink.Utilities;

public class IpWhitelistAttribute : TypeFilterAttribute
{
    public IpWhitelistAttribute() : base(typeof(IpWhitelistFilter))
    {
    }
}

public class IpWhitelistFilter : IAuthorizationFilter
{
    private readonly ILogger<IpWhitelistFilter> _logger;

    public IpWhitelistFilter(ILogger<IpWhitelistFilter> logger)
    {
        _logger = logger;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var clientIp = context.HttpContext.Connection.RemoteIpAddress;

        if (clientIp == null)
        {
            _logger.LogWarning("Unable to determine client IP address");
            context.Result = new ForbidResult();
            return;
        }

        var config = DeoVrDeeplinkPlugin.Instance?.Configuration;
        if (config == null || !config.EnableIpRestriction || config.AllowedIpRanges == null || !config.AllowedIpRanges.Any())
        {
            // 若未启用 IP 限制或未配置 IP 地址范围，则允许所有访问
            return;
        }

        var isAllowed = config.AllowedIpRanges.Any(clientIp.IsInCidrRange);

        if (!isAllowed)
        {
            _logger.LogWarning("Unauthorized access attempt from IP: {IpAddress}", clientIp);
            context.Result = new ForbidResult();
        }
    }
}


/// <summary>
/// IP 地址操作的扩展方法。
/// </summary>
public static class IpAddressExtensions
{
    /// <summary>
    /// 检查指定 IP 地址是否位于指定的 CIDR 范围内。
    /// </summary>
    /// <param name="address">待检查的 IP 地址。</param>
    /// <param name="cidrNotation">CIDR 格式字符串（例如 "192.168.1.0/24"）。</param>
    /// <returns>若该 IP 在 CIDR 范围内则返回 true，否则返回 false。</returns>
    public static bool IsInCidrRange(this IPAddress address, string cidrNotation)
    {
        if (string.IsNullOrWhiteSpace(cidrNotation))
            return false;

        try
        {
            // 解析 CIDR 格式字符串
            var parts = cidrNotation.Split('/');
            if (parts.Length != 2)
                return false;

            // 解析网络地址部分
            if (!IPAddress.TryParse(parts[0], out var network))
                return false;

            // 解析前缀长度部分
            if (!int.TryParse(parts[1], out int prefixLength))
                return false;

            // 确保比较的是相同的地址族（IPv4 或 IPv6）
            if (address.AddressFamily != network.AddressFamily)
                return false;

            // 获取两个地址的字节数组
            var addressBytes = address.GetAddressBytes();
            var networkBytes = network.GetAddressBytes();

            // 根据地址族验证最大前缀长度
            var maxPrefixLength = address.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
            
            if (prefixLength < 0 || prefixLength > maxPrefixLength)
                return false;

            // 计算前缀中包含多少个整字节
            var byteCount = prefixLength / 8;
            
            // 检查整字节
            for (var i = 0; i < byteCount && i < networkBytes.Length; i++)
            {
                if (addressBytes[i] != networkBytes[i])
                    return false;
            }

            // 若前缀长度不是 8 的倍数，则需要检查剩余的位
            var remainingBits = prefixLength % 8;
            if (remainingBits <= 0 || byteCount >= networkBytes.Length) return true;
            // 为剩余位创建掩码
            var mask = (byte)(0xFF << (8 - remainingBits));

            // 应用掩码并进行比较
            return (addressBytes[byteCount] & mask) == (networkBytes[byteCount] & mask);
        }
        catch (Exception)
        {
            return false;
        }
    }
}