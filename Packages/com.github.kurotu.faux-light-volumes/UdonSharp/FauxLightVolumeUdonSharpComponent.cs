using UdonSharp;
using UnityEngine;

namespace FauxLightVolumes
{
    /// <summary>
    /// Base class for UdonSharp faux light volume components.
    /// This class is just a marker for the editor to identify UdonSharp faux light volume components.
    /// </summary>
    [Icon("Packages/com.github.kurotu.faux-light-volumes/Resources/FauxLightVolumesIconUdonSharp.png")]
    public abstract class FauxLightVolumeUdonSharpComponent : UdonSharpBehaviour
    {
    }
}
