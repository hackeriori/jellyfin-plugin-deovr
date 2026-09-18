using System.Text.Json.Serialization;

namespace JellyfinPluginDeoVR.Model;

/// <summary>
/// 表示包含 DeoVR 视频详细信息的响应。
/// </summary>
public class DeoVrVideoResponse
{
    /// <summary>
    /// 获取或设置视频的可用编码列表。
    /// </summary>
    [JsonPropertyName("encodings")]
    public List<DeoVrEncoding> Encodings { get; set; } = [];

    /// <summary>
    /// 获取或设置视频标题。
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置视频标识符。
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// 获取或设置视频时长（以秒为单位）。
    /// </summary>
    [JsonPropertyName("videoLength")]
    public int VideoLength { get; set; }

    /// <summary>
    /// 获取或设置一个值，指示该视频是否为 3D。
    /// </summary>
    [JsonPropertyName("is3d")]
    public bool Is3D { get; set; }

    /// <summary>
    /// 获取或设置视频的屏幕类型（例如 flat、dome、sphere、fisheye 等）。
    /// </summary>
    [JsonPropertyName("screenType")]
    public string ScreenType { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置视频的立体模式（例如 "off"、"sbs"、"tb" 等）。
    /// </summary>
    [JsonPropertyName("stereoMode")]
    public string StereoMode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置视频封面缩略图的 URL。
    /// </summary>
    [JsonPropertyName("thumbnailUrl")]
    public string ThumbnailUrl { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置时间轴预览拼图的 URL（类似于 Jellyfin Trickplay）。
    /// </summary>
    [JsonPropertyName("timelinePreview")]
    public string TimelinePreview { get; set; } = string.Empty;
    
     /// <summary>
    /// 获取或设置视频的时间戳章节标记列表。
    /// </summary>
    [JsonPropertyName("timeStamps")]
    public List<DeoVrTimestamps> Timestamps { get; set; } = [];
}

/// <summary>
/// 表示 DeoVR 视频的特定编码。
/// </summary>
public class DeoVrEncoding
{
    /// <summary>
    /// 获取或设置编码名称（例如 "1080p", "4K"）。
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置此编码下的视频源列表。
    /// </summary>
    [JsonPropertyName("videoSources")]
    public List<DeoVrVideoSource> VideoSources { get; set; } = [];
}

/// <summary>
/// 表示特定编码的视频源。
/// </summary>
public class DeoVrVideoSource
{
    /// <summary>
    /// 获取或设置视频源的分辨率（例如 2160 代表 2160p）。
    /// </summary>
    [JsonPropertyName("resolution")]
    public int Resolution { get; set; }

    /// <summary>
    /// 获取或设置视频源的 URL。
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// 表示 DeoVR 视频中带名称的时间戳章节标记。
/// </summary>
public class DeoVrTimestamps
{
    /// <summary>
    /// 获取或设置时间戳（以秒为单位）。
    /// </summary>
    [JsonPropertyName("ts")]
    public int ts { get; set; }

    /// <summary>
    /// 获取或设置时间戳的名称或描述。
    /// </summary>
    [JsonPropertyName("name")]
    public string? name { get; set; }
}
