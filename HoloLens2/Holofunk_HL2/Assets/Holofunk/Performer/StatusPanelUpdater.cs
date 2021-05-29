/// Copyright by Rob Jellinghaus.  All rights reserved.

using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Viewpoint;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using UnityEngine;

namespace Holofunk.HandPose
{
    /// <summary>
    /// This behavior expects to inhabit a "FloatingTextPanel" GameObject.
    /// </summary>
    public class StatusPanelUpdater : MonoBehaviour
    {
        private HandPoseClassifier _classifier = new HandPoseClassifier();

        // Update is called once per frame
        public void Update()
        {
            var handJointService = CoreServices.GetInputSystemDataProvider<IMixedRealityHandJointService>();
            var gazeProvider = CoreServices.InputSystem.EyeGazeProvider;
            TextMesh textMesh = gameObject.transform.GetChild(0).GetComponent<TextMesh>();

            // do we currently have a Viewpoint?
            GameObject instanceContainer = DistributedObjectFactory.FindFirstContainer(DistributedObjectFactory.DistributedType.Viewpoint);

            textMesh.text =
$@"{LocalHandString(handJointService, gazeProvider, Handedness.Left)}
{ViewpointHandString(instanceContainer, Handedness.Left)}

{LocalHandString(handJointService, gazeProvider, Handedness.Right)}
{ViewpointHandString(instanceContainer, Handedness.Right)}";

            string LocalHandString(
                IMixedRealityHandJointService hjs,
                IMixedRealityEyeGazeProvider gp,
                Handedness h)
            {
                if (hjs.IsHandTracked(h))
                {
                    Vector3 position = handJointService.RequestJointTransform(TrackedHandJoint.Palm, h).position;
                    _classifier.Recalculate(handJointService, gazeProvider, h);
                    return $"{h}: pose {_classifier.GetHandPose()}, pos {position}";
                }
                else
                {
                    return $"{h}: untracked";
                }
            }

            string ViewpointHandString(GameObject ic, Handedness h)
            {
                if (ic != null)
                {
                    // get the first viewpoint out of it
                    if (ic.transform.childCount > 0)
                    {
                        LocalViewpoint localViewpoint = ic.transform.GetChild(0).GetComponent<LocalViewpoint>();
                        if (localViewpoint.PlayerCount > 0)
                        {
                            Player player0 = localViewpoint.GetPlayer(0);
                            return $"Viewpoint {h}: pos {(h == Handedness.Left ? player0.LeftHandPosition : player0.RightHandPosition)}";
                        }
                    }
                }
                return $"Viewpoint {h}: untracked";
            }
        }
    }
}
