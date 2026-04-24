using System.Drawing;

namespace CursorShake;

/// <summary>
/// Last successful region capture in **screen pixels**, shared across sessions and overlay instances.
/// </summary>
public static class ScreenshotRegionMemory
{
    private static Rectangle? _lastRegion;

    public static event EventHandler? LastRegionChanged;

    public static Rectangle? LastRegion
    {
        get => _lastRegion;
        set
        {
            _lastRegion = value;
            LastRegionChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static bool HasRegion =>
        LastRegion is { Width: >= 2, Height: >= 2 };
}
