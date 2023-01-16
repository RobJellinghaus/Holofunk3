// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Perform;
using Holofunk.Viewpoint;
using LiteNetLib;
using UnityEngine;

namespace Holofunk.Camera
{
    /// <summary>
    /// The position of the camera, in a platform-independent way.
    /// </summary>
    /// <remarks>
    /// Both the HL2 and AzKin apps need the ability to orient specific sprite planes towards the "camera"
    /// (e.g. the performer gaze position, or the player's head position). This class provides a generic
    /// CameraPosition operation which allows other code to do this reorientation.
    /// </remarks>
    public class LocalCamera : MonoBehaviour
    {
        public static LocalCamera Instance { get; private set; }

        /// <summary>
        /// The last known camera position, in viewpoint coordinates.
        /// </summary>
        /// <remarks>
        /// If this app is the viewpoint app, this is the sensor position.
        /// If this app is a performer app, this is the performer's head position.
        /// 
        /// If no camera position is known, then this is None.
        /// </remarks>
        public Option<Vector3> LastViewpointCameraPosition { get; private set; }

        public void Start()
        {
            // Only one of these ever
            Instance = this;
        }

        public void Update()
        {
            // Look for a Viewpoint.
            DistributedViewpoint viewpoint = DistributedViewpoint.Instance;
            LastViewpointCameraPosition = Option<Vector3>.None;

            if (viewpoint != null)
            {
                if (viewpoint.IsOwner)
                {
                    // we want the viewpoint camera position
                    if (viewpoint.PlayerCount > 0)
                    {
                        // TODO: hack sensor position
                        // LastViewpointCameraPosition = viewpoint.GetPlayerByIndex(0).SensorPosition;
                    }
                }
                else
                {
                    GameObject performerContainer = DistributedObjectFactory.FindPrototypeContainer(
                        DistributedObjectFactory.DistributedType.Performer);
                    LocalPerformer localPerformer = performerContainer.GetComponent<LocalPerformer>();
                    PerformerState performerState = localPerformer.GetPerformer();

                    Matrix4x4 localToViewpointMatrix = DistributedViewpoint.Instance.LocalToViewpointMatrix();
                    Vector3 viewpointHeadPosition = localToViewpointMatrix.MultiplyPoint(performerState.HeadPosition);

                    LastViewpointCameraPosition = viewpointHeadPosition;
                }
            }
        }

        /// <summary>
        /// Return a flat rotation from the object's current forward direction to a camera-facing forward direction.
        /// </summary>
        /// <param name="objectPosition">Where the object is (in viewpoint coordinates)</param>
        /// <param name="objectForwardDirection">The object's forward direction (in viewpoint coordinates)</param>
        /// <returns>A quaternion that achieves the from-to rotation to keep facing the camera.</returns>
        public Option<Quaternion> CameraFacingFlatRotation(Vector3 viewpointPosition, Vector3 viewpointForwardDirection)
        {
            if (!LastViewpointCameraPosition.HasValue)
            {
                return Option<Quaternion>.None;
            }

            // Get the vector from the viewpoint position to the camera position
            Vector3 newForwardDirection = (LastViewpointCameraPosition.Value - viewpointPosition).normalized;

            // flat rotation in XZ plane only (otherwise rotation axis order comes into play and things get odd)
            viewpointForwardDirection.y = 0;
            newForwardDirection.y = 0;

            return Quaternion.FromToRotation(viewpointForwardDirection, newForwardDirection);
        }
    }
}
