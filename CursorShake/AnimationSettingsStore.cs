using System;
using System.IO;
using System.Text.Json;

namespace CursorShake;

public sealed class AnimationSettings
{
    public double PeakScale { get; set; } = 3.19;
    public int ScaleUpMs { get; set; } = 247;
    public int HoldAtPeakMs { get; set; } = 108;
    public int ScaleDownMs { get; set; } = 225;
    public int EndPadMs { get; set; } = 93;
}

public static class AnimationSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public static AnimationSettings Current { get; set; } = new();

    public static string FilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CursorShake", "settings.json");

    public static void Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return;
            var json = File.ReadAllText(FilePath);
            var loaded = JsonSerializer.Deserialize<AnimationSettings>(json, JsonOptions);
            if (loaded is not null) Current = loaded;
        }
        catch
        {
            // keep defaults
        }
    }

    public static void Save()
    {
        var dir = Path.GetDirectoryName(FilePath);
        if (dir is not null) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(Current, JsonOptions);
        File.WriteAllText(FilePath, json);
    }
}
