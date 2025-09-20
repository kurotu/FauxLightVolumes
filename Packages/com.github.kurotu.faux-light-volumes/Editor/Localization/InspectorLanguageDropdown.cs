using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FauxLightVolumes.Editor
{
    /// <summary>
    /// Adds a language selector dropdown to every inspector header (MonoBehaviour/UdonSharpBehaviour included).
    /// </summary>
    /// <summary>
    /// Shared IMGUI helper to draw language selection inside custom inspectors.
    /// </summary>
    internal static class LanguageDropdownUtility
    {
        private static readonly GUIContent _languageLabel = new GUIContent("Language");

        public static void DrawCompactLanguageRow()
        {
            var langs = LocalizationManager.SupportedLanguages.ToArray();
            if (langs.Length == 0)
            {
                return;
            }
            var codes = langs.Select(l => l.Code).ToArray();
            var names = langs.Select(l => l.DisplayName).ToArray();
            int currentIndex = System.Array.IndexOf(codes, LocalizationManager.CurrentLanguageCode);
            if (currentIndex < 0) currentIndex = 0;

            using (new EditorGUILayout.HorizontalScope())
            {
                using var changeCheck = new EditorGUI.ChangeCheckScope();
                int newIndex = EditorGUILayout.Popup(_languageLabel, currentIndex, names);
                if (changeCheck.changed)
                {
                    if (newIndex >= 0 && newIndex < codes.Length)
                    {
                        LocalizationManager.SetLanguage(codes[newIndex]);
                    }
                }
            }
        }
    }
}
