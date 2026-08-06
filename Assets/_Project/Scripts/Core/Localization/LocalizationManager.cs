using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using GlimmerOfHope.Core.Services;

namespace GlimmerOfHope.Core.Localization
{
    public class LocalizationManager : IService
    {
        public const string FALLBACK_LANGUAGE = "fr";

        private const string PREF_KEY = "glimmer.language";

        public event Action OnLanguageChanged;

        private string _currentLanguage = FALLBACK_LANGUAGE;
        private readonly Dictionary<string, LocalizationTable> _tables = new();
        private readonly List<string> _availableLanguages = new() { "fr", "en", "es" };

        public string CurrentLanguage => _currentLanguage;
        public IReadOnlyList<string> AvailableLanguages => _availableLanguages;

        public void Initialize()
        {
            var saved = PlayerPrefs.GetString(PREF_KEY, FALLBACK_LANGUAGE);
            _currentLanguage = _availableLanguages.Contains(saved) ? saved : FALLBACK_LANGUAGE;
            ReloadTables();
        }

        public void Shutdown()
        {
            _tables.Clear();
        }

        public void SetLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode) || languageCode == _currentLanguage)
                return;

            if (!_availableLanguages.Contains(languageCode))
            {
                Debug.LogWarning($"[Localization] Langue inconnue: {languageCode}");
                return;
            }

            _currentLanguage = languageCode;
            PlayerPrefs.SetString(PREF_KEY, languageCode);
            PlayerPrefs.Save();

            ReloadTables();
            OnLanguageChanged?.Invoke();
        }

        public void CycleLanguage()
        {
            int index = _availableLanguages.IndexOf(_currentLanguage);
            int next = (index + 1) % _availableLanguages.Count;
            SetLanguage(_availableLanguages[next]);
        }

        public string GetLocalizedString(string tableName, string key)
        {
            if (string.IsNullOrEmpty(key))
                return "";

            if (TryFind(_currentLanguage, tableName, key, out var value))
                return value;

            if (TryFind(FALLBACK_LANGUAGE, tableName, key, out value))
                return value;

            return $"[{key}]";
        }

        public string Get(string key)
        {
            return GetLocalizedString(null, key);
        }

        public bool HasKey(string tableName, string key)
        {
            return TryFind(_currentLanguage, tableName, key, out _) ||
                   TryFind(FALLBACK_LANGUAGE, tableName, key, out _);
        }

        private bool TryFind(string language, string tableName, string key, out string value)
        {
            if (!string.IsNullOrEmpty(tableName) &&
                _tables.TryGetValue($"{language}_{tableName}", out var table) &&
                table.TryGetValue(key, out value))
            {
                return true;
            }

            var prefix = $"{language}_";

            foreach (var kvp in _tables)
            {
                if (!kvp.Key.StartsWith(prefix, StringComparison.Ordinal))
                    continue;

                if (kvp.Value != null && kvp.Value.TryGetValue(key, out value))
                    return true;
            }

            value = null;
            return false;
        }

        private void ReloadTables()
        {
            _tables.Clear();
            LoadLanguage(_currentLanguage);

            if (_currentLanguage != FALLBACK_LANGUAGE)
                LoadLanguage(FALLBACK_LANGUAGE);
        }

        private void LoadLanguage(string languageCode)
        {
            var basePath = Path.Combine(Application.streamingAssetsPath, "Localization", languageCode);

            if (!Directory.Exists(basePath))
            {
                Debug.LogWarning($"[Localization] Directory not found: {basePath}");
                return;
            }

            foreach (var file in Directory.GetFiles(basePath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var table = JsonUtility.FromJson<LocalizationTable>(json);

                    if (table == null)
                        continue;

                    var tableName = Path.GetFileNameWithoutExtension(file);
                    _tables[$"{languageCode}_{tableName}"] = table;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Localization] Failed to load {file}: {e.Message}");
                }
            }
        }
    }
}
