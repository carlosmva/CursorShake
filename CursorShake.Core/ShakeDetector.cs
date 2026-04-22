using System;
using System.Collections.Generic;
using System.Linq;

namespace CursorShake.Core
{
    public class ShakeDetector
    {
        private readonly List<MouseSample> _samples = new();
        private DateTime _lastShake = DateTime.MinValue;

        public event Action? OnShake;

        public void AddPoint(int x, int y)
        {
            var now = DateTime.UtcNow;
            _samples.Add(new MouseSample(x, y, now));

            // Keep only the last 350 ms
            _samples.RemoveAll(s => (now - s.Time).TotalMilliseconds > 350);

            if (_samples.Count < 6)
                return;

            DetectShake(now);
        }

        private void DetectShake(DateTime now)
        {
            int directionChanges = 0;
            int lastSign = 0;
            int horizontalTravel = 0;

            for (int i = 1; i < _samples.Count; i++)
            {
                int dx = _samples[i].X - _samples[i - 1].X;
                horizontalTravel += Math.Abs(dx);

                // Ignore tiny jitter
                if (Math.Abs(dx) < 20)
                    continue;

                int sign = Math.Sign(dx);

                if (lastSign != 0 && sign != lastSign)
                    directionChanges++;

                lastSign = sign;
            }

            int minX = _samples.Min(s => s.X);
            int maxX = _samples.Max(s => s.X);
            int horizontalSpan = maxX - minX;

            System.Diagnostics.Debug.WriteLine(
                $"shake check | flips={directionChanges} travel={horizontalTravel} span={horizontalSpan} samples={_samples.Count}");

            if (directionChanges >= 2 && horizontalTravel >= 500 && horizontalSpan >= 180)
            {
                if ((now - _lastShake).TotalMilliseconds >= 900)
                {
                    _lastShake = now;
                    _samples.Clear();
                    OnShake?.Invoke();
                }
            }
        }

        private record MouseSample(int X, int Y, DateTime Time);
    }
}