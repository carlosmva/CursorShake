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
        private ToolStripMenuItem? _annotationBarMenuItem;
        private readonly List<AnnotationDockWindow> _annotationDocks = new();
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
                var screenshotPrevRegionItem = new ToolStripMenuItem("Previous region", null, (_, __) =>
                {
                    Current.Dispatcher.Invoke(() =>
                    {
                        var screen = Screen.FromPoint(System.Windows.Forms.Cursor.Position);
                        ScreenshotClipboardActions.TryCopyLastSavedRegion(screen);
                    });
                });
                var screenshotMenu = new ToolStripMenuItem("Screenshot");
                screenshotMenu.DropDownItems.Add("Monitor", null, (_, __) =>
                {
                    Current.Dispatcher.Invoke(() =>
                    {
                        var screen = Screen.FromPoint(System.Windows.Forms.Cursor.Position);
                        ScreenshotClipboardActions.TryCopyMonitor(screen);
                    });
                });
                screenshotMenu.DropDownItems.Add("Region…", null, (_, __) =>
                {
                    Current.Dispatcher.Invoke(() =>
                    {
                        var screen = Screen.FromPoint(System.Windows.Forms.Cursor.Position);
                        ScreenshotClipboardActions.TryPickRegionAndCopy(screen, owner: null);
                    });
                });
                screenshotMenu.DropDownItems.Add(screenshotPrevRegionItem);
                screenshotMenu.DropDownOpening += (_, __) =>
                {
                    screenshotPrevRegionItem.Enabled = ScreenshotRegionMemory.HasRegion;
                };
                menu.Items.Add(screenshotMenu);
                _annotationBarMenuItem = new ToolStripMenuItem("Annotation bar")
                {
                    CheckOnClick = true,
                    Checked = false
                };
                _annotationBarMenuItem.Click += (_, __) =>
                {
                    Current.Dispatcher.Invoke(() =>
                    {
                        if (_annotationBarMenuItem.Checked)
                            ShowAnnotationDocks();
                        else
                            HideAnnotationDocks();
                    });
                };
                menu.Items.Add(_annotationBarMenuItem);
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

        private void EnsureAnnotationDocks()
        {
            if (_annotationDocks.Count > 0)
                return;
            foreach (var screen in Screen.AllScreens)
            {
                var dock = new AnnotationDockWindow(screen);
                dock.ToolRequested += OnAnnotationToolRequested;
                _annotationDocks.Add(dock);
            }
        }

        private void ShowAnnotationDocks()
        {
            EnsureAnnotationDocks();
            foreach (var dock in _annotationDocks)
            {
                dock.ApplyPositionCollapsed();
                dock.Show();
            }
        }

        private void HideAnnotationDocks()
        {
            foreach (var dock in _annotationDocks)
                dock.Hide();
        }

        private void OnAnnotationToolRequested(object? sender, AnnotationTool tool)
        {
            if (sender is not AnnotationDockWindow dock)
                return;
            foreach (var d in _annotationDocks)
                d.Hide();
            var canvas = new AnnotationCanvasWindow(dock.TargetScreen, tool);
            canvas.Closed += (_, _) =>
            {
                if (_annotationBarMenuItem is { Checked: true })
                {
                    foreach (var d in _annotationDocks)
                    {
                        d.ApplyPositionCollapsed();
                        d.Show();
                    }
                }
            };
            canvas.Show();
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