using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using GlimmerOfHope.Core.Services;

namespace GlimmerOfHope.Core.Localization
{
    public class LocalizationManager : IService
    {
        public event Action OnLanguageChanged;

        private string _currentLanguage = "fr";
        private readonly Dictionary<string, LocalizationTable> _tables = new();
        private readonly List<string> _availableLanguages = new() { "fr", "en", "es" };

        public string CurrentLanguage => _currentLanguage;
        public IReadOnlyList<string> AvailableLanguages => _availableLanguages;

        public void Initialize()
        {
            LoadLanguage(_currentLanguage);
        }

        public void Shutdown()
        {
            _tables.Clear();
        }

        public void SetLanguage(string languageCode)
        {
            if (_currentLanguage == languageCode)
                return;

            _currentLanguage = languageCode;
            LoadLanguage(languageCode);
            OnLanguageChanged?.Invoke();
        }

        public string GetLocalizedString(string tableName, string key)
        {
            var tableKey = $"{_currentLanguage}_{tableName}";

            if (_tables.TryGetValue(tableKey, out var table))
            {
                if (table.TryGetValue(key, out var value))
                    return value;
            }

            return $"[{key}]";
        }

        public string Get(string key)
        {
            foreach (var table in _tables.Values)
            {
                if (table.TryGetValue(key, out var value))
                    return value;
            }
            return $"[{key}]";
        }

        private void LoadLanguage(string languageCode)
        {
            _tables.Clear();
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
                    var tableName = Path.GetFileNameWithoutExtension(file);
                    var tableKey = $"{languageCode}_{tableName}";
                    _tables[tableKey] = table;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Localization] Failed to load {file}: {e.Message}");
                }
            }
        }
    }
}
