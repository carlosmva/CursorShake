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
            var s = Clamped(AnimationSettingsStore.Current);
            var (bmp, hotspotX, hotspotY, x, y) = CursorHelper.GetCursor();

            CursorImage.Source = bmp;
            CursorImage.Width = bmp.Width;
            CursorImage.Height = bmp.Height;

            _hotspotX = hotspotX;
            _hotspotY = hotspotY;

            Width = Math.Max(256, bmp.Width * s.PeakScale * 2);
            Height = Math.Max(256, bmp.Height * s.PeakScale * 2);

            _imageLeft = (Width - bmp.Width) / 2.0;
            _imageTop = (Height - bmp.Height) / 2.0;

            Left = x - (_imageLeft + _hotspotX);
            Top = y - (_imageTop + _hotspotY);

            CursorHelper.HideSystemCursor();

            Show();
            _followTimer.Start();

            Animate(s);

            var totalMs = s.ScaleUpMs + s.HoldAtPeakMs + s.ScaleDownMs + s.EndPadMs;
            await Task.Delay(totalMs);

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

        private void Animate(AnimationSettings s)
        {
            var popEase = new QuinticEase { EasingMode = EasingMode.EaseOut };

            var upX = new DoubleAnimation(1, s.PeakScale, TimeSpan.FromMilliseconds(s.ScaleUpMs))
            {
                EasingFunction = popEase
            };

            var upY = new DoubleAnimation(1, s.PeakScale, TimeSpan.FromMilliseconds(s.ScaleUpMs))
            {
                EasingFunction = popEase
            };

            ScaleTf.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, upX);
            ScaleTf.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, upY);

            var downTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(s.ScaleUpMs + s.HoldAtPeakMs)
            };
            downTimer.Tick += (_, _) =>
            {
                downTimer.Stop();

                var settleEase = new SineEase { EasingMode = EasingMode.EaseIn };

                var downX = new DoubleAnimation(s.PeakScale, 1, TimeSpan.FromMilliseconds(s.ScaleDownMs))
                {
                    EasingFunction = settleEase
                };

                var downY = new DoubleAnimation(s.PeakScale, 1, TimeSpan.FromMilliseconds(s.ScaleDownMs))
                {
                    EasingFunction = settleEase
                };

                ScaleTf.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, downX);
                ScaleTf.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, downY);
            };
            downTimer.Start();
        }

        private static AnimationSettings Clamped(AnimationSettings raw) =>
            new()
            {
                PeakScale = Math.Round(Math.Clamp(raw.PeakScale, 1.05, 4.0), 2, MidpointRounding.AwayFromZero),
                ScaleUpMs = (int)Math.Clamp(raw.ScaleUpMs, 20, 800),
                HoldAtPeakMs = (int)Math.Clamp(raw.HoldAtPeakMs, 0, 800),
                ScaleDownMs = (int)Math.Clamp(raw.ScaleDownMs, 20, 800),
                EndPadMs = (int)Math.Clamp(raw.EndPadMs, 0, 500)
            };
    }
}
