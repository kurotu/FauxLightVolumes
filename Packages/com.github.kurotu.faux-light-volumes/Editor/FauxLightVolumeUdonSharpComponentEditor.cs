using UnityEditor;
using UdonSharpEditor;

namespace FauxLightVolumes.Editor
{
    abstract class FauxLightVolumeUdonSharpComponentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            LanguageDropdownUtility.DrawCompactLanguageRow();
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
            {
                return;
            }
            UdonSharpGUI.DrawVariables(target);
        }
    }

    [CustomEditor(typeof(FauxLightVolumeManager))]
    class FauxLightVolumeManagerEditor : FauxLightVolumeUdonSharpComponentEditor
    {
    }

    [CustomEditor(typeof(FauxLightVolumeInstance))]
    class FauxLightVolumeInstanceEditor : FauxLightVolumeUdonSharpComponentEditor
    {
    }
}
