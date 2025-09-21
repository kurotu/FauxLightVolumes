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
        private const string LanguageDisplayNameKey = "LanguageDisplayName"; // Each locale .po should provide msgid "LanguageDisplayName" for its self-name.

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

    // Dynamically populated from discovered .po files.
    // DisplayName resolution order (first non-empty wins):
    //  1. Localization string with key "LanguageDisplayName" inside that locale file.
    //  2. CultureInfo(code).NativeName (if valid culture)
    //  3. Upper-cased language code
        private static readonly List<LanguageInfo> _supportedLanguages = new List<LanguageInfo>();

        public static IEnumerable<LanguageInfo> SupportedLanguages => _supportedLanguages.OrderBy(l => l.Code);

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
            _supportedLanguages.Clear();
            if (!Directory.Exists(PackageLocalizationFolder))
            {
                Debug.LogWarning($"[FauxLightVolumes] Localization folder not found: {PackageLocalizationFolder}");
                return; // No localization folder yet
            }
            var poFiles = Directory.GetFiles(PackageLocalizationFolder, "*.po", SearchOption.TopDirectoryOnly);
            foreach (var poFile in poFiles)
            {
                var asset = AssetDatabase.LoadAssetAtPath<LocalizationAsset>(poFile);
                if (asset == null)
                {
                    continue;
                }
                string codeProp = asset.localeIsoCode;
                string code = string.IsNullOrEmpty(codeProp)
                    ? Path.GetFileNameWithoutExtension(poFile).ToLowerInvariant()
                    : codeProp.Trim().ToLowerInvariant();
                if (_languageAssets.ContainsKey(code))
                {
                    Debug.LogWarning($"[FauxLightVolumes] Duplicate localization asset found: {code}");
                    continue;
                }

                _languageAssets.Add(code, asset);

                // Resolve display name via localization key inside the asset.
                string displayName = null;
                var candidate = SafeGetLocalized(asset, LanguageDisplayNameKey);
                if (!string.IsNullOrEmpty(candidate) && candidate != LanguageDisplayNameKey)
                {
                    displayName = candidate.Trim();
                }

                if (string.IsNullOrEmpty(displayName))
                {
                    displayName = SafeGetCultureNativeName(code) ?? code.ToUpperInvariant();
                }

                if (!_supportedLanguages.Any(l => l.Code == code))
                {
                    _supportedLanguages.Add(new LanguageInfo { Code = code, DisplayName = displayName });
                }
            }
        }

        private static string SafeGetLocalized(LocalizationAsset asset, string key)
        {
            try { return asset.GetLocalizedString(key); }
            catch { return null; }
        }

        private static string SafeGetCultureNativeName(string code)
        {
            try { return CultureInfo.GetCultureInfo(code).NativeName; }
            catch { return null; }
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
            // If list contains English choose it; else first available; else default en
            if (_supportedLanguages.Any(l => l.Code == "en"))
            {
                return "en";
            }
            if (_supportedLanguages.Count > 0)
            {
                return _supportedLanguages[0].Code;
            }
            return "en"; // no languages loaded yet
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
                var localized = SafeGetLocalized(asset, key);
                if (!string.IsNullOrEmpty(localized) && localized != key)
                {
                    return localized;
                }
            }
            // English fallback (only if not already English)
            if (_currentLanguageCode != "en" && _languageAssets.TryGetValue("en", out var enAsset))
            {
                var enValue = SafeGetLocalized(enAsset, key);
                if (!string.IsNullOrEmpty(enValue))
                {
                    return enValue;
                }
            }
            return key; // final fallback: key itself
        }
    }
}
