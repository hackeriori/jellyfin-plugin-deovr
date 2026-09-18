using MediaBrowser.Model.Plugins;

namespace JellyfinPluginDeoVR.Configuration;

/// <summary>VR 内容的投影类型。</summary>
public enum ProjectionType
{
    /// <summary>不强制指定投影（自动检测）。</summary>
    None = 0,
    /// <summary>180度等距柱状投影（Equirectangular）。</summary>
    Projection180 = 1,
    /// <summary>360度等距柱状投影（Equirectangular）。</summary>
    Projection360 = 2,
    /// <summary>平面 2D 或影院 3D 投影。</summary>
    Flat = 3,
    /// <summary>180度鱼眼投影（Fisheye）。</summary>
    Fisheye = 4,
    /// <summary>190度鱼眼投影（佳能 RF 5.2mm，VR头显实测映射至DeoVR的fisheye网格以防畸变）。</summary>
    Fisheye190 = 5,
    /// <summary>200度鱼眼投影（MKX 200）。</summary>
    Fisheye200 = 6
}

/// <summary>VR 内容的立体模式。</summary>
public enum StereoMode
{
    /// <summary>不强制指定立体模式（自动检测）。</summary>
    None = 0,
    /// <summary>左右立体格式（Side-by-Side / SBS）。</summary>
    SideBySide = 1,
    /// <summary>上下立体格式（Top-Bottom / TB）。</summary>
    TopBottom = 2,
    /// <summary>单目 2D 模式（关闭立体）。</summary>
    Off = 3,
    /// <summary>自定义 UV 布局（原始佳能 RF 5.2mm 格式）。</summary>
    CustomUV = 4
}

/// <summary>媒体库排序方式。</summary>
public enum SortBy
{
    /// <summary>按名称排序。</summary>
    Name = 0,
    /// <summary>随机排序。</summary>
    Random = 1,
    /// <summary>按添加日期排序。</summary>
    DateAdded = 2,
    /// <summary>按上映日期排序。</summary>
    ReleaseDate = 3
}

/// <summary>单个媒体库的配置设置。</summary>
public class LibraryConfiguration
{
    /// <summary>获取或设置媒体库标识符。</summary>
    public Guid Id { get; set; }
    
    /// <summary>获取或设置一个值，指示该媒体库是否启用。</summary>
    public bool Enabled { get; set; }
    
    /// <summary>获取或设置排序条件。</summary>
    public SortBy SortBy { get; set; }
    
    /// <summary>获取或设置排序顺序。</summary>
    public Jellyfin.Database.Implementations.Enums.SortOrder SortOrder { get; set; }
    
    /// <summary>获取或设置一个值，指示是否启用时间轴缩略图。</summary>
    public bool TimelineImages { get; set; }
    
    /// <summary>获取或设置强制投影类型（覆盖文件名检测）。</summary>
    public ProjectionType ForcedProjection { get; set; }
    
    /// <summary>获取或设置强制立体模式（覆盖文件名检测）。</summary>
    public StereoMode ForcedStereoMode { get; set; }

    /// <summary>获取或设置后备投影类型。</summary>
    public ProjectionType FallbackProjection { get; set; }
    
    /// <summary>获取或设置后备立体模式。</summary>
    public StereoMode FallbackStereoMode { get; set; }
}

/// <summary>插件配置。</summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>初始化 <see cref="PluginConfiguration"/> 类的新实例。</summary>
    public PluginConfiguration()
    {
        AllowedIpRanges = [];
        EnableIpRestriction = false;
        Libraries = new List<LibraryConfiguration>();
    }
    
    /// <summary>获取或设置 CIDR 格式的允许 IP 地址范围列表（例如 "192.168.1.0/24", "10.0.0.0/8", "127.0.0.1/32"）。</summary>
    public List<string> AllowedIpRanges { get; set; }
    
    /// <summary>获取或设置一个值，指示是否启用 IP 限制。</summary>
    public bool EnableIpRestriction { get; set; }
    
    /// <summary>获取或设置各媒体库的配置。</summary>
    public List<LibraryConfiguration> Libraries { get; set; }
}