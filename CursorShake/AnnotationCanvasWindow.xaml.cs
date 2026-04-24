using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using WpfRectangle = System.Windows.Shapes.Rectangle;

namespace CursorShake;

public partial class AnnotationCanvasWindow : Window
{
    private const double StrokeW = 2;

    private readonly System.Windows.Forms.Screen _screen;
    private AnnotationTool _currentTool;
    private readonly SolidColorBrush _strokeBrush;
    private Point? _origin;
    private UIElement? _preview;
    private readonly Stack<UIElement[]> _undoChunks = new();
    private readonly Stack<UIElement[]> _redoChunks = new();
    private readonly DispatcherTimer _prtMenuCloseTimer = new() { Interval = TimeSpan.FromMilliseconds(220) };

    public AnnotationCanvasWindow(System.Windows.Forms.Screen screen, AnnotationTool tool)
    {
        _screen = screen;
        _currentTool = tool;
        _strokeBrush = new SolidColorBrush(Color.FromRgb(0xD7, 0x19, 0x21));
        InitializeComponent();
        _prtMenuCloseTimer.Tick += (_, _) =>
        {
            _prtMenuCloseTimer.Stop();
            PrtMenuPopup.IsOpen = false;
        };
        ScreenshotRegionMemory.LastRegionChanged += OnScreenshotRegionMemoryChanged;
        Closed += (_, _) => ScreenshotRegionMemory.LastRegionChanged -= OnScreenshotRegionMemoryChanged;
        SourceInitialized += (_, _) => ScreenLayoutHelper.ApplyFullScreenBounds(this, _screen);
        SizeChanged += (_, __) => SyncCanvasSize();
    }

    private void OnScreenshotRegionMemoryChanged(object? sender, EventArgs e) =>
        Dispatcher.Invoke(RefreshPrtMenuPrev);

    private void SyncCanvasSize()
    {
        DrawingCanvas.Width = DrawingHost.ActualWidth;
        DrawingCanvas.Height = DrawingHost.ActualHeight;
    }

    private void Window_OnLoaded(object sender, RoutedEventArgs e)
    {
        ScreenLayoutHelper.ApplyFullScreenBounds(this, _screen);
        SyncCanvasSize();
        RefreshSwatchSelection(SwatchRed);
        RefreshToolChrome();
        RefreshPrtMenuPrev();
        RefreshUndoRedoChrome();
        Activate();
        Focus();
        Keyboard.Focus(this);
    }

    private void Window_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var inTextEdit = Keyboard.FocusedElement is System.Windows.Controls.TextBox;

