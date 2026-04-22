using System.Windows;
using System.Windows.Forms;
using CursorShake.Core;

using Application = System.Windows.Application;


namespace CursorShake
{
    public partial class App : Application
    {
        private NotifyIcon? _tray;
        private SettingsWindow? _settings;
        private readonly MouseHook _hook = new();
        private readonly ShakeDetector _detector = new();
        private CursorOverlay? _overlay;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);
                AnimationSettingsStore.Load();

                // Must create after WPF application start (not in a field initializer).
                _overlay = new CursorOverlay();

                var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "cursor.ico");
                var trayIcon = System.IO.File.Exists(iconPath)
                    ? new System.Drawing.Icon(iconPath)
                    : System.Drawing.SystemIcons.Application;

                _tray = new NotifyIcon
                {
                    Icon = trayIcon,
                    Visible = true,
                    Text = "Cursor Shake"
                };

                var menu = new ContextMenuStrip();
                menu.Items.Add("Settings", null, (_, __) =>
                {
                    Current.Dispatcher.Invoke(OpenOrActivateSettings);
                });
                menu.Items.Add("Exit", null, (_, __) => Current.Dispatcher.Invoke(Shutdown));
                _tray.ContextMenuStrip = menu;

                _hook.OnMouseMove += (x, y) =>
                {
                    _detector.AddPoint(x, y);
                };

                _detector.OnShake += () =>
                {
                    Current.Dispatcher.Invoke(async () =>
                    {
                        if (_overlay is not null) await _overlay.ShowAnimated();
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

        private void OpenOrActivateSettings()
        {
            if (_settings is { IsVisible: true })
            {
                _settings.Activate();
                return;
            }
            _settings = new SettingsWindow();
            _settings.Closed += (_, __) => _settings = null;
            _settings.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _hook.Stop();
            if (_tray is { } t)
            {
                t.Visible = false;
                t.Dispose();
            }
            base.OnExit(e);
        }
    }
}