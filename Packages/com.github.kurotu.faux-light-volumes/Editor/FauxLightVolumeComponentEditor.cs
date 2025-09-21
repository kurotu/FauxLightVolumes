using UnityEditor;

namespace FauxLightVolumes.Editor
{
    [CustomEditor(typeof(FauxLightVolumeComponent))]
    class FauxLightVolumeComponentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            LanguageDropdownUtility.DrawCompactLanguageRow();
            DrawDefaultInspector();
        }
    }
}
