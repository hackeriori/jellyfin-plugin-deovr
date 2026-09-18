using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace JellyfinPluginDeoVR.Utilities;

/// <summary>
/// 提供用于选择和格式化项目图片的辅助方法。
/// </summary>
public static class ImageHelper
{
    /// <summary>
    /// 返回项目最佳可用图片的 URL。
    /// 
    /// 优先级：
    /// 1. Backdrop 背景图（VR 首选 – 通常为 16:9 比例）
    /// 2. Primary 主图/封面（后备选项）
    /// 
    /// 大尺寸图片将缩放为一致的 16:9 规格。
    /// </summary>
    public static string GetImageUrl(BaseItem item, string baseUrl)
    {
        const int maxSize = 1024;
        var image = new[] { ImageType.Backdrop, ImageType.Primary }
            .Select(type => new
            {
                Type = type,
                Info = Array.Find(item.ImageInfos, i => i.Type == type && IsValid(i))
            })
            .FirstOrDefault(x => x.Info != null);

        if (image?.Info == null)
            return string.Empty;

        var img = image.Info;

        // 小尺寸图片：原样返回
        if (img.Width is > 0 and <= maxSize && img.Height <= maxSize)
            return $"{baseUrl}/Items/{item.Id}/Images/{image.Type}";

        // 大尺寸图片：强制按 16:9 缩放以保持 VR 界面布局一致
        return $"{baseUrl}/Items/{item.Id}/Images/{image.Type}?fillWidth=960&fillHeight=540&quality=90";
    }

    /// <summary>
    /// 检查图片是否可用的基础合法性校验。
    /// </summary>
    private static bool IsValid(ItemImageInfo img) =>
        !string.IsNullOrEmpty(img.Path) &&
        (!img.IsLocalFile || (img.Width > 0 && img.Height > 0));
}