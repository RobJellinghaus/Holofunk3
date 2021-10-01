/// Copyright by Rob Jellinghaus.  All rights reserved.

using Holofunk.Camera;
using Holofunk.Distributed;
using Holofunk.Viewpoint;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using UnityEngine;

namespace Holofunk.Perform
{
    /// <summary>
    /// Handle the presentation of the "sensor panel," e.g. the floating text panel that Holofunk positions
    /// over the sensor, to signal the user that recognition happened.
    /// </summary>
    public class SensorPanelController : MonoBehaviour
    {
        public void Update()
        {
            // do we currently have a Viewpoint?
            LocalViewpoint localViewpoint = DistributedObjectFactory.FindFirstInstanceComponent<LocalViewpoint>(
                DistributedObjectFactory.DistributedType.Viewpoint);
            // if not, then nothing to do
            if (localViewpoint == null)
            {
                return;
            }

            PlayerState player = localViewpoint.PlayerCount > 0 ? localViewpoint.GetPlayer(0) : default(PlayerState);

            if (player.PlayerId != default(PlayerId) && player.ViewpointToPerformerMatrix != Matrix4x4.zero)
            {
                SetTrackingActive(false);

                // move panel to the sensor's local position
                Vector3 sensorPosition = player.SensorPosition;
                Vector3 sensorPositionInPerformerSpace = player.ViewpointToPerformerMatrix.MultiplyPoint(sensorPosition);
                transform.position = sensorPositionInPerformerSpace;

                Text.text = "We see you!\nYou're ready to Holofunk!";
            }
            else
            {
                SetTrackingActive(true);

                Text.text = "Welcome to Holofunk!\nPlease look directly at the Kinect\nand raise one hand in front of you,\nfingers spread.";
            }
        }

        private TextMesh Text => transform.GetChild(0).GetComponent<TextMesh>();
        
        /// <summary>
        /// Set whether the user-tracking components of this GameObject are currently active.
        /// </summary>
        /// <remarks>
        /// They are initially active and remain so until recognition occurs.
        /// </remarks>
        /// <param name="trackUser">True if the user-tracking components of this panel should be active
        /// (e.g. the system does not currently recognize the user).</param>
        private void SetTrackingActive(bool isTrackingActive)
        {
            // these components are enabled when tracking is active
            GetComponent<SolverHandler>().enabled = isTrackingActive;
            GetComponent<ConstantViewSize>().enabled = isTrackingActive;
            GetComponent<RadialView>().enabled = isTrackingActive;

            // This component is enabled when tracking is inactive
            GetComponent<LocalCameraFacer>().enabled = !isTrackingActive;
        }
    }
}
