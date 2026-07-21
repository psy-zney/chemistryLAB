using System;

namespace ChemistryLab.Presentation
{
    /// <summary>Framework-free rectangle in screen pixels, with the origin at bottom-left.</summary>
    public struct SafeAreaRect
    {
        public SafeAreaRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public float X { get; private set; }
        public float Y { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }
        public float Right { get { return X + Width; } }
        public float Top { get { return Y + Height; } }
    }

    /// <summary>Inset margins in physical pixels for simulated device safe areas.</summary>
    public struct SafeAreaMargins
    {
        public SafeAreaMargins(float left, float right, float top, float bottom)
        {
            Left = RequireNonNegative(left, nameof(left));
            Right = RequireNonNegative(right, nameof(right));
            Top = RequireNonNegative(top, nameof(top));
            Bottom = RequireNonNegative(bottom, nameof(bottom));
        }

        public float Left { get; private set; }
        public float Right { get; private set; }
        public float Top { get; private set; }
        public float Bottom { get; private set; }

        private static float RequireNonNegative(float value, string name)
        {
            if (value < 0f) throw new ArgumentOutOfRangeException(name, "Safe-area margins cannot be negative.");
            return value;
        }
    }

    public enum SafeAreaDeviceProfile
    {
        Landscape16By9 = 0,
        Landscape19Point5By9 = 1,
        Tablet = 2
    }

    /// <summary>Calculated bounds for anchors, CTA controls, and HUD content.</summary>
    public sealed class SafeAreaLayout
    {
        internal SafeAreaLayout(SafeAreaRect screen, SafeAreaRect safeArea, SafeAreaMargins margins)
        {
            Screen = screen;
            SafeArea = safeArea;
            Margins = margins;
        }

        public SafeAreaRect Screen { get; private set; }
        public SafeAreaRect SafeArea { get; private set; }
        public SafeAreaMargins Margins { get; private set; }
    }

    /// <summary>
    /// Computes an inset content rectangle without depending on UnityEngine.UI.
    /// A Unity RectTransform adapter can apply <see cref="SafeAreaLayout.SafeArea"/>
    /// to anchors, while unit tests can call the same methods directly.
    /// </summary>
    public sealed class SafeAreaContainer
    {
        public SafeAreaLayout CurrentLayout { get; private set; }

        public event Action<SafeAreaLayout> LayoutChanged;

        public SafeAreaLayout Apply(SafeAreaRect screen, SafeAreaRect reportedSafeArea)
        {
            ValidateScreen(screen);
            var clamped = ClampToScreen(screen, reportedSafeArea);
            var margins = new SafeAreaMargins(
                clamped.X - screen.X,
                screen.Right - clamped.Right,
                screen.Top - clamped.Top,
                clamped.Y - screen.Y);
            return SetLayout(new SafeAreaLayout(screen, clamped, margins));
        }

        public SafeAreaLayout Simulate(SafeAreaRect screen, SafeAreaMargins margins)
        {
            ValidateScreen(screen);
            var width = screen.Width - margins.Left - margins.Right;
            var height = screen.Height - margins.Top - margins.Bottom;
            if (width <= 0f || height <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(margins), "Margins must leave a positive content area.");
            }

            return SetLayout(new SafeAreaLayout(
                screen,
                new SafeAreaRect(screen.X + margins.Left, screen.Y + margins.Bottom, width, height),
                margins));
        }

        public SafeAreaLayout SimulateProfile(SafeAreaDeviceProfile profile, SafeAreaRect screen)
        {
            switch (profile)
            {
                case SafeAreaDeviceProfile.Landscape16By9:
                    return Simulate(screen, new SafeAreaMargins(0f, 0f, 0f, 0f));
                case SafeAreaDeviceProfile.Landscape19Point5By9:
                    // Represents side cut-outs plus a home-indicator edge in landscape.
                    return Simulate(screen, new SafeAreaMargins(screen.Width * 0.045f, screen.Width * 0.045f, 0f, screen.Height * 0.028f));
                case SafeAreaDeviceProfile.Tablet:
                    return Simulate(screen, new SafeAreaMargins(screen.Width * 0.018f, screen.Width * 0.018f, screen.Height * 0.012f, screen.Height * 0.018f));
                default:
                    throw new ArgumentOutOfRangeException(nameof(profile));
            }
        }

        private SafeAreaLayout SetLayout(SafeAreaLayout layout)
        {
            CurrentLayout = layout;
            var changed = LayoutChanged;
            if (changed != null)
            {
                changed(layout);
            }

            return layout;
        }

        private static SafeAreaRect ClampToScreen(SafeAreaRect screen, SafeAreaRect safeArea)
        {
            var left = Math.Max(screen.X, safeArea.X);
            var bottom = Math.Max(screen.Y, safeArea.Y);
            var right = Math.Min(screen.Right, safeArea.Right);
            var top = Math.Min(screen.Top, safeArea.Top);
            if (right <= left || top <= bottom)
            {
                throw new ArgumentOutOfRangeException(nameof(safeArea), "The reported safe area does not overlap the screen.");
            }

            return new SafeAreaRect(left, bottom, right - left, top - bottom);
        }

        private static void ValidateScreen(SafeAreaRect screen)
        {
            if (screen.Width <= 0f || screen.Height <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(screen), "Screen dimensions must be positive.");
            }
        }
    }
}
