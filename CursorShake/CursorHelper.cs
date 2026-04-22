using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace CursorShake;

public static class CursorHelper
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int x, y; }

    [StructLayout(LayoutKind.Sequential)]
    public struct CURSORINFO
    {
        public int cbSize;
        public int flags;
        public IntPtr hCursor;
        public POINT ptScreenPos;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ICONINFO
    {
        public bool fIcon;
        public int xHotspot;
        public int yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    [DllImport("user32.dll")]
    public static extern bool GetCursorInfo(out CURSORINFO pci);

    [DllImport("user32.dll")]
    public static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);

    [DllImport("user32.dll")]
    public static extern int ShowCursor(bool bShow);

    public static (int x, int y) GetCursorPosition()
    {
        var ci = new CURSORINFO { cbSize = Marshal.SizeOf<CURSORINFO>() };
        GetCursorInfo(out ci);
        return (ci.ptScreenPos.x, ci.ptScreenPos.y);
    }

    public static (BitmapSource bmp, int hotspotX, int hotspotY, int x, int y) GetCursor()
    {
        var ci = new CURSORINFO { cbSize = Marshal.SizeOf<CURSORINFO>() };
        GetCursorInfo(out ci);

        using var icon = Icon.FromHandle(ci.hCursor);

        var bmp = Imaging.CreateBitmapSourceFromHIcon(
            icon.Handle,
            System.Windows.Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());

        GetIconInfo(ci.hCursor, out var info);

        return (bmp, info.xHotspot, info.yHotspot,
                ci.ptScreenPos.x, ci.ptScreenPos.y);
    }

    public static void HideSystemCursor() => ShowCursor(false);
    public static void ShowSystemCursor() => ShowCursor(true);
}