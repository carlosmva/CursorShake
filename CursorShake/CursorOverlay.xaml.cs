using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace CursorShake
{
    public partial class CursorOverlay : Window
    {
        private readonly DispatcherTimer _followTimer;
        private int _hotspotX;
        private int _hotspotY;
        private double _imageLeft;
        private double _imageTop;

        public CursorOverlay()
        {
            InitializeComponent();
            Hide();

            _followTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
            };
            _followTimer.Tick += (_, _) => UpdatePosition();
        }

        public async Task ShowAnimated()
        {
            var (bmp, hotspotX, hotspotY, x, y) = CursorHelper.GetCursor();

            CursorImage.Source = bmp;
            CursorImage.Width = bmp.Width;
            CursorImage.Height = bmp.Height;

            _hotspotX = hotspotX;
            _hotspotY = hotspotY;

            const double scale = 2.5;

            Width = Math.Max(256, bmp.Width * scale * 2);
            Height = Math.Max(256, bmp.Height * scale * 2);

            _imageLeft = (Width - bmp.Width) / 2.0;
            _imageTop = (Height - bmp.Height) / 2.0;

            Left = x - (_imageLeft + _hotspotX);
            Top = y - (_imageTop + _hotspotY);

            CursorHelper.HideSystemCursor();

            Show();
            _followTimer.Start();

            Animate();

            await Task.Delay(350);

            _followTimer.Stop();
            Hide();
            CursorHelper.ShowSystemCursor();
        }

        private void UpdatePosition()
        {
            if (!IsVisible) return;

            var (x, y) = CursorHelper.GetCursorPosition();

            Left = x - (_imageLeft + _hotspotX);
            Top = y - (_imageTop + _hotspotY);
        }

        private void Animate()
        {
            var upX = new DoubleAnimation(1, 2.5, TimeSpan.FromMilliseconds(120))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var upY = new DoubleAnimation(1, 2.5, TimeSpan.FromMilliseconds(120))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            ScaleTf.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, upX);
            ScaleTf.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, upY);

            var downTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
            downTimer.Tick += (_, _) =>
            {
                downTimer.Stop();

                var downX = new DoubleAnimation(2.5, 1, TimeSpan.FromMilliseconds(180))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };

                var downY = new DoubleAnimation(2.5, 1, TimeSpan.FromMilliseconds(180))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };

                ScaleTf.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, downX);
                ScaleTf.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, downY);
            };
            downTimer.Start();
        }
    }
}