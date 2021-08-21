// Copyright by Rob Jellinghaus. All rights reserved.

using Holofunk.Core;
using Holofunk.Viewpoint;
using UnityEngine;

namespace Holofunk.Camera
{
    /// <summary>
    /// This component causes its containing transform to face towards the camera on each frame.
    /// </summary>
    public class LocalCameraFacer : MonoBehaviour
    {
        public void Update()
        {
            // Look for a Viewpoint.
            DistributedViewpoint viewpoint = DistributedViewpoint.Instance;
            Option<Vector3> cameraPosition = LocalCamera.Instance.LastCameraPosition;
            if (viewpoint != null && cameraPosition.HasValue)
            {
                Vector3 forwardDirection = transform.rotation * new Vector3(0, 0, 1);
                Option<Quaternion> rotation = LocalCamera.Instance.CameraFacingFlatRotation(
                    transform.position,
                    forwardDirection);
                // Must have value because there is a known camera position
                Contract.Assert(rotation.HasValue);
                transform.rotation = transform.rotation * rotation.Value;
            }
        }
    }
}
