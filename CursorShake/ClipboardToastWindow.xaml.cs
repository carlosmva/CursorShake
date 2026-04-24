using System.Windows;
using System.Windows.Threading;
using Screen = System.Windows.Forms.Screen;

namespace CursorShake;

public partial class ClipboardToastWindow : Window
{
    private readonly Screen _screen;
    private readonly DispatcherTimer _closeTimer;

    public ClipboardToastWindow(Screen screen, string message = "SCREENSHOT IN CLIPBOARD")
    {
        _screen = screen;
        InitializeComponent();
        ToastText.Text = message;
        _closeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2200) };
        _closeTimer.Tick += (_, _) =>
        {
            _closeTimer.Stop();
            Close();
        };
        Loaded += Window_OnLoaded;
    }

    private void Window_OnLoaded(object sender, RoutedEventArgs e)
    {
        PositionOnScreen();
        _closeTimer.Start();
    }

    private void PositionOnScreen()
    {
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is not { } target)
            return;

        var m = target.TransformFromDevice;
        var b = _screen.WorkingArea;
        var ul = m.Transform(new System.Windows.Point(b.Left, b.Top));
        var lr = m.Transform(new System.Windows.Point(b.Right, b.Bottom));
        var w = ActualWidth > 0 ? ActualWidth : Width;
        var h = ActualHeight > 0 ? ActualHeight : Height;
        Left = ul.X + (lr.X - ul.X - w) / 2;
        Top = lr.Y - h - 20;
    }
}
