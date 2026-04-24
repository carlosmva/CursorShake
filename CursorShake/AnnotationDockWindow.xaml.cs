using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace CursorShake;

public partial class AnnotationDockWindow : Window
{
    private const double CollapsedHeightDip = 6;
    private const double ExpandedHeightDip = 56;
    /// <summary>Toolbar width in DIPs; bar is centered on the monitor.</summary>
    private const double BarWidthDip = 540;
    private readonly DispatcherTimer _collapseTimer;
    private readonly DispatcherTimer _prtMenuCloseTimer = new() { Interval = TimeSpan.FromMilliseconds(220) };

    public Screen TargetScreen { get; }

    public event EventHandler<AnnotationTool>? ToolRequested;

    public AnnotationDockWindow(Screen screen)
    {
        TargetScreen = screen;
        InitializeComponent();

        _prtMenuCloseTimer.Tick += (_, _) =>
        {
            _prtMenuCloseTimer.Stop();
            PrtMenuPopup.IsOpen = false;
        };

        _collapseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(420) };
        _collapseTimer.Tick += (_, _) =>
        {
            _collapseTimer.Stop();
            ToolbarHost.Visibility = Visibility.Collapsed;
            ApplyHeight(CollapsedHeightDip);
        };

        ScreenshotRegionMemory.LastRegionChanged += OnScreenshotRegionMemoryChanged;
        Closed += (_, _) => ScreenshotRegionMemory.LastRegionChanged -= OnScreenshotRegionMemoryChanged;
        Loaded += (_, _) => RefreshPrtMenuPrev();
    }

    private void OnScreenshotRegionMemoryChanged(object? sender, EventArgs e) =>
        Dispatcher.Invoke(RefreshPrtMenuPrev);

    public void ApplyPositionCollapsed()
    {
        ToolbarHost.Visibility = Visibility.Collapsed;
        ApplyHeight(CollapsedHeightDip);
    }

    private void ApplyHeight(double dip) =>
        ScreenLayoutHelper.ApplyScreenBoundsCentered(this, TargetScreen, dip, BarWidthDip);

    private void RootBorder_OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _collapseTimer.Stop();
        ToolbarHost.Visibility = Visibility.Visible;
        ApplyHeight(ExpandedHeightDip);
    }

    private void RootBorder_OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _collapseTimer.Stop();
        _collapseTimer.Start();
    }

    private void Line_OnClick(object sender, RoutedEventArgs e) =>
        ToolRequested?.Invoke(this, AnnotationTool.Line);

    private void Arrow_OnClick(object sender, RoutedEventArgs e) =>
        ToolRequested?.Invoke(this, AnnotationTool.Arrow);

    private void Rect_OnClick(object sender, RoutedEventArgs e) =>
        ToolRequested?.Invoke(this, AnnotationTool.Rectangle);

    private void Text_OnClick(object sender, RoutedEventArgs e) =>
        ToolRequested?.Invoke(this, AnnotationTool.Text);

    private void RefreshPrtMenuPrev() =>
        PrtMenuPrev.IsEnabled = ScreenshotRegionMemory.HasRegion;

    private void BtnPrt_OnClick(object sender, RoutedEventArgs e)
    {
        _prtMenuCloseTimer.Stop();
        PrtMenuPopup.IsOpen = false;
        ScreenshotClipboardActions.TryCopyMonitor(TargetScreen);
    }

    private void BtnPrt_OnMouseEnter(object sender, MouseEventArgs e)
    {
        _prtMenuCloseTimer.Stop();
        RefreshPrtMenuPrev();
        PrtMenuPopup.IsOpen = true;
    }

    private void BtnPrt_OnMouseLeave(object sender, MouseEventArgs e) =>
        SchedulePrtMenuClose();

    private void PrtMenuPanel_OnMouseEnter(object sender, MouseEventArgs e) =>
        _prtMenuCloseTimer.Stop();

    private void PrtMenuPanel_OnMouseLeave(object sender, MouseEventArgs e) =>
        SchedulePrtMenuClose();

    private void SchedulePrtMenuClose()
    {
        _prtMenuCloseTimer.Stop();
        _prtMenuCloseTimer.Start();
    }

    private void PrtMenuRegion_OnClick(object sender, RoutedEventArgs e)
    {
        PrtMenuPopup.IsOpen = false;
        _prtMenuCloseTimer.Stop();
        ScreenshotClipboardActions.TryPickRegionAndCopy(TargetScreen, this);
    }

    private void PrtMenuPrev_OnClick(object sender, RoutedEventArgs e)
    {
        PrtMenuPopup.IsOpen = false;
        _prtMenuCloseTimer.Stop();
        ScreenshotClipboardActions.TryCopyLastSavedRegion(TargetScreen);
    }

    private void PrtMenuMonitor_OnClick(object sender, RoutedEventArgs e)
    {
        PrtMenuPopup.IsOpen = false;
        _prtMenuCloseTimer.Stop();
        ScreenshotClipboardActions.TryCopyMonitor(TargetScreen);
    }
}
