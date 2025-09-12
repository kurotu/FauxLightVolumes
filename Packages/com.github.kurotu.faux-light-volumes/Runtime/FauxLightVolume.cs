using UnityEngine;

namespace FauxLightVolumes
{
    public class FauxLightVolume : MonoBehaviour
    {
        private static readonly Color GizmoColor = new Color(0.9f, 0.85f, 0.2f, 0.8f);

        [SerializeField]
        private Vector3 BoundsSize = Vector3.one;

        /// <summary>
        /// Computed bounds for this volume (local space).
        /// Center: Vector3.zero (local), Size: BoundsSize (local). Gizmos are drawn using
        /// transform.localToWorldMatrix so the object's position/rotation/scale are applied.
        /// </summary>
        public Bounds Bounds
        {
            get
            {
                return new Bounds(Vector3.zero, BoundsSize);
            }
            set
            {
                BoundsSize = value.size;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Visualize the bounds in the Scene view when selected
            // Draw in local space but transform by this object's TRS so rotation is reflected
            var prevColor = Gizmos.color;
            var prevMatrix = Gizmos.matrix;

            Gizmos.color = GizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.DrawWireCube(Bounds.center, Bounds.size);

            // Restore state
            Gizmos.matrix = prevMatrix;
            Gizmos.color = prevColor;
        }
    }
}
