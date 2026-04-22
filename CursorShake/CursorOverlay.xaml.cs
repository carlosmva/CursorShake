using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace CursorShake
{
    public partial class CursorOverlay : Window
    {
        // One place to tune; total must match `await Task.Delay` in `ShowAnimated`.
        // Hold: linger at max scale so the “pop” reads before the shrink.
        private const int AnimScaleUpMs = 120;
        private const int AnimHoldAtPeakMs = 80;
        private const int AnimScaleDownMs = 180;
        // Extra ms after the shrink finishes before hiding (was ~50ms with Delay(350)).
        private const int AnimEndPadMs = 50;

        // Max scale during the flash; window uses this so the enlarged cursor is not clipped.
        private const double AnimPeakScale = 2.65;

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

            Width = Math.Max(256, bmp.Width * AnimPeakScale * 2);
            Height = Math.Max(256, bmp.Height * AnimPeakScale * 2);

            _imageLeft = (Width - bmp.Width) / 2.0;
            _imageTop = (Height - bmp.Height) / 2.0;

            Left = x - (_imageLeft + _hotspotX);
            Top = y - (_imageTop + _hotspotY);

            CursorHelper.HideSystemCursor();

            Show();
            _followTimer.Start();

            Animate();

            // Pop, optional hold, shrink, then a short beat before unhide
            var totalMs = AnimScaleUpMs + AnimHoldAtPeakMs + AnimScaleDownMs + AnimEndPadMs;
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

        private void Animate()
        {
            var popEase = new QuinticEase { EasingMode = EasingMode.EaseOut };

            var upX = new DoubleAnimation(1, AnimPeakScale, TimeSpan.FromMilliseconds(AnimScaleUpMs))
            {
                EasingFunction = popEase
            };

            var upY = new DoubleAnimation(1, AnimPeakScale, TimeSpan.FromMilliseconds(AnimScaleUpMs))
            {
                EasingFunction = popEase
            };

            ScaleTf.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, upX);
            ScaleTf.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, upY);

            // Fire once scale-up is done, plus any hold (original: first tick = end of 120ms up).
            var downTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(AnimScaleUpMs + AnimHoldAtPeakMs)
            };
            downTimer.Tick += (_, _) =>
            {
                downTimer.Stop();

                // Softer return than cubic: ease-in at the start of the shrink reads as a gentle settle.
                var settleEase = new SineEase { EasingMode = EasingMode.EaseIn };

                var downX = new DoubleAnimation(AnimPeakScale, 1, TimeSpan.FromMilliseconds(AnimScaleDownMs))
                {
                    EasingFunction = settleEase
                };

                var downY = new DoubleAnimation(AnimPeakScale, 1, TimeSpan.FromMilliseconds(AnimScaleDownMs))
                {
                    EasingFunction = settleEase
                };

                ScaleTf.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, downX);
                ScaleTf.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, downY);
            };
            downTimer.Start();
        }
    }
}
