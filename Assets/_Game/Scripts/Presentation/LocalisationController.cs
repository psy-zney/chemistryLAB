using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChemistryLab.Presentation
{
    public enum Language { Vietnamese = 0, English = 1 }

    /// <summary>Resolves UI strings from Vietnamese and English tables without Unity dependencies.</summary>
    public sealed class LocalisationController
    {
        private const string LanguageKey = "localisation.language";
        private readonly IPresentationSettingsStore settingsStore;
        private readonly Dictionary<Language, Dictionary<string, string>> tables;

        public LocalisationController(IDictionary<Language, IDictionary<string, string>> sourceTables, IPresentationSettingsStore settingsStore = null)
        {
            if (sourceTables == null) throw new ArgumentNullException(nameof(sourceTables));
            this.settingsStore = settingsStore ?? PresentationSettings.DefaultStore;
            tables = CopyTables(sourceTables);
            CurrentLanguage = ReadLanguage();
        }

        public LocalisationController(IPresentationSettingsStore settingsStore = null)
            : this(new Dictionary<Language, IDictionary<string, string>>
            {
                { Language.Vietnamese, new Dictionary<string, string>(StringComparer.Ordinal) },
                { Language.English, new Dictionary<string, string>(StringComparer.Ordinal) }
            }, settingsStore)
        {
        }

        public Language CurrentLanguage { get; private set; }
        public event Action<Language> LanguageChanged;

        public string GetString(string key, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(key)) return key ?? string.Empty;
            var formatArguments = args ?? new object[0];
            string value;
            if (TryFormat(CurrentLanguage, key, formatArguments, out value)) return value;
            if (CurrentLanguage != Language.English && TryFormat(Language.English, key, formatArguments, out value)) return value;
            return key;
        }

        public void SetLanguage(Language lang)
        {
            if (!Enum.IsDefined(typeof(Language), lang)) throw new ArgumentOutOfRangeException(nameof(lang));
            if (CurrentLanguage == lang) return;
            CurrentLanguage = lang;
            settingsStore.SetString(LanguageKey, lang.ToString());
            var changed = LanguageChanged;
            if (changed != null) changed(lang);
        }

        private bool TryFormat(Language language, string key, object[] args, out string value)
        {
            value = null;
            Dictionary<string, string> table;
            string format;
            if (!tables.TryGetValue(language, out table) || !table.TryGetValue(key, out format) || format == null) return false;
            try
            {
                value = string.Format(language == Language.Vietnamese ? new CultureInfo("vi-VN") : CultureInfo.GetCultureInfo("en-US"), format, args);
                return true;
            }
            catch (FormatException) { return false; }
        }

        private Language ReadLanguage()
        {
            var value = settingsStore.GetString(LanguageKey);
            Language language;
            return Enum.TryParse(value, true, out language) && Enum.IsDefined(typeof(Language), language) ? language : Language.Vietnamese;
        }

        private static Dictionary<Language, Dictionary<string, string>> CopyTables(IDictionary<Language, IDictionary<string, string>> source)
        {
            var result = new Dictionary<Language, Dictionary<string, string>>();
            foreach (Language language in Enum.GetValues(typeof(Language)))
            {
                IDictionary<string, string> table;
                result[language] = source.TryGetValue(language, out table) && table != null
                    ? new Dictionary<string, string>(table, StringComparer.Ordinal)
                    : new Dictionary<string, string>(StringComparer.Ordinal);
            }
            return result;
        }
    }
}
