using System.Windows;
using System.Windows.Forms;
namespace CursorShake;

internal static class ScreenLayoutHelper
{
    /// <summary>
    /// Maps a WinForms screen rectangle (device pixels) to WPF DIP coordinates for this window's monitor.
    /// </summary>
    public static void ApplyScreenBounds(Window window, Screen screen, double heightDip)
    {
        void Apply()
        {
            var source = PresentationSource.FromVisual(window);
            if (source?.CompositionTarget is not { } target)
                return;

            var m = target.TransformFromDevice;
            var b = screen.Bounds;
            var upperLeft = m.Transform(new System.Windows.Point(b.Left, b.Top));
            var lowerRight = m.Transform(new System.Windows.Point(b.Right, b.Bottom));
            window.Left = upperLeft.X;
            window.Top = upperLeft.Y;
            window.Width = Math.Max(1, lowerRight.X - upperLeft.X);
            window.Height = heightDip;
        }

        if (PresentationSource.FromVisual(window) != null)
        {
            Apply();
            return;
        }

        window.SourceInitialized += OnSourceInitialized;

        void OnSourceInitialized(object? sender, EventArgs e)
        {
            window.SourceInitialized -= OnSourceInitialized;
            Apply();
        }
    }

    /// <summary>
    /// Positions a horizontal bar at the top center of the screen, capped to <paramref name="preferredBarWidthDip"/>.
    /// </summary>
    public static void ApplyScreenBoundsCentered(Window window, Screen screen, double heightDip, double preferredBarWidthDip)
    {
        void Apply()
        {
            var source = PresentationSource.FromVisual(window);
            if (source?.CompositionTarget is not { } target)
                return;

            var m = target.TransformFromDevice;
            var b = screen.Bounds;
            var upperLeft = m.Transform(new System.Windows.Point(b.Left, b.Top));
            var lowerRight = m.Transform(new System.Windows.Point(b.Right, b.Bottom));
            var screenW = Math.Max(1, lowerRight.X - upperLeft.X);
            var w = Math.Min(preferredBarWidthDip, screenW);
            window.Width = w;
            window.Left = upperLeft.X + (screenW - w) / 2;
            window.Top = upperLeft.Y;
            window.Height = heightDip;
        }

        if (PresentationSource.FromVisual(window) != null)
        {
            Apply();
            return;
        }

        window.SourceInitialized += OnSourceInitialized;

        void OnSourceInitialized(object? sender, EventArgs e)
        {
            window.SourceInitialized -= OnSourceInitialized;
            Apply();
        }
    }

    public static void ApplyFullScreenBounds(Window window, Screen screen)
    {
        void Apply()
        {
            var source = PresentationSource.FromVisual(window);
            if (source?.CompositionTarget is not { } target)
                return;

            var m = target.TransformFromDevice;
            var b = screen.Bounds;
            var upperLeft = m.Transform(new System.Windows.Point(b.Left, b.Top));
            var lowerRight = m.Transform(new System.Windows.Point(b.Right, b.Bottom));
            window.Left = upperLeft.X;
            window.Top = upperLeft.Y;
            window.Width = Math.Max(1, lowerRight.X - upperLeft.X);
            window.Height = Math.Max(1, lowerRight.Y - upperLeft.Y);
        }

        if (PresentationSource.FromVisual(window) != null)
        {
            Apply();
            return;
        }

        window.SourceInitialized += OnSourceInitialized;

        void OnSourceInitialized(object? sender, EventArgs e)
        {
            window.SourceInitialized -= OnSourceInitialized;
            Apply();
        }
    }
}
