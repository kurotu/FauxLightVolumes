using System.Linq;
using UnityEngine;

namespace FauxLightVolumes
{
    [ExecuteAlways]
    public class FauxLightVolume : FauxLightVolumeComponent
    {
        private static readonly Color GizmoColor = new Color(0.9f, 0.85f, 0.2f, 0.8f);
        private Projector LightVolumeProjector;

        [SerializeField]
        [LocalizedLabel]
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
                SyncProjector();
            }
        }

        private void OnEnable()
        {
            SyncProjector();
        }

        private void OnValidate()
        {
            SyncProjector();
        }

        private void LateUpdate()
        {
            // Keep projector in sync even if transform scale changes at runtime
            SyncProjector();
        }

        /// <summary>
        /// Ensure the Projector's projection volume matches the current Bounds.
        /// Projection orientation mapping (local to this object):
        /// - Width  (X)  -> Projector width
        /// - Height (Y)  -> Projector height (orthographicSize)
        /// - Depth  (Z)  -> Projector near/far (0..depth)
        /// Note: This method does not reposition transforms. For a centered volume,
        /// place the projector so its position is at the back face of the volume
        /// (i.e., offset by -Z * depth/2 relative to the volume center), or make
        /// the projector a child and offset it accordingly.
        /// </summary>
        private void SyncProjector()
        {
            TryAutoAssignProjector();
            if (LightVolumeProjector == null)
            {
                return;
            }

            // Compute world-space size of the oriented bounds along local axes
            var localSize = Bounds.size;
            var s = transform.lossyScale;
            var worldSize = new Vector3(Mathf.Abs(s.x) * localSize.x,
                                        Mathf.Abs(s.y) * localSize.y,
                                        Mathf.Abs(s.z) * localSize.z);

            // Guard against degenerate values
            const float epsilon = 1e-4f;
            float width = Mathf.Max(worldSize.x, epsilon);
            float height = Mathf.Max(worldSize.y, epsilon);
            float depth = Mathf.Max(worldSize.z, epsilon);

            var p = LightVolumeProjector;
            p.orthographic = true;
            p.orthographicSize = height * 0.5f; // half-height in world units
            p.aspectRatio = width / height;     // width / height

            // Clip planes along projector forward
            p.nearClipPlane = 0.0f;
            p.farClipPlane = depth;

            // Align projector transform so that its near plane sits on the bounds' near face.
            // With nearClip=0, the projector position equals its near plane.
            // Our convention maps Bounds' local +Z to projector forward, and depth spans local Z.
            // Therefore, the near face in local space is at z = -localDepth/2 from the bounds center.
            var projTr = p.transform;

            // If the projector is a child (as created by the setup), snap its local transform.
            // Using local units here avoids double-applying the parent's scale.
            if (projTr.IsChildOf(transform))
            {
                // Keep rotation aligned so projector forward == this transform's local +Z
                projTr.localRotation = Quaternion.identity;

                // Place at the near face: center (usually zero) minus half local depth along local Z
                var localHalfDepth = Mathf.Max(localSize.z * 0.5f, epsilon * 0.5f);
                projTr.localPosition = Bounds.center + new Vector3(0f, 0f, -localHalfDepth);
            }
            else
            {
                // Fallback: set world pose explicitly if the projector isn't a child.
                projTr.rotation = transform.rotation;
                var localHalfDepth = Mathf.Max(localSize.z * 0.5f, epsilon * 0.5f);
                var worldNear = transform.TransformPoint(Bounds.center + new Vector3(0f, 0f, -localHalfDepth));
                projTr.position = worldNear;
            }
        }

        private Projector TryAutoAssignProjector()
        {
            if (LightVolumeProjector != null)
            {
                return LightVolumeProjector;
            }

            // Try to find a Projector component from children
            var projectors = GetComponentsInChildren<Projector>().Where(p => p.gameObject != this.gameObject).ToArray();
            if (projectors.Length > 0)
            {
                LightVolumeProjector = projectors[0];
                return LightVolumeProjector;
            }

            return null;
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
