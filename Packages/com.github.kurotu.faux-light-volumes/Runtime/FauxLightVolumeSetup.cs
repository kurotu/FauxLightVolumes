using UnityEngine;
using VRC.SDKBase;

namespace FauxLightVolumes
{
    [ExecuteAlways]
    public class FauxLightVolumeSetup : MonoBehaviour
    {
#if UNITY_EDITOR
        private void OnEnable()
        {
            UnityEditor.EditorApplication.hierarchyChanged += OnHierarchyChanged;
            // Also run once immediately to initialize
            TryUpdateManagerArray();
        }

        private void OnDisable()
        {
            UnityEditor.EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        }

        private void OnHierarchyChanged()
        {
            TryUpdateManagerArray();
        }

        public void TryUpdateManagerArray()
        {
            // Find the manager component on the same GameObject by type name to avoid assembly coupling
            var manager = FindComponentByTypeName(gameObject, "FauxLightVolumeManager");
            if (manager == null)
            {
                return;
            }

            // Collect FauxLightVolumeInstance components that correspond to FauxLightVolume objects in the scene
            var instances = CollectInstancesFromVolumes();

            // Write them into the manager's serialized private field: LightVolumes
            var so = new UnityEditor.SerializedObject(manager);
            var prop = so.FindProperty("LightVolumes");
            if (prop == null || !prop.isArray)
            {
                return;
            }

            // Early-out if contents are identical to reduce churn
            if (IsSameArray(prop, instances))
            {
                return;
            }

            prop.arraySize = instances.Length;
            for (int i = 0; i < instances.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = instances[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            UnityEditor.EditorUtility.SetDirty(manager);
        }

        private static bool IsSameArray(UnityEditor.SerializedProperty arrayProp, UnityEngine.Object[] values)
        {
            if (!arrayProp.isArray)
            {
                return false;
            }
            if (arrayProp.arraySize != values.Length)
            {
                return false;
            }
            for (int i = 0; i < values.Length; i++)
            {
                var el = arrayProp.GetArrayElementAtIndex(i);
                if (el.objectReferenceValue != values[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static Component FindComponentByTypeName(GameObject go, string typeName)
        {
            var components = go.GetComponents<Component>();
            foreach (var c in components)
            {
                if (c == null) continue;
                var t = c.GetType();
                if (t.Name == typeName)
                {
                    return c;
                }
            }
            return null;
        }

        private static UnityEngine.Object[] CollectInstancesFromVolumes()
        {
            // Gather all FauxLightVolume MonoBehaviours present in open scenes (including inactive)
            var volumes = FindAllSceneObjectsOfType<FauxLightVolume>();

            var list = new System.Collections.Generic.List<UnityEngine.Object>();
            foreach (var vol in volumes)
            {
                if (vol == null || !vol.gameObject.scene.IsValid()) continue;

                // Search for components named "FauxLightVolumeInstance" under this volume (include inactive)
                var comps = vol.GetComponentsInChildren<Component>(true);
                foreach (var comp in comps)
                {
                    if (comp == null) continue;
                    if (comp.GetType().Name == "FauxLightVolumeInstance")
                    {
                        list.Add(comp);
                    }
                }
            }

            return list.ToArray();
        }

        // Helper that works across Unity versions to find scene objects including inactive
        private static T[] FindAllSceneObjectsOfType<T>() where T : Component
        {
            return UnityEngine.Object.FindObjectsOfType<T>(true);
        }
#endif // UNITY_EDITOR
    }
}
