using System.Diagnostics;
using System.Globalization;
using System.Text;
using DeoVRDeeplink.Configuration;
using DeoVRDeeplink.Utilities;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace DeoVRDeeplink.TimelinePreview;

public class VideoProcessor
{
    private readonly IApplicationPaths _appPaths;
    private readonly PluginConfiguration _config;
    private readonly IServerConfigurationManager _configurationManager;
    private readonly IFileSystem _fileSystem;
    private readonly ILibraryManager _libraryManager;
    private readonly ILibraryMonitor _libraryMonitor;
    private readonly ILogger<VideoProcessor> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMediaEncoder _mediaEncoder;

    public VideoProcessor(
        ILoggerFactory loggerFactory,
        ILogger<VideoProcessor> logger,
        IMediaEncoder mediaEncoder,
        IServerConfigurationManager configurationManager,
        IFileSystem fileSystem,
        IApplicationPaths appPaths,
        ILibraryMonitor libraryMonitor,
        EncodingHelper encodingHelper,
        ILibraryManager libraryManager)
    {
        _loggerFactory = loggerFactory;
        _logger = logger;
        _mediaEncoder = mediaEncoder;
        _configurationManager = configurationManager;
        _fileSystem = fileSystem;
        _appPaths = appPaths;
        _libraryMonitor = libraryMonitor;
        _libraryManager = libraryManager;
        _config = DeoVrDeeplinkPlugin.Instance!.Configuration;
    }

    public async Task Run(Video item, CancellationToken cancellationToken)
    {
        if (!EnableForItem(item)) return;

        var mediaSources = ((IHasMediaSources)item).GetMediaSources(false).ToList();

        foreach (var mediaSource in mediaSources.Where(mediaSource => item.Id.Equals(Guid.Parse(mediaSource.Id))))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Run(item, mediaSource, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task Run(Video item, MediaSourceInfo mediaSource, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing video timeline for: {ItemName}", item.Name);

            var outputPath = GetTimelineOutputPath(item);
            var ffmpegArgs = GetFFmpegArgumentsForTimeline(item, mediaSource, outputPath);

            // 检查时间轴缩略图是否已存在且为最新
            if (IsTimelineUpToDate(outputPath, mediaSource.Path))
            {
                _logger.LogDebug("Timeline image already exists and is up to date for {ItemName}", item.Name);
                return;
            }

            await RunFFmpegCommand(ffmpegArgs, cancellationToken);

            _logger.LogInformation("Timeline generation completed for: {ItemName}", item.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing timeline for: {ItemName}", item.Name);
            throw;
        }
    }

    private static string GetFFFpsForFilter(BaseItem item)
    {
        if (item is not Video { RunTimeTicks: not null } videoItem) return "0.0";
        return "fps=1/" + ((double)videoItem.RunTimeTicks.Value / TimeSpan.TicksPerSecond / 252.0)
            .ToString(CultureInfo.InvariantCulture);
    }

    private List<string> GetFFmpegArgumentsForTimeline(Video item, MediaSourceInfo mediaSource, string outputPath)
    {
        return
        [
            "-i", $"\"{mediaSource.Path}\"",
            "-vf",
            $"\"{GetFFFpsForFilter(item)}{GetFFCropFilter(item)},scale=341:195,tile=12x21:margin=0:padding=0\"",
            "-q:v", "1",
            "-y",
            $"\"{outputPath}\""
        ];
    }

    private string GetTimelineOutputPath(BaseItem item)
    {
        var fileName = $"{item.Id}.jpg";
        var timelineCachePath = Path.Combine(_appPaths.DataPath, "deovr-timeline");

        Directory.CreateDirectory(timelineCachePath);

        return Path.Combine(timelineCachePath, fileName);
    }

    private static bool IsTimelineUpToDate(string outputPath, string videoPath)
    {
        if (!File.Exists(outputPath)) return false;

        var outputInfo = new FileInfo(outputPath);
        var videoInfo = new FileInfo(videoPath);

        return outputInfo.LastWriteTime > videoInfo.LastWriteTime;
    }

    private async Task RunFFmpegCommand(List<string> args, CancellationToken cancellationToken)
    {
        var argString = string.Join(" ", args);

        // 记录完整的 FFmpeg 命令
        var fullCommand = $"{_mediaEncoder.EncoderPath} {argString}";
        _logger.LogDebug("Executing FFmpeg command: {FullCommand}", fullCommand);

        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = _mediaEncoder.EncoderPath,
                Arguments = argString,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = processStartInfo;

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data)) outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data)) errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var error = errorBuilder.ToString();
                _logger.LogError("FFmpeg failed with exit code {ProcessExitCode}: {Error}", process.ExitCode, error);
                throw new Exception($"FFmpeg process failed with exit code {process.ExitCode}");
            }

            _logger.LogInformation("FFmpeg command completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FFmpeg command execution failed");
            throw;
        }
    }

    private string GetFFCropFilter(Video video)
    {
        var libConfig = GetLibraryConfigForItem(video);
        var format = VideoFormatDetector.Detect(video, libConfig);

        return format.StereoMode switch
        {
            "sbs" => ",crop=iw/2:ih:0:0",
            "tb" => ",crop=iw:ih/2:0:0",
            _ => string.Empty
        };
    }

    private LibraryConfiguration? GetLibraryConfigForItem(BaseItem item)
    {
        var config = DeoVrDeeplinkPlugin.Instance!.Configuration;
        var libraries = config.Libraries;

        // 从 Jellyfin 获取包含该项目的媒体库文件夹（CollectionFolder）
        var collectionFolder = _libraryManager.GetCollectionFolders(item).FirstOrDefault();
        if (collectionFolder == null)
        {
            _logger.LogWarning("No collection folder found for item {ItemName} (Id: {ItemId})", item.Name, item.Id);
            return null;
        }

        // 根据 GUID 匹配插件中配置的媒体库
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

    private static bool EnableForItem(BaseItem item)
    {
        // 检查该项目是否为视频
        if (item is not Video) return false;

        // 检查项目路径是否有效
        if (string.IsNullOrEmpty(item.Path)) return false;

        // 检查文件是否存在
        if (!File.Exists(item.Path)) return false;

        // 如有需要，在此处添加其他检查
        return true;
    }
}