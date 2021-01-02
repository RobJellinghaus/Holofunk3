/// Copyright by Rob Jellinghaus.  All rights reserved.

using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using UnityEngine;

namespace Holofunk
{
    /// <summary>
    /// This behavior expects to inhabit a "FloatingHandTextPanel" GameObject, with a peer
    /// SolverHandler that determines the handedness this is tracking, and a child
    /// Text object with a Text Mesh component that will be updated with text about this
    /// hand.
    /// </summary>
    public class HandPoseRecognizer : MonoBehaviour
    {
        private FullHandPose _fullHandPose = new FullHandPose();

        // Update is called once per frame
        void Update()
        {
            var handJointService = CoreServices.GetInputSystemDataProvider<IMixedRealityHandJointService>();
            Handedness handedness = GetComponent<SolverHandler>().TrackedHandness;
            if (handJointService != null && handJointService.IsHandTracked(handedness))
            {
                _fullHandPose.Recalculate(handJointService, handedness);

                // and update the text
                TextMesh textMesh = gameObject.transform.GetChild(0).GetComponent<TextMesh>();
                textMesh.text =
$@"Poses: {PoseString(Finger.Thumb)}, {PoseString(Finger.Index)}, {PoseString(Finger.Middle)}, {PoseString(Finger.Ring)}, {PoseString(Finger.Pinky)}
Colinearities: {_fullHandPose.FingerColinearity(Finger.Thumb),0:f}, {_fullHandPose.FingerColinearity(Finger.Index),0:f}, {_fullHandPose.FingerColinearity(Finger.Middle),0:f}, {_fullHandPose.FingerColinearity(Finger.Ring),0:f}, {_fullHandPose.FingerColinearity(Finger.Pinky),0:f}";

                string PoseString(Finger finger)
                {
                    FingerPose pose = _fullHandPose.FingerPose(finger);
                    return pose == FingerPose.Extended ? "Ext" : pose == FingerPose.Curled ? "Curl" : "?";
                }
            }
        }
    }
}
