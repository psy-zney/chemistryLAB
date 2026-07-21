using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ChemistryLab.Presentation
{
    /// <summary>Small persistence boundary for presentation-only preferences.</summary>
    public interface IPresentationSettingsStore
    {
        string GetString(string key);
        void SetString(string key, string value);
    }

    /// <summary>
    /// A durable, dependency-free preference store. A platform integration may replace
    /// this with PlayerPrefs, cloud save, or a native settings implementation.
    /// </summary>
    public sealed class FilePresentationSettingsStore : IPresentationSettingsStore
    {
        private readonly object syncRoot = new object();
        private readonly string path;
        private readonly Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);

        public FilePresentationSettingsStore(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("A settings file path is required.", nameof(filePath));
            path = Path.GetFullPath(filePath);
            Load();
        }

        public string GetString(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A settings key is required.", nameof(key));
            lock (syncRoot)
            {
                string value;
                return values.TryGetValue(key, out value) ? value : null;
            }
        }

        public void SetString(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A settings key is required.", nameof(key));
            lock (syncRoot)
            {
                if (value == null) values.Remove(key);
                else values[key] = value;
                Persist();
            }
        }

        private void Load()
        {
            lock (syncRoot)
            {
                if (!File.Exists(path)) return;
                try
                {
                    var lines = File.ReadAllLines(path, Encoding.UTF8);
                    for (var index = 0; index < lines.Length; index++)
                    {
                        var separator = lines[index].IndexOf(':');
                        if (separator <= 0) continue;
                        var key = Decode(lines[index].Substring(0, separator));
                        var value = Decode(lines[index].Substring(separator + 1));
                        if (!string.IsNullOrEmpty(key)) values[key] = value;
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                catch (FormatException) { }
            }
        }

        private void Persist()
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var lines = new List<string>(values.Count);
            foreach (var entry in values) lines.Add(Encode(entry.Key) + ":" + Encode(entry.Value));
            var temporaryPath = path + ".tmp";
            File.WriteAllLines(temporaryPath, lines.ToArray(), Encoding.UTF8);
            File.Copy(temporaryPath, path, true);
            File.Delete(temporaryPath);
        }

        private static string Encode(string value) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(value)); }
        private static string Decode(string value) { return Encoding.UTF8.GetString(Convert.FromBase64String(value)); }
    }

    internal static class PresentationSettings
    {
        private static readonly IPresentationSettingsStore defaultStore = new FilePresentationSettingsStore(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChemistryLab", "presentation-settings.dat"));

        public static IPresentationSettingsStore DefaultStore { get { return defaultStore; } }
    }
}
