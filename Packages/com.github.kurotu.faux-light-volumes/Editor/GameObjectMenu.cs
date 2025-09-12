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
            var lightVolumes = Object.FindObjectsOfType<FauxLightVolume>(true);

            var lightVolumePrefabPath = AssetDatabase.GUIDToAssetPath(Constants.LightVolumePrefabGUID);
            var lightVolumePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(lightVolumePrefabPath);
            var lightVolume = command.context is GameObject go ?
                PrefabUtility.InstantiatePrefab(lightVolumePrefab, go.transform) :
                PrefabUtility.InstantiatePrefab(lightVolumePrefab);
            lightVolume.name = ObjectNames.GetUniqueName(lightVolumes.Select(l => l.gameObject.name).ToArray(), "Faux Light Volume");

            Undo.RegisterCreatedObjectUndo(lightVolume, $"Create Faux Light Volume");

            var setup = Object.FindObjectOfType<FauxLightVolumeSetup>();
            if (setup == null)
            {
                var managerPrefabPath = AssetDatabase.GUIDToAssetPath(Constants.LightVolumeManagerPrefabGUID);
                var managerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(managerPrefabPath);
                var manager = PrefabUtility.InstantiatePrefab(managerPrefab);
                Undo.RegisterCreatedObjectUndo(manager, "Create Faux Light Volume Manager");
            }

            Selection.activeObject = lightVolume;
        }
    }
}
