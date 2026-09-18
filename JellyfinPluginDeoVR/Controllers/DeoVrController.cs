using System.Net.Mime;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using JellyfinPluginDeoVR.Configuration;
using JellyfinPluginDeoVR.Model;
using JellyfinPluginDeoVR.Utilities;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JellyfinPluginDeoVR.Controllers;

[ApiController]
[Route("deovr")]
public class DeoVrController : ControllerBase
{
    private readonly IServerConfigurationManager _config;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<DeoVrController> _logger;

    public DeoVrController(
        ILogger<DeoVrController> logger,
        ILibraryManager libraryManager,
        IHttpContextAccessor httpContextAccessor,
        IServerConfigurationManager config)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _httpContextAccessor = httpContextAccessor;
        _config = config;
    }
    
    /// <summary>
    ///     返回与 DeoVR 深度链接兼容的 JSON 结构。
    /// </summary>
    [HttpGet]
    [Produces(MediaTypeNames.Application.Json)]
    [IpWhitelist]
    public async Task<IActionResult> GetScenesAsync()
    {
        try
        {
            var baseUrl = UrlHelper.GetServerUrl(_httpContextAccessor.HttpContext);
            var configLibraries = JellyfinPluginDeoVRPlugin.Instance!.Configuration.Libraries;
            var librariesWithConfig = GetEnabledLibrariesWithConfig(configLibraries).ToArray();

            if (librariesWithConfig.Length == 0)
            {
                _logger.LogWarning("No libraries found");
                return Ok(new DeoVrScenesResponse());
            }

            var response = new DeoVrScenesResponse();
            var totalVideosCount = 0;

            // 并行处理媒体库以提升性能
            var sceneTasks = librariesWithConfig.Select(async libraryWithConfig =>
            {
                var (library, config) = libraryWithConfig;
                var videos = (await GetVideosFromLibraryAsync(library, config).ConfigureAwait(false)).ToList();

                if (videos.Count == 0)
                {
                    _logger.LogInformation("No videos found in library: {LibraryName}", library.Name);
                    return null;
                }

                var videoList = videos.Select(video => new DeoVrVideoItem
                {
                    Title = video.Name,
                    VideoLength = GetVideoDuration(video),
                    VideoUrl = $"{baseUrl}/deovr/json/{video.Id}/response.json",
                    ThumbnailUrl = ImageHelper.GetImageUrl(video, baseUrl)
                }).ToList();

                _logger.LogDebug("Added {Count} videos from library: {LibraryName}",
                    videoList.Count, library.Name);

                return new DeoVrScene
                {
                    Name = library.Name,
                    List = videoList
                };
            });

            var scenes = await Task.WhenAll(sceneTasks).ConfigureAwait(false);
            
            foreach (var scene in scenes.Where(s => s != null))
            {
                response.Scenes.Add(scene);
                totalVideosCount += scene.List.Count;
            }

            _logger.LogDebug("Generated DeoVR response with {TotalCount} videos from {LibraryCount} libraries",
                totalVideosCount, response.Scenes.Count);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating DeoVR scenes JSON");
            return StatusCode(StatusCodes.Status500InternalServerError, "Error generating DeoVR scenes");
        }
    }

    private IEnumerable<(Folder Library, LibraryConfiguration Config)> GetEnabledLibrariesWithConfig(
        IList<LibraryConfiguration> configLibraries)
    {
        var enabledConfigs = configLibraries.Where(lib => lib.Enabled).ToList();
        if (enabledConfigs.Count == 0)
            return [];
        
        var enabledLibraryIds = enabledConfigs.Select(lib => lib.Id).ToHashSet();

        var allLibraries = _libraryManager.GetUserRootFolder()
            .Children
            .OfType<CollectionFolder>()
            .Where(lib => enabledLibraryIds.Contains(lib.Id))
            .ToList();
        
        return allLibraries.Join(
            enabledConfigs,
            library => library.Id,
            config => config.Id,
            (library, config) => ((Folder)library, config)
        );
    }

    private async Task<IEnumerable<Video>> GetVideosFromLibraryAsync(Folder library, LibraryConfiguration config)
    {
        var query = new InternalItemsQuery
        {
            ParentId = library.Id,
            IncludeItemTypes = [BaseItemKind.Movie],
            Recursive = true,
            IsFolder = false,
            OrderBy = GetOrderBySortType(config)
        };
    
        var items = _libraryManager.GetItemList(query);
        return await Task.FromResult( // 根据 ID 去重 - 避开 Jellyfin 10.11.0 的已知重复项问题？
            items.OfType<Video>()
            .GroupBy(v => v.Id)
            .Select(g => g.First())
        ).ConfigureAwait(false);
    }

    private IReadOnlyList<(ItemSortBy OrderBy, SortOrder SortOrder)> GetOrderBySortType(LibraryConfiguration config) =>
        config.SortBy switch
        {
            SortBy.Name => [(ItemSortBy.Name, config.SortOrder)],
            SortBy.Random => [(ItemSortBy.Random, config.SortOrder)],
            SortBy.DateAdded => [(ItemSortBy.DateCreated, config.SortOrder)],
            SortBy.ReleaseDate => [(ItemSortBy.PremiereDate, config.SortOrder)],
            _ => throw new ArgumentOutOfRangeException()
        };

    private static int GetVideoDuration(BaseItem video) =>
        video is Video { RunTimeTicks: not null } ticks  
            ? (int)(ticks.RunTimeTicks.Value / TimeSpan.TicksPerSecond) 
            : 0;
}