        if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control && !inTextEdit)
        {
            Undo();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control && !inTextEdit)
        {
            Redo();
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Escape)
            return;
        Close();
        e.Handled = true;
    }

    private void PushUndo(params UIElement[] elements)
    {
        _undoChunks.Push(elements);
        _redoChunks.Clear();
        RefreshUndoRedoChrome();
    }

    private void RefreshUndoRedoChrome()
    {
        BtnUndo.IsEnabled = _undoChunks.Count > 0;
        BtnRedo.IsEnabled = _redoChunks.Count > 0;
    }

    private void Undo()
    {
        CancelActiveStroke();
        if (_undoChunks.Count == 0)
            return;
        var chunk = _undoChunks.Pop();
        foreach (var x in chunk)
            DrawingCanvas.Children.Remove(x);
        _redoChunks.Push(chunk);
        RefreshUndoRedoChrome();
    }

    private void Redo()
    {
        CancelActiveStroke();
        if (_redoChunks.Count == 0)
            return;
        var chunk = _redoChunks.Pop();
        foreach (var x in chunk)
            DrawingCanvas.Children.Add(x);
        _undoChunks.Push(chunk);
        RefreshUndoRedoChrome();
    }

    private void BtnUndo_OnClick(object sender, RoutedEventArgs e) => Undo();

    private void BtnRedo_OnClick(object sender, RoutedEventArgs e) => Redo();

    private void CancelActiveStroke()
    {
        if (_origin is null)
            return;
        DrawingCanvas.ReleaseMouseCapture();
        ClearPreview();
        _origin = null;
    }

    private void SetCurrentTool(AnnotationTool t)
    {
        CancelActiveStroke();
        _currentTool = t;
        RefreshToolChrome();
    }

    private void RefreshToolChrome()
    {
        void StyleTool(Button b, bool on)
        {
            b.BorderBrush = new SolidColorBrush(on ? Color.FromRgb(0xE8, 0xE8, 0xE8) : Color.FromRgb(0x33, 0x33, 0x33));
            b.BorderThickness = new Thickness(on ? 2 : 1);
        }

        StyleTool(BtnLine, _currentTool == AnnotationTool.Line);
        StyleTool(BtnArrow, _currentTool == AnnotationTool.Arrow);
        StyleTool(BtnRect, _currentTool == AnnotationTool.Rectangle);
        StyleTool(BtnText, _currentTool == AnnotationTool.Text);
    }

    private void RefreshSwatchSelection(Button swatch)
    {
        foreach (var b in new[] { SwatchRed, SwatchWhite, SwatchGreen, SwatchAmber, SwatchBlue, SwatchInk })
        {
            var sel = ReferenceEquals(b, swatch);
            b.BorderBrush = new SolidColorBrush(sel ? Color.FromRgb(0xFF, 0xFF, 0xFF) : Color.FromRgb(0x33, 0x33, 0x33));
            b.BorderThickness = new Thickness(sel ? 2 : 1);
        }
    }

    private void BtnLine_OnClick(object sender, RoutedEventArgs e) =>
        SetCurrentTool(AnnotationTool.Line);

    private void BtnArrow_OnClick(object sender, RoutedEventArgs e) =>
        SetCurrentTool(AnnotationTool.Arrow);

    private void BtnRect_OnClick(object sender, RoutedEventArgs e) =>
        SetCurrentTool(AnnotationTool.Rectangle);

    private void BtnText_OnClick(object sender, RoutedEventArgs e) =>
        SetCurrentTool(AnnotationTool.Text);

    private void Swatch_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string hex)
            return;
        var rgb = hex.TrimStart('#');
        if (rgb.Length != 6)
            return;
        CancelActiveStroke();
        var c = Color.FromRgb(
            byte.Parse(rgb[..2], NumberStyles.HexNumber),
            byte.Parse(rgb[2..4], NumberStyles.HexNumber),
            byte.Parse(rgb[4..6], NumberStyles.HexNumber));
        _strokeBrush.Color = c;
        RefreshSwatchSelection(btn);
    }

    private void BtnPrt_OnClick(object sender, RoutedEventArgs e)
    {
        _prtMenuCloseTimer.Stop();
        PrtMenuPopup.IsOpen = false;
        CopyMonitorToClipboard();
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

    private void RefreshPrtMenuPrev() =>
        PrtMenuPrev.IsEnabled = ScreenshotRegionMemory.HasRegion;

    private void PrtMenuRegion_OnClick(object sender, RoutedEventArgs e)
    {
        PrtMenuPopup.IsOpen = false;
        _prtMenuCloseTimer.Stop();
        CancelActiveStroke();
        if (ScreenshotClipboardActions.TryPickRegionAndCopy(_screen, this))
            Close();
    }

    private void PrtMenuPrev_OnClick(object sender, RoutedEventArgs e)
    {
        PrtMenuPopup.IsOpen = false;
        _prtMenuCloseTimer.Stop();
        if (ScreenshotClipboardActions.TryCopyLastSavedRegion(_screen))
            Close();
    }

    private void PrtMenuMonitor_OnClick(object sender, RoutedEventArgs e)
    {
        PrtMenuPopup.IsOpen = false;
        _prtMenuCloseTimer.Stop();
        CopyMonitorToClipboard();
    }

    private void CopyMonitorToClipboard()
    {
        CancelActiveStroke();
        if (ScreenshotClipboardActions.TryCopyMonitor(_screen))
            Close();
    }

    private void DrawingCanvas_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var p = e.GetPosition(DrawingCanvas);

        if (_currentTool == AnnotationTool.Text)
        {
            PlaceTextBox(p);
            return;
        }

        _origin = p;
        DrawingCanvas.CaptureMouse();
        ClearPreview();
    }

    private void DrawingCanvas_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_origin is not { } o || e.LeftButton != MouseButtonState.Pressed)
            return;

        var p = e.GetPosition(DrawingCanvas);

        if (_currentTool == AnnotationTool.Text)
            return;

        ClearPreview();
        _preview = _currentTool switch
        {
            AnnotationTool.Line => PreviewLine(o, p),
            AnnotationTool.Arrow => PreviewArrow(o, p),
            AnnotationTool.Rectangle => PreviewRect(o, p),
            _ => null
        };
        if (_preview != null)
            DrawingCanvas.Children.Add(_preview);
    }

    private void DrawingCanvas_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_currentTool == AnnotationTool.Text)
            return;

        DrawingCanvas.ReleaseMouseCapture();
        if (_origin is not { } o)
            return;

        var p = e.GetPosition(DrawingCanvas);
        ClearPreview();

        switch (_currentTool)
        {
            case AnnotationTool.Line:
                CommitLine(o, p);
                break;
            case AnnotationTool.Arrow:
                CommitArrow(o, p);
                break;
            case AnnotationTool.Rectangle:
                CommitRect(o, p);
                break;
        }

        _origin = null;
    }

    private void ClearPreview()
    {
        if (_preview is null)
            return;
        DrawingCanvas.Children.Remove(_preview);
        _preview = null;
    }

    private Line PreviewLine(Point a, Point b) =>
        new()
        {
            X1 = a.X,
            Y1 = a.Y,
            X2 = b.X,
            Y2 = b.Y,
            Stroke = _strokeBrush,
            StrokeThickness = StrokeW,
            StrokeDashArray = new DoubleCollection { 4, 3 }
        };

    private UIElement PreviewArrow(Point a, Point b)
    {
        var c = new Canvas { IsHitTestVisible = false };
        c.Children.Add(new Line
        {
            X1 = a.X,
            Y1 = a.Y,
            X2 = b.X,
            Y2 = b.Y,
            Stroke = _strokeBrush,
            StrokeThickness = StrokeW,
            StrokeDashArray = new DoubleCollection { 4, 3 }
        });
        var head = ArrowHeadPolygon(a, b);
        head.Fill = System.Windows.Media.Brushes.Transparent;
        head.Stroke = _strokeBrush;
        head.StrokeThickness = StrokeW;
        head.StrokeDashArray = new DoubleCollection { 4, 3 };
        c.Children.Add(head);
        Canvas.SetLeft(c, 0);
        Canvas.SetTop(c, 0);
        return c;
    }

    private WpfRectangle PreviewRect(Point a, Point b)
    {
        var r = new WpfRectangle
        {
            Width = Math.Abs(b.X - a.X),
            Height = Math.Abs(b.Y - a.Y),
            Stroke = _strokeBrush,
            StrokeThickness = StrokeW,
            Fill = System.Windows.Media.Brushes.Transparent,
            StrokeDashArray = new DoubleCollection { 4, 3 },
            IsHitTestVisible = false
        };
        Canvas.SetLeft(r, Math.Min(a.X, b.X));
        Canvas.SetTop(r, Math.Min(a.Y, b.Y));
        return r;
    }

    private void CommitLine(Point a, Point b)
    {
        if (Distance(a, b) < 2)
            return;
        var line = new Line
        {
            X1 = a.X,
            Y1 = a.Y,
            X2 = b.X,
            Y2 = b.Y,
            Stroke = new SolidColorBrush(_strokeBrush.Color),
            StrokeThickness = StrokeW,
            IsHitTestVisible = false
        };
        DrawingCanvas.Children.Add(line);
        PushUndo(line);
    }

    private void CommitArrow(Point a, Point b)
    {
        if (Distance(a, b) < 2)
            return;
        var ink = new SolidColorBrush(_strokeBrush.Color);
        var shaft = new Line
        {
            X1 = a.X,
            Y1 = a.Y,
            X2 = b.X,
            Y2 = b.Y,
            Stroke = ink,
            StrokeThickness = StrokeW,
            IsHitTestVisible = false
        };
        DrawingCanvas.Children.Add(shaft);
        var head = ArrowHeadPolygon(a, b);
        head.Fill = ink;
        head.Stroke = ink;
        head.StrokeThickness = 1;
        head.IsHitTestVisible = false;
        DrawingCanvas.Children.Add(head);
        PushUndo(shaft, head);
    }

    private void CommitRect(Point a, Point b)
    {
        if (Math.Abs(b.X - a.X) < 2 && Math.Abs(b.Y - a.Y) < 2)
            return;
        var rect = new WpfRectangle
        {
            Width = Math.Abs(b.X - a.X),
            Height = Math.Abs(b.Y - a.Y),
            Stroke = new SolidColorBrush(_strokeBrush.Color),
            StrokeThickness = StrokeW,
            Fill = System.Windows.Media.Brushes.Transparent,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(rect, Math.Min(a.X, b.X));
        Canvas.SetTop(rect, Math.Min(a.Y, b.Y));
        DrawingCanvas.Children.Add(rect);
        PushUndo(rect);
    }

    private static Polygon ArrowHeadPolygon(Point from, Point tip, double headLen = 14, double halfW = 7)
    {
        var dx = tip.X - from.X;
        var dy = tip.Y - from.Y;
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 0.001)
            len = 0.001;
        var ux = dx / len;
        var uy = dy / len;
        var bx = tip.X - ux * headLen;
        var by = tip.Y - uy * headLen;
        var px = -uy * halfW;
        var py = ux * halfW;
        var poly = new Polygon();
        poly.Points.Add(tip);
        poly.Points.Add(new Point(bx + px, by + py));
        poly.Points.Add(new Point(bx - px, by - py));
        return poly;
    }

    private static double Distance(Point a, Point b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>Dark ink needs a light field for contrast (e.g. black swatch).</summary>
    private static bool IsDarkInk(Color c) =>
        c.R * 0.299 + c.G * 0.587 + c.B * 0.114 < 90;

    private void PlaceTextBox(Point p)
    {
        var ink = _strokeBrush.Color;
        var fg = new SolidColorBrush(ink);
        var lightField = IsDarkInk(ink);
        var tb = new System.Windows.Controls.TextBox
        {
            FontFamily = new System.Windows.Media.FontFamily("Space Grotesk, Segoe UI"),
            FontSize = 16,
            Foreground = fg,
            Background = new SolidColorBrush(lightField ? Color.FromRgb(0xFF, 0xFF, 0xFF) : Color.FromRgb(0x11, 0x11, 0x11)),
            BorderBrush = new SolidColorBrush(lightField ? Color.FromRgb(0xCC, 0xCC, 0xCC) : Color.FromRgb(0x33, 0x33, 0x33)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 6, 8, 6),
            MinWidth = 120,
            AcceptsReturn = true,
            CaretBrush = fg
        };
        Canvas.SetLeft(tb, p.X);
        Canvas.SetTop(tb, p.Y);
        DrawingCanvas.Children.Add(tb);
        PushUndo(tb);
        Dispatcher.BeginInvoke(() =>
        {
            tb.Focus();
            Keyboard.Focus(tb);
        }, DispatcherPriority.Render);
    }
}
