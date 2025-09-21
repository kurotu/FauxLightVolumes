using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FauxLightVolumes.Editor
{
    static class GameObjectMenu
    {
        // VRC Light Volumes: 9999
        [MenuItem("GameObject/Faux Light Volume", false, 10000)]
        private static void CreateLightVolume(MenuCommand command)
        {
            var setupExisting = Object.FindObjectOfType<FauxLightVolumeSetup>();
            if (setupExisting == null)
            {
                var managerPrefabPath = AssetDatabase.GUIDToAssetPath(Constants.LightVolumeManagerPrefabGUID);
                var managerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(managerPrefabPath);
                var manager = PrefabUtility.InstantiatePrefab(managerPrefab);
                Undo.RegisterCreatedObjectUndo(manager, "Create Faux Light Volume Manager");
                setupExisting = Object.FindObjectOfType<FauxLightVolumeSetup>();

#if FLV_VRCLV
                // If there is at least one VRCLightVolumes.LightVolume (static) in the scene, perform batch generate/align instead.
                var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
                bool hasVRCLV = scene.IsValid() && scene.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<VRCLightVolumes.LightVolume>(true))
                    .Any(lv => lv != null && lv.Dynamic == false);
                if (hasVRCLV)
                {
                    FauxLightVolumeSetupEditor.GenerateOrAlignFromVRCLV(setupExisting);
                    Selection.activeObject = setupExisting.gameObject;
                    return;
                }
#endif
            }

            // Default behavior: create a single Faux Light Volume
            var lightVolumes = Object.FindObjectsOfType<FauxLightVolume>(true);
            var lightVolumePrefabPath = AssetDatabase.GUIDToAssetPath(Constants.LightVolumePrefabGUID);
            var lightVolumePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(lightVolumePrefabPath);
            var parent = command.context as GameObject;
            var lightVolume = parent != null ?
                PrefabUtility.InstantiatePrefab(lightVolumePrefab, parent.transform) :
                PrefabUtility.InstantiatePrefab(lightVolumePrefab);
            lightVolume.name = ObjectNames.GetUniqueName(lightVolumes.Select(l => l.gameObject.name).ToArray(), "Faux Light Volume");
            Undo.RegisterCreatedObjectUndo(lightVolume, "Create Faux Light Volume");

            Selection.activeObject = lightVolume;
        }
    }
}
