using System.Text.RegularExpressions;
using DeoVRDeeplink.Configuration;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace DeoVRDeeplink.Utilities;

/// <summary>
/// 表示检测到的 DeoVR 播放格式。
/// </summary>
/// <param name="StereoMode">立体模式（'sbs', 'tb', 'cuv' 或 'off'）。</param>
/// <param name="ScreenType">屏幕/投影类型（'flat', 'dome', 'sphere', 'fisheye', 'mkx200'）。</param>
/// <param name="Is3D">DeoVR 中是否应启用立体 3D 渲染。</param>
public readonly record struct VideoFormatInfo(string StereoMode, string ScreenType, bool Is3D);

/// <summary>
/// 检测用于 DeoVR 播放的视频格式（立体模式、投影屏幕类型和 3D 标识）。
/// 优先级：显式元数据配置 > 文件名命名规范 > Jellyfin 3D 格式 > 媒体库后备设置。
/// </summary>
public static class VideoFormatDetector
{
    // 投影 / 屏幕类型匹配模式
    // 200度鱼眼（MKX 200）
    private static readonly Regex RegexMkx200 = new(
        @"(?<![a-zA-Z0-9])(?:mkx200|fisheye200)(?![a-zA-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 190度鱼眼（佳能 RF 5.2mm）
    // 注意：VR头显实测表明，DeoVR客户端对佳能RF52镜头视频若使用 "rf52" 投影类型会导致画面畸变/异常，必须使用 "fisheye" 投影网格渲染
    private static readonly Regex RegexRf52 = new(
        @"(?<![a-zA-Z0-9])(?:rf52|fisheye190)(?![a-zA-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 180度 / 220度鱼眼
    private static readonly Regex RegexFisheye = new(
        @"(?<![a-zA-Z0-9])(?:fisheye180|fisheye|vrca220)(?![a-zA-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 180度等距柱状投影（VR180 / 半球 Dome）
    // 注意：环视断言确保不会误匹配 1080p / 2160p 或 180p 等分辨率，同时支持 _180、Video180、LR_180 等命名。
    private static readonly Regex Regex180 = new(
        @"(?<![a-zA-Z0-9])(?:vr180|180vr|lr[_\-\.\s]?180|180[_\-\.\s]?lr)(?![a-zA-Z0-9])|(?<!\d)180(?![a-zA-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 360度等距柱状投影（VR360 / 全球 Sphere）
    // 注意：环视断言确保不会误匹配 360p 等分辨率或 1360 等数字，同时支持 _360、Space360 等命名。
    private static readonly Regex Regex360 = new(
        @"(?<![a-zA-Z0-9])(?:vr360|360vr)(?![a-zA-Z0-9])|(?<!\d)360(?![a-zA-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 显式平面屏幕标识
    private static readonly Regex RegexFlat = new(
        @"(?<![a-zA-Z0-9])(?:flat|flat3d|cinema)(?![a-zA-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 立体布局匹配模式
    // 左右立体（Side-by-Side / SBS）
    private static readonly Regex RegexSbs = new(
        @"(?<![a-zA-Z0-9])(?:sbs|hsbs|fsbs|lr|3dh|sidebyside|side-by-side|half-sbs|full-sbs|lr[_\-\.\s]?180|180[_\-\.\s]?lr)(?![a-zA-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 上下立体（Top-and-Bottom / TB / OverUnder）
    private static readonly Regex RegexTb = new(
        @"(?<![a-zA-Z0-9])(?:tb|htab|ftab|ou|3dv|overunder|over-under|topbottom|top-bottom|half-ou|full-ou|half-tab|full-tab)(?![a-zA-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 自定义 UV 布局（佳能 RF 5.2mm 双鱼眼原始画面）
    private static readonly Regex RegexCuv = new(
        @"(?<![a-zA-Z0-9])cuv(?![a-zA-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 单目 / 2D
    private static readonly Regex RegexMono = new(
        @"(?<![a-zA-Z0-9])(?:2d|mono|monoscopic)(?![a-zA-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 通用 3D 标识标签（例如 Jellyfin 文件命名规范中的 '.3D.'）
    private static readonly Regex RegexGeneral3D = new(
        @"(?<![a-zA-Z0-9])3d(?![a-zA-Z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// 检测视频项的 DeoVR 格式。
    /// 优先级顺序：
    /// 1. 项目级标签（Jellyfin Web 界面或 .nfo 元数据）
    /// 2. 媒体库级强制设置（在插件设置中配置）
    /// 3. 文件名检测（DeoVR 本地命名与 Jellyfin 3D 规范）
    /// 4. Jellyfin 扫描的 Video3DFormat（默认为平面影院 3D）
    /// 5. 媒体库级后备设置
    /// 6. 全局默认值（平面 2D，关闭立体）
    /// </summary>
    /// <param name="video">Jellyfin 视频项。</param>
    /// <param name="libConfig">媒体库配置（如果有）。</param>
    /// <returns>包含 stereoMode、screenType 和 is3D 标识的 <see cref="VideoFormatInfo"/>。</returns>
    public static VideoFormatInfo Detect(Video video, LibraryConfiguration? libConfig)
    {
        string? stereoMode = null;
        string? screenType = null;

        // 优先级 1：项目级显式标签（最高优先级 - 配置覆盖）
        DetectFromTags(video, ref stereoMode, ref screenType);

        // 优先级 2：媒体库级强制设置（若已配置）
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

        // 优先级 3：文件名解析（主要自动化规范）
        var rawPath = video.Path ?? video.FileNameWithoutExtension ?? video.Name ?? string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(rawPath.Replace('\\', '/'));
        DetectFromFileName(fileName, ref stereoMode, ref screenType);

        // 优先级 4：Jellyfin 扫描到的 Video3DFormat
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

            // 无 VR 标签的传统 3D 电影（左右半宽/全宽蓝光）属于平面影院屏幕
            screenType ??= "flat";
        }

        // 优先级 5：媒体库后备设置
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

        // 优先级 6：全局默认值（标准 2D 平面视频）
        stereoMode ??= "off";
        screenType ??= "flat";

        // 仅在立体渲染处于激活状态时 is3d 才为 true
        var is3d = stereoMode is "sbs" or "tb" or "cuv";

        return new VideoFormatInfo(stereoMode, screenType, is3d);
    }

    /// <summary>
    /// 检查 Jellyfin 项目标签中的格式覆盖项。
    /// 支持如 'VR180'、'Fisheye'、'SBS'、'Flat' 等标签，或带命名空间的标签如 'deovr:dome'、'deovr:sbs'。
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

            // 屏幕 / 投影标签
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

            // 立体模式标签
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
    /// 从文件名中解析 DeoVR 投影和立体模式标签。
    /// 符合 DeoVR 官方文档第 10 节规范及 Jellyfin 3D 文件命名约定。
    /// </summary>
    private static void DetectFromFileName(string fileName, ref string? stereoMode, ref string? screenType)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        // 从文件名检测屏幕类型
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

        // 从文件名检测立体模式
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

        // 若文件名指定了 3D 布局（例如 '.3D.hsbs'）但未指定 VR 投影标签，
        // 则视为传统的平面 3D 影院屏幕。
        if (screenType == null && (RegexGeneral3D.IsMatch(fileName) || stereoMode is "sbs" or "tb"))
        {
            // 若文件名包含 3D 格式标签但没有任何 VR 投影网格，
            // 则默认为平面投影，避免将影院 3D 电影扭曲为半球/全球投影。
            if (RegexGeneral3D.IsMatch(fileName))
            {
                screenType = "flat";
            }
        }
    }
}

/// <summary>
/// 用于将配置枚举转换为 DeoVR 协议字符串值的辅助类。
/// </summary>
public static class FormatMappingHelper
{
    /// <summary>
    /// 将 <see cref="ProjectionType"/> 转换为 DeoVR 的 screenType。
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
    /// 将 <see cref="StereoMode"/> 转换为 DeoVR 的 stereoMode。
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
