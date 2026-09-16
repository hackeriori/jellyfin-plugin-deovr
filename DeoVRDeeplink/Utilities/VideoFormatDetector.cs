using System.Text.RegularExpressions;
using DeoVRDeeplink.Configuration;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace DeoVRDeeplink.Utilities;

/// <summary>
/// Represents the detected DeoVR playback format.
/// </summary>
/// <param name="StereoMode">The stereo mode ('sbs', 'tb', 'cuv', or 'off').</param>
/// <param name="ScreenType">The screen/projection type ('flat', 'dome', 'sphere', 'fisheye', 'mkx200').</param>
/// <param name="Is3D">Whether stereoscopic 3D rendering should be enabled in DeoVR.</param>
public readonly record struct VideoFormatInfo(string StereoMode, string ScreenType, bool Is3D);

/// <summary>
/// Detects video format (stereo mode, projection screen type, and 3D flag) for DeoVR playback.
/// Prioritizes explicit metadata configuration, then filename conventions, Jellyfin 3D format, and library fallbacks.
/// </summary>
public static class VideoFormatDetector
{
    // Projection / screen type patterns
    // 200-degree fisheye (MKX 200)
    private static readonly Regex RegexMkx200 = new(
        @"(?<![a-zA-Z0-9])(?:mkx200|fisheye200)(?![a-zA-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 190-degree fisheye (Canon RF 5.2mm)
    // 注意：VR头显实测表明，DeoVR客户端对佳能RF52镜头视频若使用 "rf52" 投影类型会导致画面畸变/异常，必须使用 "fisheye" 投影网格渲染
    private static readonly Regex RegexRf52 = new(
        @"(?<![a-zA-Z0-9])(?:rf52|fisheye190)(?![a-zA-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 180-degree / 220-degree fisheye
    private static readonly Regex RegexFisheye = new(
        @"(?<![a-zA-Z0-9])(?:fisheye180|fisheye|vrca220)(?![a-zA-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 180-degree equirectangular (VR180 / Dome)
    // Note: Lookaround ensures we do not match resolutions like 1080p / 2160p or 180p, while supporting _180, Video180, LR_180, etc.
    private static readonly Regex Regex180 = new(
        @"(?<![a-zA-Z0-9])(?:vr180|180vr|lr[_\-\.\s]?180|180[_\-\.\s]?lr)(?![a-zA-Z0-9])|(?<!\d)180(?![a-zA-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 360-degree equirectangular (VR360 / Sphere)
    // Note: Lookaround ensures we do not match resolutions like 360p or numbers like 1360, while supporting _360, Space360, etc.
    private static readonly Regex Regex360 = new(
        @"(?<![a-zA-Z0-9])(?:vr360|360vr)(?![a-zA-Z0-9])|(?<!\d)360(?![a-zA-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Explicit flat screen indicator
    private static readonly Regex RegexFlat = new(
        @"(?<![a-zA-Z0-9])(?:flat|flat3d|cinema)(?![a-zA-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Stereo layout patterns
    // Side-by-Side (SBS)
    private static readonly Regex RegexSbs = new(
        @"(?<![a-zA-Z0-9])(?:sbs|hsbs|fsbs|lr|3dh|sidebyside|side-by-side|half-sbs|full-sbs|lr[_\-\.\s]?180|180[_\-\.\s]?lr)(?![a-zA-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Top-and-Bottom (TB / OverUnder)
    private static readonly Regex RegexTb = new(
        @"(?<![a-zA-Z0-9])(?:tb|htab|ftab|ou|3dv|overunder|over-under|topbottom|top-bottom|half-ou|full-ou|half-tab|full-tab)(?![a-zA-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Custom UV layout (Canon RF 5.2mm dual fisheye raw feed)
    private static readonly Regex RegexCuv = new(
        @"(?<![a-zA-Z0-9])cuv(?![a-zA-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Monoscopic / 2D
    private static readonly Regex RegexMono = new(
        @"(?<![a-zA-Z0-9])(?:2d|mono|monoscopic)(?![a-zA-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // General 3D indicator tag (e.g., '.3D.' in Jellyfin file naming convention)
    private static readonly Regex RegexGeneral3D = new(
        @"(?<![a-zA-Z0-9])3d(?![a-zA-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Detects the DeoVR format for a video item.
    /// Priority order:
    /// 1. Item-level tags (metadata in Jellyfin web UI or .nfo)
    /// 2. Library-level forced settings (configured in plugin settings)
    /// 3. Filename detection (DeoVR local naming &amp; Jellyfin 3D conventions)
    /// 4. Jellyfin scanned Video3DFormat (defaults to flat cinema 3D)
    /// 5. Library-level fallback settings
    /// 6. Global default (flat 2D, mono off)
    /// </summary>
    /// <param name="video">The Jellyfin video item.</param>
    /// <param name="libConfig">The library configuration, if any.</param>
    /// <returns>A <see cref="VideoFormatInfo"/> containing stereoMode, screenType, and is3D flag.</returns>
    public static VideoFormatInfo Detect(Video video, LibraryConfiguration? libConfig)
    {
        string? stereoMode = null;
        string? screenType = null;

        // Level 1: Item-level explicit tags (Highest Priority - configuration override)
        DetectFromTags(video, ref stereoMode, ref screenType);

        // Level 2: Library-level forced settings (if configured)
        if (libConfig != null)
        {
            if (screenType == null && libConfig.ForcedProjection != ProjectionType.None)
            {
                screenType = FormatMappingHelper.ToScreenType(libConfig.ForcedProjection);
            }

            if (stereoMode == null && libConfig.ForcedStereoMode != StereoMode.None)
            {
                stereoMode = FormatMappingHelper.ToStereoMode(libConfig.ForcedStereoMode);
            }
        }

        // Level 3: Filename parsing (Primary automated convention)
        var rawPath = video.Path ?? video.FileNameWithoutExtension ?? video.Name ?? string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(rawPath.Replace('\\', '/'));
        DetectFromFileName(fileName, ref stereoMode, ref screenType);

        // Level 4: Jellyfin scanned Video3DFormat
        if (video.Video3DFormat.HasValue)
        {
            if (stereoMode == null)
            {
                stereoMode = video.Video3DFormat.Value switch
                {
                    Video3DFormat.FullSideBySide or Video3DFormat.HalfSideBySide => "sbs",
                    Video3DFormat.FullTopAndBottom or Video3DFormat.HalfTopAndBottom => "tb",
                    _ => null
                };
            }

            // Traditional 3D movies (HSBS/FSBS Blu-rays) without VR tags are flat cinema screens
            screenType ??= "flat";
        }

        // Level 5: Library fallback settings
        if (libConfig != null)
        {
            if (screenType == null && libConfig.FallbackProjection != ProjectionType.None)
            {
                screenType = FormatMappingHelper.ToScreenType(libConfig.FallbackProjection);
            }

            if (stereoMode == null && libConfig.FallbackStereoMode != StereoMode.None)
            {
                stereoMode = FormatMappingHelper.ToStereoMode(libConfig.FallbackStereoMode);
            }
        }

        // Level 6: Global defaults (Standard 2D flat video)
        stereoMode ??= "off";
        screenType ??= "flat";

        // is3d should be true only when stereoscopic rendering is active
        var is3d = stereoMode is "sbs" or "tb" or "cuv";

        return new VideoFormatInfo(stereoMode, screenType, is3d);
    }

    /// <summary>
    /// Checks Jellyfin item tags for format overrides.
    /// Supports tags like 'VR180', 'Fisheye', 'SBS', 'Flat', or namespaced tags like 'deovr:dome', 'deovr:sbs'.
    /// </summary>
    private static void DetectFromTags(Video video, ref string? stereoMode, ref string? screenType)
    {
        if (video.Tags == null || video.Tags.Length == 0)
        {
            return;
        }

        foreach (var rawTag in video.Tags)
        {
            var tag = rawTag.Trim();
            if (tag.StartsWith("deovr:", StringComparison.OrdinalIgnoreCase))
            {
                tag = tag[6..].Trim();
            }

            // Screen / Projection tags
            if (screenType == null)
            {
                screenType = tag.ToLowerInvariant() switch
                {
                    "dome" or "180" or "vr180" or "180vr" or "lr_180" or "180_lr" or "lr-180" or "180-lr" or "lr180" or "180lr" => "dome",
                    "sphere" or "360" or "vr360" or "360vr" => "sphere",
                    // 注意：VR头显实测表明，DeoVR对佳能RF52镜头视频若指定 "rf52" 投影类型会导致画面畸变，应统一使用 "fisheye" 投影网格
                    "fisheye" or "fisheye180" or "vrca220" or "rf52" or "fisheye190" => "fisheye",
                    "mkx200" or "fisheye200" => "mkx200",
                    "flat" or "flat3d" or "cinema" => "flat",
                    _ => null
                };
            }

            // Stereo mode tags
            if (stereoMode == null)
            {
                stereoMode = tag.ToLowerInvariant() switch
                {
                    "sbs" or "hsbs" or "fsbs" or "lr" or "3dh" or "sidebyside" or "side-by-side" or "half-sbs" or "full-sbs" or "lr_180" or "180_lr" or "lr-180" or "180-lr" or "lr180" or "180lr" => "sbs",
                    "tb" or "htab" or "ftab" or "ou" or "3dv" or "overunder" or "over-under" or "topbottom" or "top-bottom" or "half-ou" or "full-ou" or "half-tab" or "full-tab" => "tb",
                    "cuv" => "cuv",
                    "off" or "mono" or "monoscopic" or "2d" => "off",
                    _ => null
                };
            }
        }
    }

    /// <summary>
    /// Parses the filename for DeoVR projection and stereo tags.
    /// Complies with DeoVR documentation section 10 and Jellyfin 3D naming conventions.
    /// </summary>
    private static void DetectFromFileName(string fileName, ref string? stereoMode, ref string? screenType)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        // Screen type detection from filename
        if (screenType == null)
        {
            if (RegexMkx200.IsMatch(fileName))
            {
                screenType = "mkx200";
            }
            else if (RegexRf52.IsMatch(fileName))
            {
                // 注意：VR头显实测表明，DeoVR客户端对佳能RF5.2mm（RF52/Fisheye190）视频使用 "rf52" 投影网格会出现画面畸变/异常，
                // 必须使用 "fisheye" 投影类型才能在 DeoVR 中正确渲染播放。
                screenType = "fisheye";
            }
            else if (RegexFisheye.IsMatch(fileName))
            {
                screenType = "fisheye";
            }
            else if (Regex180.IsMatch(fileName))
            {
                screenType = "dome";
            }
            else if (Regex360.IsMatch(fileName))
            {
                screenType = "sphere";
            }
            else if (RegexFlat.IsMatch(fileName))
            {
                screenType = "flat";
            }
        }

        // Stereo mode detection from filename
        if (stereoMode == null)
        {
            if (RegexSbs.IsMatch(fileName))
            {
                stereoMode = "sbs";
            }
            else if (RegexTb.IsMatch(fileName))
            {
                stereoMode = "tb";
            }
            else if (RegexCuv.IsMatch(fileName))
            {
                stereoMode = "cuv";
            }
            else if (RegexMono.IsMatch(fileName))
            {
                stereoMode = "off";
            }
        }

        // If filename specifies 3D layout (e.g. '.3D.hsbs') but no VR projection tag,
        // it is a traditional flat 3D cinema screen.
        if (screenType == null && (RegexGeneral3D.IsMatch(fileName) || stereoMode is "sbs" or "tb"))
        {
            // If the filename contains 3D format tags without any VR projection mesh,
            // default to flat projection to avoid warping cinema 3D movies into a dome/sphere.
            if (RegexGeneral3D.IsMatch(fileName))
            {
                screenType = "flat";
            }
        }
    }
}

/// <summary>
/// Helper for converting configuration enums to DeoVR protocol string values.
/// </summary>
public static class FormatMappingHelper
{
    /// <summary>
    /// Converts a <see cref="ProjectionType"/> to DeoVR screenType.
    /// </summary>
    public static string? ToScreenType(ProjectionType projection) => projection switch
    {
        ProjectionType.Projection180 => "dome",
        ProjectionType.Projection360 => "sphere",
        ProjectionType.Flat => "flat",
        ProjectionType.Fisheye => "fisheye",
        // 注意：VR头显实测表明，DeoVR对佳能RF52镜头若映射为 "rf52" 会出现投影畸变，应映射为 "fisheye"
        ProjectionType.Fisheye190 => "fisheye",
        ProjectionType.Fisheye200 => "mkx200",
        _ => null
    };

    /// <summary>
    /// Converts a <see cref="StereoMode"/> to DeoVR stereoMode.
    /// </summary>
    public static string? ToStereoMode(StereoMode stereo) => stereo switch
    {
        StereoMode.SideBySide => "sbs",
        StereoMode.TopBottom => "tb",
        StereoMode.Off => "off",
        StereoMode.CustomUV => "cuv",
        _ => null
    };
}
