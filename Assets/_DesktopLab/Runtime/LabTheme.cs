/* Hallmark · pre-emit critique: P5 H5 E5 S5 R5 V5
 * Hallmark · macrostructure: Map / Diagram · genre: modern-minimal · theme: Coral
 * audience: chemistry students · use: first-person experiment simulation · tone: technical
 * navigation: N9 edge-aligned HUD · footer: Ft5 contextual command line
 * enrichment: Tier-D Unity procedural 3D · anchor hue: safety coral
 * final audit: slop pass · contrast pass (40–41) · native focus states pass
 * audio: 14 procedural clips · diagnostics: F3 runtime panel · reduced motion: F10
 */
using UnityEngine;

namespace ChemistryLab.Desktop
{
    /// <summary>
    /// Named presentation tokens for the native desktop laboratory.
    /// UI colours live here; scientific sample colours remain chemistry data.
    /// </summary>
    public static class LabTheme
    {
        public static readonly Color Paper = FromHex("#F4F1EC");
        public static readonly Color PaperRaised = FromHex("#FBF9F5");
        public static readonly Color PaperDeep = FromHex("#E8E3DC");
        public static readonly Color Ink = FromHex("#1C2227");
        public static readonly Color InkSoft = FromHex("#465058");
        public static readonly Color Muted = FromHex("#6B747A");
        public static readonly Color Rule = FromHex("#C9C5BE");
        public static readonly Color RuleStrong = FromHex("#97948E");
        public static readonly Color Accent = FromHex("#D8573E");
        public static readonly Color AccentDeep = FromHex("#A63F2C");
        public static readonly Color AccentInk = FromHex("#FFF8F1");
        public static readonly Color Graphite = FromHex("#151C22");
        public static readonly Color GraphiteRaised = FromHex("#202A31");
        public static readonly Color GraphiteInk = FromHex("#EDF2F2");
        public static readonly Color Safe = FromHex("#3DA77D");
        public static readonly Color Warning = FromHex("#B54E39");
        public static readonly Color Focus = FromHex("#F17A59");
        public static readonly Color Glass = FromHex("#B8DDE1");
        public static readonly Color Steel = FromHex("#82939A");
        public static readonly Color SteelDark = FromHex("#45565E");
        public static readonly Color Bench = FromHex("#D8E1E1");
        public static readonly Color BenchTop = FromHex("#EFF2EF");
        public static readonly Color Wall = FromHex("#C8D5D7");
        public static readonly Color WallSecondary = FromHex("#AFBFC2");
        public static readonly Color Floor = FromHex("#87989E");
        public static readonly Color FloorRule = FromHex("#677A82");
        public static readonly Color Coat = FromHex("#E7EEEC");
        public static readonly Color Glove = FromHex("#63BEB7");

        public const int ReferenceWidth = 1920;
        public const int ReferenceHeight = 1080;

        public const float Space2Xs = 4f;
        public const float SpaceXs = 8f;
        public const float SpaceSm = 12f;
        public const float SpaceMd = 16f;
        public const float SpaceLg = 24f;
        public const float SpaceXl = 40f;

        public const float DurationMicro = 0.12f;
        public const float DurationShort = 0.22f;
        public const float DurationLong = 0.42f;

        public static Font CreateBodyFont(int size = 16)
        {
            return CreateFont(
                new[] { "Segoe UI Variable Text", "Segoe UI", "Arial" },
                size);
        }

        public static Font CreateDisplayFont(int size = 20)
        {
            return CreateFont(
                new[] { "Bahnschrift", "Aptos Display", "Segoe UI Semibold", "Arial" },
                size);
        }

        public static Font CreateMonoFont(int size = 14)
        {
            return CreateFont(
                new[] { "Cascadia Mono", "JetBrains Mono", "Consolas", "Courier New" },
                size);
        }

        public static Color WithAlpha(Color colour, float alpha)
        {
            colour.a = alpha;
            return colour;
        }

        private static Font CreateFont(string[] names, int size)
        {
            var font = Font.CreateDynamicFontFromOSFont(names, size);
            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static Color FromHex(string value)
        {
            Color colour;
            return ColorUtility.TryParseHtmlString(value, out colour) ? colour : Color.magenta;
        }
    }

    public static class LabAccessibility
    {
        private const string ReducedMotionKey = "chemistryLab.desktop.reducedMotion";

        public static bool ReducedMotion
        {
            get { return PlayerPrefs.GetInt(ReducedMotionKey, 0) == 1; }
            set
            {
                PlayerPrefs.SetInt(ReducedMotionKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
