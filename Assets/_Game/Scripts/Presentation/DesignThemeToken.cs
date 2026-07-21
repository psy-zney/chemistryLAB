using System;
using ChemistryLab.Domain;

namespace ChemistryLab.Presentation
{
    /// <summary>Portable RGBA value that a Unity, TMP, or test adapter can convert as needed.</summary>
    [Serializable]
    public struct ThemeColor
    {
        public ThemeColor(float red, float green, float blue, float alpha)
        {
            Red = Validate(red, nameof(red));
            Green = Validate(green, nameof(green));
            Blue = Validate(blue, nameof(blue));
            Alpha = Validate(alpha, nameof(alpha));
        }

        public float Red { get; private set; }
        public float Green { get; private set; }
        public float Blue { get; private set; }
        public float Alpha { get; private set; }

        private static float Validate(float value, string name)
        {
            if (value < 0f || value > 1f) throw new ArgumentOutOfRangeException(name, "Colour channels must be in the range [0, 1].");
            return value;
        }
    }

    [Serializable]
    public sealed class TypographyToken
    {
        public TypographyToken(string fontFamily, float bodySize, float headingSize, float captionSize)
        {
            if (string.IsNullOrWhiteSpace(fontFamily)) throw new ArgumentException("A font family is required.", nameof(fontFamily));
            if (bodySize <= 0f || headingSize <= 0f || captionSize <= 0f) throw new ArgumentOutOfRangeException("Typography sizes must be positive.");
            FontFamily = fontFamily;
            BodySize = bodySize;
            HeadingSize = headingSize;
            CaptionSize = captionSize;
        }

        public string FontFamily { get; private set; }
        public float BodySize { get; private set; }
        public float HeadingSize { get; private set; }
        public float CaptionSize { get; private set; }
    }

    [Serializable]
    public sealed class ButtonSizeToken
    {
        public ButtonSizeToken(float minimumWidth, float minimumHeight, float horizontalPadding, float cornerRadius)
        {
            if (minimumWidth <= 0f || minimumHeight <= 0f || horizontalPadding < 0f || cornerRadius < 0f)
                throw new ArgumentOutOfRangeException("Button dimensions are invalid.");
            MinimumWidth = minimumWidth;
            MinimumHeight = minimumHeight;
            HorizontalPadding = horizontalPadding;
            CornerRadius = cornerRadius;
        }

        public float MinimumWidth { get; private set; }
        public float MinimumHeight { get; private set; }
        public float HorizontalPadding { get; private set; }
        public float CornerRadius { get; private set; }
    }

    /// <summary>
    /// Reusable visual tokens for UI adapters. It is intentionally a plain C# object;
    /// projects may serialize it in a ScriptableObject wrapper without making views or
    /// unit tests depend on UnityEngine.UI.
    /// </summary>
    [Serializable]
    public sealed class DesignThemeToken
    {
        public DesignThemeToken(
            ThemeColor background,
            ThemeColor surface,
            ThemeColor primary,
            ThemeColor onPrimary,
            ThemeColor textPrimary,
            ThemeColor textSecondary,
            ThemeColor warning,
            ThemeColor error,
            ThemeColor common,
            ThemeColor rare,
            ThemeColor legendary,
            TypographyToken typography,
            ButtonSizeToken standardButton)
        {
            Background = background;
            Surface = surface;
            Primary = primary;
            OnPrimary = onPrimary;
            TextPrimary = textPrimary;
            TextSecondary = textSecondary;
            Warning = warning;
            Error = error;
            Common = common;
            Rare = rare;
            Legendary = legendary;
            Typography = typography ?? throw new ArgumentNullException(nameof(typography));
            StandardButton = standardButton ?? throw new ArgumentNullException(nameof(standardButton));
        }

        public ThemeColor Background { get; private set; }
        public ThemeColor Surface { get; private set; }
        public ThemeColor Primary { get; private set; }
        public ThemeColor OnPrimary { get; private set; }
        public ThemeColor TextPrimary { get; private set; }
        public ThemeColor TextSecondary { get; private set; }
        public ThemeColor Warning { get; private set; }
        public ThemeColor Error { get; private set; }
        public ThemeColor Common { get; private set; }
        public ThemeColor Rare { get; private set; }
        public ThemeColor Legendary { get; private set; }
        public TypographyToken Typography { get; private set; }
        public ButtonSizeToken StandardButton { get; private set; }

        public ThemeColor GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare:
                case ItemRarity.Epic:
                    return Rare;
                case ItemRarity.Legendary:
                    return Legendary;
                default:
                    return Common;
            }
        }

        public static DesignThemeToken CreateDefault()
        {
            return new DesignThemeToken(
                new ThemeColor(0.035f, 0.075f, 0.105f, 1f), new ThemeColor(0.075f, 0.145f, 0.185f, 1f),
                new ThemeColor(0.10f, 0.67f, 0.73f, 1f), new ThemeColor(0.01f, 0.09f, 0.12f, 1f),
                new ThemeColor(0.94f, 0.98f, 1f, 1f), new ThemeColor(0.66f, 0.76f, 0.80f, 1f),
                new ThemeColor(1f, 0.68f, 0.12f, 1f), new ThemeColor(0.93f, 0.23f, 0.25f, 1f),
                new ThemeColor(0.66f, 0.76f, 0.80f, 1f), new ThemeColor(0.37f, 0.57f, 1f, 1f),
                new ThemeColor(0.95f, 0.69f, 0.18f, 1f),
                new TypographyToken("Noto Sans", 20f, 34f, 16f), new ButtonSizeToken(160f, 56f, 24f, 12f));
        }
    }
}
