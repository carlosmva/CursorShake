using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace CursorShake;

/// <summary>Clipboard screenshot flows shared by the annotation overlay, dock bar, and tray.</summary>
internal static class ScreenshotClipboardActions
{
    public static bool TryCopyMonitor(Screen screen)
    {
        try
        {
            var img = ScreenBitmapHelper.PrepareForClipboard(ScreenBitmapHelper.CaptureMonitor(screen));
            System.Windows.Clipboard.SetImage(img);
            ClipboardToast.Show(screen);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryCopyRectangle(Screen screen, Rectangle r)
    {
        try
        {
            var img = ScreenBitmapHelper.PrepareForClipboard(ScreenBitmapHelper.CaptureRectanglePixels(r));
            System.Windows.Clipboard.SetImage(img);
            ClipboardToast.Show(screen);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Region picker; on OK updates <see cref="ScreenshotRegionMemory"/> and copies.</summary>
    public static bool TryPickRegionAndCopy(Screen screen, Window? owner)
    {
        var pick = new RegionPickOverlayWindow(screen);
        if (owner is not null)
            pick.Owner = owner;
        if (pick.ShowDialog() != true || pick.SelectedRegion is not { } r)
            return false;
        ScreenshotRegionMemory.LastRegion = r;
        return TryCopyRectangle(screen, r);
    }

    public static bool TryCopyLastSavedRegion(Screen screen)
    {
        if (!ScreenshotRegionMemory.HasRegion || ScreenshotRegionMemory.LastRegion is not { } r)
            return false;
        return TryCopyRectangle(screen, r);
    }
}
