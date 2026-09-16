using DeoVRDeeplink.Configuration;
using DeoVRDeeplink.Model;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace DeoVRDeeplink.Utilities;

public static class DeoVrResponseBuilder
{
    /// <summary>
    /// Builds a DeoVR response for an actor/person showing all their videos
    /// </summary>
    public static DeoVrScenesResponse BuildActorResponse(
        Person person, 
        string baseUrl,
        ILibraryManager libraryManager,
        ILogger logger)
    {
        var query = new InternalItemsQuery
        {
            PersonIds = [person.Id],
            IncludeItemTypes = [BaseItemKind.Movie],
            Recursive = true,
            IsFolder = false
        };

        var response = new DeoVrScenesResponse();
        var videoList = libraryManager
            .GetItemList(query)
            .OfType<Video>()
            .Select(video => new DeoVrVideoItem
            {
                Title = video.Name,
                VideoLength = (int)((video.RunTimeTicks ?? 0) / TimeSpan.TicksPerSecond),
                VideoUrl = $"{baseUrl}/deovr/json/{video.Id}/response.json",
                ThumbnailUrl = ImageHelper.GetImageUrl(video, baseUrl)
            }).ToList();

        var scene = new DeoVrScene
        {
            Name = person.Name,
            List = videoList
        };

        response.Scenes.Add(scene);
        logger.LogInformation("Added {Count} videos from library: {Person}",
            videoList.Count, person.Name);
        
        return response;
    }

    /// <summary>
    /// Builds a DeoVR response for a video with all metadata and encodings
    /// </summary>
    public static DeoVrVideoResponse BuildVideoResponse(
        Video video, 
        string baseUrl, 
        LibraryConfiguration? libConfig,
        IChapterRepository chapterRepository,
        ILogger logger)
    {
        var runtimeSeconds = (int)((video.RunTimeTicks ?? 0) / TimeSpan.TicksPerSecond);
        var proxySecret = DeoVrDeeplinkPlugin.ProxySecret;
        var expiry = DateTimeOffset.UtcNow.AddSeconds(runtimeSeconds * 2).ToUnixTimeSeconds();

        var format = VideoFormatDetector.Detect(video, libConfig);
        var thumbnailUrl = ImageHelper.GetImageUrl(video, baseUrl);

        var encodings = video.GetMediaSources(false)
            .GroupBy(ms => ms.VideoStream.Codec ?? "unknown")
            .Select(g => new DeoVrEncoding
            {
                Name = g.Key,
                VideoSources = g.Select(ms => new DeoVrVideoSource
                {
                    Resolution = ms.VideoStream?.Height ?? 2160,
                    Url = $"{baseUrl}/deovr/proxy/{video.Id}/{ms.Id}/{expiry}/{SignatureValidator.GenerateSignature(video.Id, ms.Id, expiry, proxySecret)}/stream.mp4"
                }).ToList()
            }).ToList();

        var response = new DeoVrVideoResponse
        {
            Id = video.Id.GetHashCode(),
            Title = video.Name ?? "Unknown",
            Is3D = format.Is3D,
            VideoLength = runtimeSeconds,
            ScreenType = format.ScreenType,
            StereoMode = format.StereoMode,
            ThumbnailUrl = thumbnailUrl!,
            TimelinePreview = $"{baseUrl}/deovr/timeline/{video.Id}/4096_timelinePreview341x195.jpg",
            Encodings = encodings,
            Timestamps = GetDeoVrTimestamps(video, chapterRepository, logger)
        };

        return response;
    }

    /// <summary>
    /// Retrieves chapter timestamps, in seconds, for the item
    /// </summary>
    private static List<DeoVrTimestamps> GetDeoVrTimestamps(
        BaseItem item,
        IChapterRepository chapterRepository,
        ILogger logger)
    {
        try
        {
            var chapters = chapterRepository.GetChapters(item.Id);
            if (chapters.Count != 0)
            {
                return chapters
                    .Select(ch => new DeoVrTimestamps
                    {
                        ts = (int)(ch.StartPositionTicks / TimeSpan.TicksPerSecond),
                        name = ch.Name ?? "Untitled Chapter"
                    })
                    .ToList();
            }
            
            logger.LogDebug("No chapters found for item {ItemName}", item.Name);
            return [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting chapters for item {ItemName}", item.Name);
            return [];
        }
    }
}
