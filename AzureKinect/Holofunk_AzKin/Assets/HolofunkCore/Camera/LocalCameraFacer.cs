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
            Option<Vector3> viewpointCameraPosition = LocalCamera.Instance.LastViewpointCameraPosition;
            if (viewpoint != null && viewpointCameraPosition.HasValue)
            {
                Vector3 localCameraPosition = viewpoint.ViewpointToLocalMatrix()
                    .MultiplyPoint(viewpointCameraPosition.Value);
                Vector3 localForwardDirection = (transform.position - localCameraPosition).normalized;
                localForwardDirection.y = 0;
                if (localForwardDirection != Vector3.zero)
                {
                    // this spams if it's zero
                    Quaternion rotation = Quaternion.LookRotation(localForwardDirection, Vector3.up);
                    transform.rotation = rotation;
                }
            }
        }
    }
}
