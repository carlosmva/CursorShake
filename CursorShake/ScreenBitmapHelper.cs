using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace CursorShake;

internal static class ScreenBitmapHelper
{
    [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
    private static extern bool DeleteObject(IntPtr hObject);

    public static BitmapSource CaptureRectanglePixels(Rectangle rect)
    {
        using var bmp = new Bitmap(rect.Width, rect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(rect.Left, rect.Top, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);
        }

        var h = bmp.GetHbitmap();
        try
        {
            var src = Imaging.CreateBitmapSourceFromHBitmap(
                h,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            src.Freeze();
            return src;
        }
        finally
        {
            DeleteObject(h);
        }
    }

    public static BitmapSource CaptureMonitor(System.Windows.Forms.Screen screen)
    {
        var b = screen.Bounds;
        return CaptureRectanglePixels(new Rectangle(b.Left, b.Top, b.Width, b.Height));
    }

    /// <summary>Applies optional border/shadow from settings for clipboard export.</summary>
    public static BitmapSource PrepareForClipboard(BitmapSource captured)
    {
        var s = AnimationSettingsStore.Current;
        if (!s.ScreenshotBorder && !s.ScreenshotShadow)
            return captured;

        return DecorateForClipboard(captured, s.ScreenshotBorder, s.ScreenshotShadow);
    }

    /// <summary>
    /// Original clipboard decoration: WPF <see cref="Border"/> + optional <see cref="DropShadowEffect"/>
    /// on a <b>white</b> outer mat. Transparent mats and manual <see cref="DrawingVisual"/> paths regressed
    /// in Office paste (heavy edges / lost shadow).
    /// </summary>
    private static BitmapSource DecorateForClipboard(BitmapSource captured, bool border, bool shadow)
    {
        var image = new System.Windows.Controls.Image
        {
            Source = captured,
            Stretch = Stretch.None,
            SnapsToDevicePixels = true
        };

        var framed = new System.Windows.Controls.Border
        {
            Child = image,
            Background = System.Windows.Media.Brushes.Transparent,
            SnapsToDevicePixels = true
        };

        if (border)
        {
            framed.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xCC, 0xCC, 0xCC));
            framed.BorderThickness = new System.Windows.Thickness(1);
        }

        if (shadow)
        {
            framed.Effect = new DropShadowEffect
            {
                Color = System.Windows.Media.Colors.Black,
                BlurRadius = 18,
                ShadowDepth = 3,
                Direction = 315,
                Opacity = 0.42
            };
        }

        var outerPad = shadow ? 20.0 : border ? 2.0 : 0.0;
        var root = new System.Windows.Controls.Border
        {
            Child = framed,
            Padding = new System.Windows.Thickness(outerPad),
            Background = System.Windows.Media.Brushes.White,
            SnapsToDevicePixels = true
        };

        root.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
        root.Arrange(new System.Windows.Rect(0, 0, root.DesiredSize.Width, root.DesiredSize.Height));
        root.UpdateLayout();

        var wPx = Math.Max(1, (int)Math.Ceiling(root.ActualWidth));
        var hPx = Math.Max(1, (int)Math.Ceiling(root.ActualHeight));

        var rtb = new RenderTargetBitmap(wPx, hPx, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(root);
        rtb.Freeze();
        return rtb;
    }
}
