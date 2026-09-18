using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using Microsoft.AspNetCore.Http;

namespace JellyfinPluginDeoVR.Utilities;

/// <summary>
/// URL 相关操作的辅助类。
/// </summary>
public static class UrlHelper
{
    /// <summary>
    /// 从当前 HTTP 上下文获取可访问的服务器 URL。
    /// </summary>
    /// <param name="context">当前 HTTP 上下文。</param>
    /// <returns>包含协议（Scheme）、主机（Host）和基础路径（PathBase）的完整服务器 URL。</returns>
    public static string GetServerUrl(HttpContext? context)
    {
        var req = context?.Request;
        if (req == null)
        {
            return string.Empty;
        }

        return $"{req.Scheme}://{req.Host}{req.PathBase}";
    }
    
    /// <summary>
    /// 获取 Jellyfin 内部基础 URL（用于本地内部请求）。
    /// </summary>
    /// <param name="config">服务器配置管理器。</param>
    /// <returns>内部基础 URL 字符串。</returns>
    public static string GetInternalBaseUrl(IServerConfigurationManager config)
    {
        var options = config.GetNetworkConfiguration();
        var protocol = options.RequireHttps ? "https" : "http";
        var port = options.RequireHttps ? options.InternalHttpsPort : options.InternalHttpPort;

        return $"{protocol}://localhost:{port}";
    }
}
