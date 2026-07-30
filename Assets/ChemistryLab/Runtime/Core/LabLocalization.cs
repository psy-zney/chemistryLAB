using UnityEngine;

namespace ChemistryLab.Desktop
{
    public enum LabLanguage
    {
        Vietnamese = 0,
        English = 1
    }

    public static class LabLocalization
    {
        private const string LanguagePreferenceKey = "chemistryLab.desktop.language";

        public static LabLanguage Current
        {
            get
            {
                return PlayerPrefs.GetInt(
                    LanguagePreferenceKey,
                    (int)LabLanguage.Vietnamese) == (int)LabLanguage.English
                    ? LabLanguage.English
                    : LabLanguage.Vietnamese;
            }
            set
            {
                PlayerPrefs.SetInt(LanguagePreferenceKey, (int)value);
                PlayerPrefs.Save();
            }
        }

        public static bool IsEnglish
        {
            get { return Current == LabLanguage.English; }
        }

        public static string Text(string vietnamese, string english)
        {
            return IsEnglish ? english : vietnamese;
        }

        public static void Toggle()
        {
            Current = IsEnglish ? LabLanguage.Vietnamese : LabLanguage.English;
        }
    }
}
