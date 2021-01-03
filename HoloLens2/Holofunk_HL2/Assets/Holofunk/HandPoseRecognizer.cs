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
$@"Poses: {Pose(Finger.Thumb)}, {Pose(Finger.Index)}, {Pose(Finger.Middle)}, {Pose(Finger.Ring)}, {Pose(Finger.Pinky)}
Colinearities: {Colin(Finger.Thumb),0:f}, {Colin(Finger.Index),0:f}, {Colin(Finger.Middle),0:f}, {Colin(Finger.Ring),0:f}, {Colin(Finger.Pinky),0:f}
Adjacencies: {Adj(Finger.Thumb)}, {Adj(Finger.Index)}, {Adj(Finger.Middle)}, {Adj(Finger.Ring)}
Ratios: {Ratio(Finger.Thumb),0:f}, {Ratio(Finger.Index),0:f}, {Ratio(Finger.Middle),0:f}, {Ratio(Finger.Ring),0:f}
Hand Pose: {_fullHandPose.GetHandPose()}";

                string Pose(Finger finger)
                {
                    FingerPose pose = _fullHandPose.GetFingerPose(finger);
                    return pose == FingerPose.Extended ? "Ext" : pose == FingerPose.Curled ? "Curl" : "?";
                }

                float Colin(Finger finger) => _fullHandPose.GetFingerColinearity(finger); // short for "Colinearity"

                string Adj(Finger finger) // short for "Adjacency"
                {
                    FingerAdjacency adj = _fullHandPose.GetFingerAdjacency(finger);
                    return adj == FingerAdjacency.Adjacent ? "Adj" : adj == FingerAdjacency.NotAdjacent ? "Non" : "?";
                }

                float Ratio(Finger finger) => _fullHandPose.GetFingerAdjacencyRatio(finger);
            }
        }
    }
}
