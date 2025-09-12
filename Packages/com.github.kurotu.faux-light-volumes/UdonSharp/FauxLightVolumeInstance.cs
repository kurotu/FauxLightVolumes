using UdonSharp;
using UnityEngine;

namespace FauxLightVolumes
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FauxLightVolumeInstance : UdonSharpBehaviour
    {
        [SerializeField]
        private GameObject ProjectorObject;

        public void Enable()
        {
            ProjectorObject.SetActive(true);
        }

        public void Disable()
        {
            ProjectorObject.SetActive(false);
        }
    }
}
