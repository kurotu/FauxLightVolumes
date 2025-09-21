using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FauxLightVolumes.Editor
{
    [CustomEditor(typeof(FauxLightVolumeSetup))]
    public class FauxLightVolumeSetupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            LanguageDropdownUtility.DrawCompactLanguageRow();
            DrawDefaultInspector();

#if FLV_VRCLV
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(LocalizationManager.Get("UI.FauxLightVolumeSetup.VRCLVIntegrationTitle"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(LocalizationManager.Get("UI.FauxLightVolumeSetup.VRCLVIntegrationHelp"), MessageType.Info);
            if (GUILayout.Button(LocalizationManager.Get("UI.FauxLightVolumeSetup.VRCLVIntegrationGenerateButton")))
            {
                GenerateOrAlignFromVRCLV((FauxLightVolumeSetup)target);
            }
#else
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(LocalizationManager.Get("UI.FauxLightVolumeSetup.VRCLVIntegrationDisabled"), MessageType.Info);
#endif
        }

#if FLV_VRCLV
        public static void GenerateOrAlignFromVRCLV(FauxLightVolumeSetup setup)
        {
            if (setup == null) return;

            // Work only within the scene that contains this setup
            var scene = setup.gameObject.scene;
            if (!scene.IsValid()) return;

            // 1) Collect all VRCLV in this scene in hierarchy order (including inactive)
            var vrclvs = CollectVRCLVInHierarchyOrder(scene);

            // 2) Collect all existing FauxLightVolumes in this scene in hierarchy order (including inactive)
            var existing = CollectFauxInHierarchyOrder(scene);

            // 3) Create/remove to match counts if necessary
            Undo.SetCurrentGroupName("Sync Faux Light Volumes with VRCLV");
            int group = Undo.GetCurrentGroup();

            var prefabPath = AssetDatabase.GUIDToAssetPath(Constants.LightVolumePrefabGUID);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Faux Light Volume", "Faux Light Volume prefab not found. Please verify the GUID in Constants.", "OK");
                return;
            }

            // Create missing ones (at this scene root)
            for (int i = existing.Count; i < vrclvs.Count; i++)
            {
                // Unique name considering current FauxLightVolumes in this scene
                var currentNames = CollectFauxInHierarchyOrder(scene).Select(e => e.gameObject.name).ToArray();

                // Instantiate into the same scene (no parent)
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                go.name = ObjectNames.GetUniqueName(currentNames, "Faux Light Volume");
                Undo.RegisterCreatedObjectUndo(go, "Create Faux Light Volume");
                existing.Add(go.GetComponent<FauxLightVolume>());
            }

            // Remove extras from the end (those appear later in the hierarchy/inspector)
            for (int i = existing.Count - 1; i >= vrclvs.Count; i--)
            {
                if (existing[i] != null)
                {
                    Undo.DestroyObjectImmediate(existing[i].gameObject);
                }
                existing.RemoveAt(i);
            }

            // 4) Align position, rotation and bounds to VRCLV using proximity-based matching
            // Create a working list of available FauxLightVolumes to assign
            var unassigned = new List<FauxLightVolume>(existing.Where(e => e != null));
            for (int i = 0; i < vrclvs.Count; i++)
            {
                var src = vrclvs[i].transform;
                if (src == null) continue;

                // Find nearest FauxLightVolume (by world position) among unassigned
                FauxLightVolume dst = null;
                float bestDist = float.MaxValue;
                var srcPos = src.position;
                for (int c = 0; c < unassigned.Count; c++)
                {
                    var candidate = unassigned[c];
                    if (candidate == null) continue;
                    float d = (candidate.transform.position - srcPos).sqrMagnitude;
                    if (d < bestDist)
                    {
                        bestDist = d;
                        dst = candidate;
                    }
                }
                if (dst == null) continue; // Should not happen unless list is empty unexpectedly
                unassigned.Remove(dst);

                Undo.RecordObject(dst.transform, "Align Faux Light Volume Transform");
                Undo.RecordObject(dst, "Align Faux Light Volume Bounds");

                // Match world position & rotation
                dst.transform.position = src.position;
                dst.transform.rotation = src.rotation;

                // Keep local scale at 1 (bounds control size)
                dst.transform.localScale = Vector3.one;

                // Compute local bounds size considering parent scale (if any)
                var worldSizeAbs = new Vector3(Mathf.Abs(src.lossyScale.x), Mathf.Abs(src.lossyScale.y), Mathf.Abs(src.lossyScale.z));
                Vector3 parentScaleAbs = Vector3.one;
                if (dst.transform.parent != null)
                {
                    var p = dst.transform.parent.lossyScale;
                    parentScaleAbs = new Vector3(Mathf.Abs(p.x), Mathf.Abs(p.y), Mathf.Abs(p.z));
                }
                const float eps = 1e-6f;
                var localSize = new Vector3(
                    worldSizeAbs.x / Mathf.Max(parentScaleAbs.x, eps),
                    worldSizeAbs.y / Mathf.Max(parentScaleAbs.y, eps),
                    worldSizeAbs.z / Mathf.Max(parentScaleAbs.z, eps)
                );
                dst.Bounds = new Bounds(Vector3.zero, localSize);
                EditorUtility.SetDirty(dst);
            }

            Undo.CollapseUndoOperations(group);

            // 5) Update manager references via the now public API
            setup.TryUpdateManagerArray();
            EditorUtility.SetDirty(setup);
        }

        private static List<VRCLightVolumes.LightVolume> CollectVRCLVInHierarchyOrder(UnityEngine.SceneManagement.Scene scene)
        {
            var list = new List<VRCLightVolumes.LightVolume>();
            if (!scene.IsValid()) return list;
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                Traverse(root.transform, t =>
                {
                    var comp = t.GetComponent<VRCLightVolumes.LightVolume>();
                    if (comp != null && comp.Dynamic == false)
                    {
                        list.Add(comp);
                    }
                });
            }
            return list;
        }

        private static List<FauxLightVolume> CollectFauxInHierarchyOrder(UnityEngine.SceneManagement.Scene scene)
        {
            var list = new List<FauxLightVolume>();
            if (!scene.IsValid())
            {
                return list;
            }
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                Traverse(root.transform, t =>
                {
                    var comp = t.GetComponent<FauxLightVolume>();
                    if (comp != null) list.Add(comp);
                });
            }
            return list;
        }

        private static void Traverse(Transform tr, System.Action<Transform> visit)
        {
            if (tr == null) return;
            visit?.Invoke(tr);
            int childCount = tr.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Traverse(tr.GetChild(i), visit);
            }
        }
#endif
    }
}
