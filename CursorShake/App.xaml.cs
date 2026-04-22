using System.Windows;
using System.Windows.Forms;
using CursorShake.Core;

using Application = System.Windows.Application;


namespace CursorShake
{
    public partial class App : Application
    {
        private NotifyIcon? _tray;
        private MouseHook _hook = new();
        private ShakeDetector _detector = new();
        private CursorOverlay _overlay = new();

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

                var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "YourIcon.ico");

                _tray = new NotifyIcon
                {
                    Icon = System.Drawing.SystemIcons.Application,
                    Visible = true,
                    Text = "Cursor Shake"
                };

                var menu = new ContextMenuStrip();
                menu.Items.Add("Exit", null, (_, __) => Shutdown());
                _tray.ContextMenuStrip = menu;

                _hook.OnMouseMove += (x, y) =>
                {
                    _detector.AddPoint(x, y);
                };

                _detector.OnShake += () =>
                {
                    Current.Dispatcher.Invoke(async () =>
                    {
                        await _overlay.ShowAnimated();
                    });
                };

                _hook.Start();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString(), "Startup error");
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _tray.Visible = false;
            _tray.Dispose();
            base.OnExit(e);
        }
    }
}