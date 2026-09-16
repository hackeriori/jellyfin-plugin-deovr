using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using Microsoft.AspNetCore.Http;

namespace DeoVRDeeplink.Utilities;

/// <summary>
/// Helper class for URL-related operations.
/// </summary>
public static class UrlHelper
{
    /// <summary>
    /// Gets the accessible server URL from the current HTTP context.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>The full server URL with scheme, host, and path base.</returns>
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
    /// Gets the internal Jellyfin base URL (used for local requests).
    /// </summary>
    /// <param name="config">Server configuration manager.</param>
    /// <returns>Internal base URL string.</returns>
    public static string GetInternalBaseUrl(IServerConfigurationManager config)
    {
        var options = config.GetNetworkConfiguration();
        var protocol = options.RequireHttps ? "https" : "http";
        var port = options.RequireHttps ? options.InternalHttpsPort : options.InternalHttpPort;

        return $"{protocol}://localhost:{port}";
    }
}
