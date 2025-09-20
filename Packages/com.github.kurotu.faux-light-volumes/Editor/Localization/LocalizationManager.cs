using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using UnityEditor;
using UnityEngine;
// Simplified version: no reflection, no LocalizationAsset usage.

namespace FauxLightVolumes.Editor
{
    /// <summary>
    /// Minimal localization manager (editor-only).
    /// Loads .po files (imported as UnityEngine.LocalizationAsset) under:
    ///   Packages/com.github.kurotu.faux-light-volumes/Localization/
    /// Keeps only a map of languageCode -> LocalizationAsset and queries strings on demand.
    /// </summary>
    [InitializeOnLoad]
    internal static class LocalizationManager
    {
        private const string PackageLocalizationFolder = "Packages/com.github.kurotu.faux-light-volumes/Localization";
        private const string EditorPrefLangKey = "FauxLightVolumes.Localization.Language";

    // languageCode -> LocalizationAsset
    private static readonly Dictionary<string, LocalizationAsset> _languageAssets = new Dictionary<string, LocalizationAsset>();
        private static string _currentLanguageCode = "en"; // default
        private static bool _initialized;

        public static event Action LanguageChanged;

        public struct LanguageInfo
        {
            public string Code; // e.g. "en", "ja"
            public string DisplayName; // e.g. "English", "日本語"
        }

        private static readonly LanguageInfo[] _supportedLanguages = new[]
        {
            new LanguageInfo { Code = "en", DisplayName = "English" },
            new LanguageInfo { Code = "ja", DisplayName = "日本語" }
        };

        public static IEnumerable<LanguageInfo> SupportedLanguages => _supportedLanguages;

        public static string CurrentLanguageCode => _currentLanguageCode;

        static LocalizationManager()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;
            LoadAllLanguages();
            AutoDetectOrLoadPreviousLanguage();
        }

        private static void LoadAllLanguages()
        {
            _languageAssets.Clear();
            if (!Directory.Exists(PackageLocalizationFolder))
            {
                return; // No localization folder yet
            }
            var poFiles = Directory.GetFiles(PackageLocalizationFolder, "*.po", SearchOption.TopDirectoryOnly);
            foreach (var poFile in poFiles)
            {
                try
                {
                    var asset = AssetDatabase.LoadAssetAtPath<LocalizationAsset>(poFile);
                    if (asset == null)
                    {
                        // If for some reason importer not active yet, skip silently.
                        continue;
                    }
                    // Prefer asset.localeIsoCode; fallback to file name sans extension.
                    var codeProp = asset.localeIsoCode; // property from Unity API
                    string code = string.IsNullOrEmpty(codeProp)
                        ? Path.GetFileNameWithoutExtension(poFile).ToLowerInvariant()
                        : codeProp.Trim().ToLowerInvariant();
                    if (!_languageAssets.ContainsKey(code))
                    {
                        _languageAssets.Add(code, asset);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[FauxLightVolumes][Localization] Failed to load localization asset {poFile}: {e.Message}");
                }
            }
        }

        private static void AutoDetectOrLoadPreviousLanguage()
        {
            var saved = EditorPrefs.GetString(EditorPrefLangKey, string.Empty);
            if (!string.IsNullOrEmpty(saved) && _languageAssets.ContainsKey(saved))
            {
                _currentLanguageCode = saved;
                return;
            }

            // Generic detection from system language / culture.
            string detected = ResolveSystemLanguageCode();
            if (!_languageAssets.ContainsKey(detected))
            {
                // Prefer English if available, else first available language, else keep detected.
                if (_languageAssets.ContainsKey("en"))
                {
                    detected = "en";
                }
                else if (_languageAssets.Count > 0)
                {
                    detected = _languageAssets.Keys.First();
                }
            }
            _currentLanguageCode = detected;
        }

        // Attempts to resolve the system's language into one of the supported language codes
        // without hard-coding specific languages. Strategy:
        // 1. Match supported code directly.
        // 2. Match display name (case-insensitive) prefix.
        // 3. Use CurrentUICulture two-letter ISO code if it matches a supported code.
        // 4. Fallback to "en".
        private static string ResolveSystemLanguageCode()
        {
            // Raw names: e.g. "Japanese", "English" etc.
            string sysName = Application.systemLanguage.ToString();
            string sysLower = sysName.ToLowerInvariant();

            // Direct code or name match
            foreach (var lang in _supportedLanguages)
            {
                if (sysLower.StartsWith(lang.Code.ToLowerInvariant()))
                {
                    return lang.Code;
                }
                if (sysLower.StartsWith(lang.DisplayName.ToLowerInvariant()))
                {
                    return lang.Code;
                }
            }

            // CultureInfo based fallback
            var iso = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
            if (_supportedLanguages.Any(l => l.Code == iso))
            {
                return iso;
            }

            return "en"; // final default
        }

        public static void SetLanguage(string code)
        {
            if (string.IsNullOrEmpty(code) || code == _currentLanguageCode)
            {
                return;
            }
            if (!_languageAssets.ContainsKey(code))
            {
                return;
            }
            _currentLanguageCode = code;
            EditorPrefs.SetString(EditorPrefLangKey, code);
            LanguageChanged?.Invoke();
            // Repaint inspectors
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) {
                return key;
            }
            // Current language lookup
            if (_languageAssets.TryGetValue(_currentLanguageCode, out var asset))
            {
                try
                {
                    var localized = asset.GetLocalizedString(key);
                    // Unity's LocalizationAsset returns the key itself if not found; treat that as a miss
                    if (!string.IsNullOrEmpty(localized) && localized != key)
                        return localized;
                }
                catch (Exception) { /* ignore and try fallback */ }
            }
            // English fallback (only if not already English)
            if (_currentLanguageCode != "en" && _languageAssets.TryGetValue("en", out var enAsset))
            {
                var enValue = enAsset.GetLocalizedString(key);
                if (!string.IsNullOrEmpty(enValue))
                {
                    {
                        return enValue; // even if it's identical to key we just return it
                    }
                }
            }
            return key; // final fallback: key itself
        }
    }
}
