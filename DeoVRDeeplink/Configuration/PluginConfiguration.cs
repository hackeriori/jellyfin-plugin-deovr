using MediaBrowser.Model.Plugins;

namespace DeoVRDeeplink.Configuration;

/// <summary>Projection type for VR content.</summary>
public enum ProjectionType
{
    /// <summary>No forced projection (auto-detect).</summary>
    None = 0,
    /// <summary>180-degree equirectangular projection.</summary>
    Projection180 = 1,
    /// <summary>360-degree equirectangular projection.</summary>
    Projection360 = 2,
    /// <summary>Flat 2D or cinema 3D projection.</summary>
    Flat = 3,
    /// <summary>180-degree fisheye projection.</summary>
    Fisheye = 4,
    /// <summary>190-degree fisheye projection (Canon RF 5.2mm, VR头显实测映射至DeoVR的fisheye网格以防畸变).</summary>
    Fisheye190 = 5,
    /// <summary>200-degree fisheye projection (MKX 200).</summary>
    Fisheye200 = 6
}

/// <summary>Stereo mode for VR content.</summary>
public enum StereoMode
{
    /// <summary>No forced stereo mode (auto-detect).</summary>
    None = 0,
    /// <summary>Side-by-side stereo format.</summary>
    SideBySide = 1,
    /// <summary>Top-bottom stereo format.</summary>
    TopBottom = 2,
    /// <summary>Monoscopic 2D mode (stereo off).</summary>
    Off = 3,
    /// <summary>Custom UV layout (raw Canon RF 5.2mm format).</summary>
    CustomUV = 4
}

/// <summary>Order in which the library is sorted.</summary>
public enum SortBy
{
    /// <summary>Sort by name.</summary>
    Name = 0,
    /// <summary>Random sort order.</summary>
    Random = 1,
    /// <summary>Sort by date added.</summary>
    DateAdded = 2,
    /// <summary>Sort by release date.</summary>
    ReleaseDate = 3
}

/// <summary>Configuration for individual library settings.</summary>
public class LibraryConfiguration
{
    /// <summary>Gets or sets the library identifier.</summary>
    public Guid Id { get; set; }
    
    /// <summary>Gets or sets a value indicating whether the library is enabled.</summary>
    public bool Enabled { get; set; }
    
    /// <summary>Gets or sets the sort criteria.</summary>
    public SortBy SortBy { get; set; }
    
    /// <summary>Gets or sets the sort order.</summary>
    public Jellyfin.Database.Implementations.Enums.SortOrder SortOrder { get; set; }
    
    /// <summary>Gets or sets a value indicating whether timeline images are enabled.</summary>
    public bool TimelineImages { get; set; }
    
    /// <summary>Gets or sets the forced projection type (overrides filename detection).</summary>
    public ProjectionType ForcedProjection { get; set; }
    
    /// <summary>Gets or sets the forced stereo mode (overrides filename detection).</summary>
    public StereoMode ForcedStereoMode { get; set; }

    /// <summary>Gets or sets the fallback projection type.</summary>
    public ProjectionType FallbackProjection { get; set; }
    
    /// <summary>Gets or sets the fallback stereo mode.</summary>
    public StereoMode FallbackStereoMode { get; set; }
}

/// <summary>Plugin configuration.</summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="PluginConfiguration"/> class.</summary>
    public PluginConfiguration()
    {
        AllowedIpRanges = [];
        EnableIpRestriction = false;
        Libraries = new List<LibraryConfiguration>();
    }
    
    /// <summary>Gets or sets the list of allowed IP ranges in CIDR notation (e.g., "192.168.1.0/24", "10.0.0.0/8", "127.0.0.1/32").</summary>
    public List<string> AllowedIpRanges { get; set; }
    
    /// <summary>Gets or sets a value indicating whether IP restriction is enabled.</summary>
    public bool EnableIpRestriction { get; set; }
    
    /// <summary>Gets or sets the per-library configurations.</summary>
    public List<LibraryConfiguration> Libraries { get; set; }
}