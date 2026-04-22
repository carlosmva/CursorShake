using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace CursorShake;

public partial class SettingsWindow
{
    public SettingsWindow()
    {
        InitializeComponent();
        Title = "Cursor Shake · Tuning";
        var iconPath = Path.Combine(AppContext.BaseDirectory, "cursor.ico");
        if (File.Exists(iconPath)) Icon = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
        Loaded += OnLoaded;
        SliderPeak.ValueChanged += (_, _) => ValuePeak.Text = SliderPeak.Value.ToString("0.00");
        SliderGrow.ValueChanged += (_, _) => ValueGrow.Text = ((int)SliderGrow.Value).ToString();
        SliderHold.ValueChanged += (_, _) => ValueHold.Text = ((int)SliderHold.Value).ToString();
        SliderShrink.ValueChanged += (_, _) => ValueShrink.Text = ((int)SliderShrink.Value).ToString();
        SliderEndPad.ValueChanged += (_, _) => ValueEndPad.Text = ((int)SliderEndPad.Value).ToString();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => ApplyFromStore();

    private void TitleBar_OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove();
    }

    private void Close_OnClick(object sender, RoutedEventArgs e) => Close();

    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        AnimationSettingsStore.Current = new AnimationSettings
        {
            PeakScale = Math.Round(Math.Clamp(SliderPeak.Value, 1.05, 4.0), 2, MidpointRounding.AwayFromZero),
            ScaleUpMs = (int)Math.Clamp(SliderGrow.Value, 20, 800),
            HoldAtPeakMs = (int)Math.Clamp(SliderHold.Value, 0, 800),
            ScaleDownMs = (int)Math.Clamp(SliderShrink.Value, 20, 800),
            EndPadMs = (int)Math.Clamp(SliderEndPad.Value, 0, 500)
        };
        AnimationSettingsStore.Save();
        StatusLine.Text = "[SAVED]";
        StatusLine.Visibility = Visibility.Visible;
    }

    private void ApplyFromStore()
    {
        var s = AnimationSettingsStore.Current;
        SliderPeak.Value = s.PeakScale;
        SliderGrow.Value = s.ScaleUpMs;
        SliderHold.Value = s.HoldAtPeakMs;
        SliderShrink.Value = s.ScaleDownMs;
        SliderEndPad.Value = s.EndPadMs;
        ValuePeak.Text = s.PeakScale.ToString("0.00");
        ValueGrow.Text = s.ScaleUpMs.ToString();
        ValueHold.Text = s.HoldAtPeakMs.ToString();
        ValueShrink.Text = s.ScaleDownMs.ToString();
        ValueEndPad.Text = s.EndPadMs.ToString();
        StatusLine.Visibility = Visibility.Collapsed;
    }
}
