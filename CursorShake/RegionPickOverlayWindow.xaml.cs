using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using SdRectangle = System.Drawing.Rectangle;

namespace CursorShake;

public partial class RegionPickOverlayWindow : Window
{
    private const int MagSourcePx = 36;
    private const double MagHostSize = 140;
    private readonly System.Windows.Forms.Screen _screen;
    private Point? _origin;

    public SdRectangle? SelectedRegion { get; private set; }

    public RegionPickOverlayWindow(System.Windows.Forms.Screen screen)
    {
        _screen = screen;
        InitializeComponent();
        SourceInitialized += (_, _) => ScreenLayoutHelper.ApplyFullScreenBounds(this, _screen);
        Loaded += (_, _) =>
        {
            ScreenLayoutHelper.ApplyFullScreenBounds(this, _screen);
            Keyboard.Focus(this);
        };
    }

    private void Window_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;
        DialogResult = false;
        e.Handled = true;
    }

    private void Window_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _origin = e.GetPosition(this);
        CaptureMouse();
        RubberBand.Visibility = Visibility.Visible;
        Canvas.SetLeft(RubberBand, _origin.Value.X);
        Canvas.SetTop(RubberBand, _origin.Value.Y);
        RubberBand.Width = 0;
        RubberBand.Height = 0;
    }

    private void Window_OnMouseMove(object sender, MouseEventArgs e)
    {
        var p = e.GetPosition(this);
        UpdateMagnifier(p);
        PlaceMagnifierNear(p);

        if (_origin is not { } o || e.LeftButton != MouseButtonState.Pressed)
            return;

        Canvas.SetLeft(RubberBand, Math.Min(o.X, p.X));
        Canvas.SetTop(RubberBand, Math.Min(o.Y, p.Y));
        RubberBand.Width = Math.Abs(p.X - o.X);
        RubberBand.Height = Math.Abs(p.Y - o.Y);
    }

    private void Window_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ReleaseMouseCapture();
        RubberBand.Visibility = Visibility.Collapsed;

        if (_origin is not { } o)
        {
            DialogResult = false;
            return;
        }

        var p = e.GetPosition(this);
        _origin = null;

        if (Distance(o, p) < 3)
        {
            DialogResult = false;
            return;
        }

        SelectedRegion = WindowDragToScreenRectPixels(o, p);
        if (SelectedRegion is not { Width: >= 2, Height: >= 2 })
        {
            DialogResult = false;
            return;
        }

        DialogResult = true;
    }

    private void UpdateMagnifier(Point windowPt)
    {
        var screenPt = PointToScreen(windowPt);
        var sx = (int)screenPt.X - MagSourcePx / 2;
        var sy = (int)screenPt.Y - MagSourcePx / 2;
        var cap = new SdRectangle(sx, sy, MagSourcePx, MagSourcePx);
        var vs = System.Windows.Forms.SystemInformation.VirtualScreen;
        var vr = new SdRectangle(vs.X, vs.Y, vs.Width, vs.Height);
        cap = SdRectangle.Intersect(cap, vr);
        if (cap.Width < 4 || cap.Height < 4)
            return;
        try
        {
            MagImage.Source = ScreenBitmapHelper.CaptureRectanglePixels(cap);
        }
        catch
        {
            /* ignore */
        }
    }

    private void PlaceMagnifierNear(Point p)
    {
        var pad = 20.0;
        var mx = p.X + pad;
        var my = p.Y + pad;
        if (mx + MagHostSize > ActualWidth)
            mx = p.X - MagHostSize - pad;
        if (my + MagHostSize > ActualHeight)
            my = p.Y - MagHostSize - pad;
        mx = Math.Clamp(mx, 8, Math.Max(8, ActualWidth - MagHostSize - 8));
        my = Math.Clamp(my, 8, Math.Max(8, ActualHeight - MagHostSize - 8));
        MagnifierTranslate.X = mx;
        MagnifierTranslate.Y = my;
    }

    private SdRectangle? WindowDragToScreenRectPixels(Point a, Point b)
    {
        var tl = PointToScreen(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)));
        var br = PointToScreen(new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
        var ix1 = (int)Math.Floor(Math.Min(tl.X, br.X));
        var iy1 = (int)Math.Floor(Math.Min(tl.Y, br.Y));
        var ix2 = (int)Math.Ceiling(Math.Max(tl.X, br.X));
        var iy2 = (int)Math.Ceiling(Math.Max(tl.Y, br.Y));
        var r = new SdRectangle(ix1, iy1, Math.Max(1, ix2 - ix1), Math.Max(1, iy2 - iy1));
        r = SdRectangle.Intersect(r, _screen.Bounds);
        if (r.Width < 2 || r.Height < 2)
            return null;
        return r;
    }

    private static double Distance(Point a, Point b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
