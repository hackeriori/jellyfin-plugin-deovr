using System.Net.Mime;
using DeoVRDeeplink.Configuration;
using DeoVRDeeplink.Utilities;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DeoVRDeeplink.Api;

[ApiController]
[Route("deovr")]
public class DeoVrDeeplinkController(
    ILogger<DeoVrDeeplinkController> logger,
    ILibraryManager libraryManager,
    IMediaSourceManager mediaSourceManager,
    IHttpContextAccessor httpContextAccessor,
    IServerConfigurationManager config,
    IItemRepository itemRepository,
    IChapterRepository chapterRepository) : ControllerBase
{
    private readonly IChapterRepository _chapterRepository = chapterRepository;
    private readonly IServerConfigurationManager _config = config;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IItemRepository _itemRepository = itemRepository;
    private readonly ILibraryManager _libraryManager = libraryManager;
    private readonly ILogger<DeoVrDeeplinkController> _logger = logger;
    private readonly IMediaSourceManager _mediaSourceManager = mediaSourceManager;

    /// <summary>
    ///     返回与 DeoVR 兼容的电影或人物 JSON。
    /// </summary>
    [HttpGet("json/{Id}/response.json")]
    [Produces(MediaTypeNames.Application.Json)]
    [IpWhitelist]
    public IActionResult GetDeoVrResponse(string Id)
    {
        if (!Guid.TryParse(Id, out var itemId))
            return NotFound();

        var item = _libraryManager.GetItemById(itemId);
        var baseUrl = UrlHelper.GetServerUrl(_httpContextAccessor.HttpContext);

        switch (item)
        {
            case Video video:
                try
                {
                    var libConfig = GetLibraryConfigForItem(video);
                    var response =
                        DeoVrResponseBuilder.BuildVideoResponse(video, baseUrl, libConfig, _chapterRepository, _logger);
                    return Ok(response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating DeoVR response for movie ID: {Id}", Id);
                    return StatusCode(StatusCodes.Status500InternalServerError, "Error generating DeoVR response.");
                }
            case Person person:
                try
                {
                    var response = DeoVrResponseBuilder.BuildActorResponse(person, baseUrl, _libraryManager, _logger);
                    return Ok(response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating DeoVR response for Actor ID: {Id}", Id);
                    return StatusCode(StatusCodes.Status500InternalServerError, "Error generating DeoVR response.");
                }
            default:
                return NotFound();
        }
    }

    private LibraryConfiguration? GetLibraryConfigForItem(BaseItem item)
    {
        var config = DeoVrDeeplinkPlugin.Instance!.Configuration;
        var libraries = config.Libraries;
        
        var collectionFolder = _libraryManager.GetCollectionFolders(item).FirstOrDefault();
        if (collectionFolder == null)
        {
            _logger.LogWarning("No collection folder found for item {ItemName} (Id: {ItemId})", item.Name, item.Id);
            return null;
        }
        
        var lib = libraries.FirstOrDefault(l => l.Id == collectionFolder.Id);
        if (lib != null)
        {
            _logger.LogDebug("Found library config for {CollectionFolderName} (Id: {CollectionFolderId})",
                collectionFolder.Name, collectionFolder.Id);
            return lib;
        }

        _logger.LogWarning("No library config found for library {CollectionFolderName} (Id: {CollectionFolderId})",
            collectionFolder.Name, collectionFolder.Id);
        return null;
    }

    /// <summary>
    ///     使用带签名的过期令牌安全代理视频流。
    /// </summary>
    [HttpGet("proxy/{movieId}/{mediaSourceId}/{expiry}/{signature}/stream.mp4")]
    [AllowAnonymous]
    [IpWhitelist]
    public async Task ProxyStream(string movieId, string mediaSourceId, long expiry, string signature)
    {
        if (!Guid.TryParse(movieId, out _))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.Body.FlushAsync();
            return;
        }
        
        if (SignatureValidator.IsExpired(expiry))
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            await Response.Body.FlushAsync();
            return;
        }

        // 验证签名
        var proxySecret = DeoVrDeeplinkPlugin.ProxySecret;
        if (!SignatureValidator.TryValidateSignature(movieId, mediaSourceId, expiry, signature, proxySecret, out var expectedSig))
        {
            _logger.LogWarning(
                "Proxy signature mismatch. Provided: {UserSig}, Expected: {ExpectedSig}, movieId: {MovieId}, mediaSourceId: {mediaSourceId}, expiry: {Expiry}",
                signature, expectedSig, movieId, mediaSourceId, expiry);

            Response.StatusCode = StatusCodes.Status401Unauthorized;
            await Response.Body.FlushAsync();
            return;
        }
        
        var jellyfinInternalBaseUrl = UrlHelper.GetInternalBaseUrl(_config);
        var jellyfinUrl =
            $"{jellyfinInternalBaseUrl}/Videos/{movieId}/stream.mp4?Static=true&mediaSourceId={mediaSourceId}&deviceId=JellyfinPluginDeoVR";

        var httpClient = StaticHttpClient.Instance;
        var forwardRequest = new HttpRequestMessage(HttpMethod.Get, jellyfinUrl);

        // 转发 Range 请求头以支持播放定位/拖动进度
        if (Request.Headers.TryGetValue("Range", out var rangeValues))
            foreach (var value in rangeValues)
                forwardRequest.Headers.TryAddWithoutValidation("Range", value);

        using var resp = await httpClient.SendAsync(forwardRequest, HttpCompletionOption.ResponseHeadersRead,
            HttpContext.RequestAborted);

        Response.StatusCode = (int)resp.StatusCode;

        // 将 Jellyfin 响应中的所有请求头复制到当前响应中
        foreach (var header in resp.Headers)
            Response.Headers[header.Key] = header.Value.ToArray();
        foreach (var header in resp.Content.Headers)
            Response.Headers[header.Key] = header.Value.ToArray();

        // 移除不应由用户代码设置的请求头
        Response.Headers.Remove("transfer-encoding");

        // 以大块分片方式代理内容流以提升性能，并支持请求取消
        await using var stream = await resp.Content.ReadAsStreamAsync();
        var buffer = new byte[2 * 1024 * 1024]; // 2 MB 分块大小

        try
        {
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, HttpContext.RequestAborted)) > 0)
            {
                await Response.Body.WriteAsync(buffer.AsMemory(0, bytesRead), HttpContext.RequestAborted);
                if (HttpContext.RequestAborted.IsCancellationRequested)
                    break;
                await Response.Body.FlushAsync(HttpContext.RequestAborted);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Client disconnected during streaming for movie {MovieId}", movieId);
            // 客户端断开连接时属于正常情况
        }
    }
}

public class StaticHttpClient
{
    private static readonly Lazy<HttpClient> _instance = new(() => new HttpClient
    {
        Timeout = Timeout.InfiniteTimeSpan, // 流媒体传输不设超时时间
        DefaultRequestHeaders = { ConnectionClose = false }
    });

    public static HttpClient Instance => _instance.Value;
}