using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace FauxLightVolumes.Editor
{
    [CustomEditor(typeof(FauxLightVolumes.FauxLightVolume))]
    public class FauxLightVolumeEditor : UnityEditor.Editor
    {
        private BoxBoundsHandle _boundsHandle;

        private void OnEnable()
        {
            _boundsHandle ??= new BoxBoundsHandle();
        }

        public void OnSceneGUI()
        {
            var comp = (FauxLightVolumes.FauxLightVolume)target;
            var t = comp.transform;

            // Setup handle matrix so the bounds align with world axes regardless of rotation
            // We want the center at position and size equal to scale in world-space.
            using (new Handles.DrawingScope(Color.yellow))
            {
                var matrix = Handles.matrix * t.transform.localToWorldMatrix;

                _boundsHandle.center = comp.Bounds.center;
                _boundsHandle.size = comp.Bounds.size;

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    using (new Handles.DrawingScope(matrix))
                    {
                        _boundsHandle.DrawHandle();
                    }

                    if (check.changed)
                    {
                        Undo.RecordObject(comp, "Change Faux Light Volume Bounds");
                        comp.Bounds = new Bounds(_boundsHandle.center, _boundsHandle.size);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(comp);
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            LanguageDropdownUtility.DrawCompactLanguageRow();
            DrawDefaultInspector();
        }
    }
}
