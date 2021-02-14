/// Copyright by Rob Jellinghaus.  All rights reserved.

using Holofunk.Core;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using UnityEngine;

namespace Holofunk.HandPose
{
    /// <summary>
    /// This behavior expects to inhabit a "FloatingHandTextPanel" GameObject, with a peer
    /// SolverHandler that determines the handedness this is tracking, and a child
    /// Text object with a Text Mesh component that will be updated with text about this
    /// hand.
    /// </summary>
    public class HandPoseVisualizer : MonoBehaviour
    {
        private HandPoseClassifier _classifier = new HandPoseClassifier();

        // Update is called once per frame
        void Update()
        {
            HoloDebug.Log("HandPoseRecognizer.Update()");

            var handJointService = CoreServices.GetInputSystemDataProvider<IMixedRealityHandJointService>();
            var gazeProvider = CoreServices.InputSystem.EyeGazeProvider;
            Handedness handedness = GetComponent<SolverHandler>().TrackedHandness;
            if (handJointService != null && handJointService.IsHandTracked(handedness))
            {
                _classifier.Recalculate(handJointService, gazeProvider, handedness);

                // and update the text
                TextMesh textMesh = gameObject.transform.GetChild(0).GetComponent<TextMesh>();
                textMesh.text =
$@"Finger poses: {Pose(Finger.Thumb)}, {Pose(Finger.Index)}, {Pose(Finger.Middle)}, {Pose(Finger.Ring)}, {Pose(Finger.Pinky)}
Finger linearities: {Colin(Finger.Thumb),0:f}, {Colin(Finger.Index),0:f}, {Colin(Finger.Middle),0:f}, {Colin(Finger.Ring),0:f}, {Colin(Finger.Pinky),0:f}
Finger co-extensions: {Ext(Finger.Thumb)}, {Ext(Finger.Index)}, {Ext(Finger.Middle)}, {Ext(Finger.Ring)}
Finger co-linearities: {PairColin(Finger.Thumb),0:f}, {PairColin(Finger.Index),0:f}, {PairColin(Finger.Middle),0:f}, {PairColin(Finger.Ring),0:f}
Eye co-linearities: {EyeColin(Finger.Index),0:f}, {EyeColin(Finger.Middle),0:f}, {EyeColin(Finger.Ring),0:f}
Fingertip / knuckle distances: {_classifier.GetSumPairwiseFingertipDistances(),0:f} / {_classifier.GetSumPairwiseKnuckleDistances():0,f} = {_classifier.GetSumPairwiseFingertipDistances()/_classifier.GetSumPairwiseKnuckleDistances(),0:f} (alt {_classifier.GetSumFingertipAltitudes(),0:f})
Hand pose: {_classifier.GetHandPose()}";

                string Pose(Finger finger)
                {
                    FingerPose pose = _classifier.GetFingerPose(finger);
                    return pose == FingerPose.Extended ? "Ext" : pose == FingerPose.Curled ? "Curl" : "?";
                }

                float Colin(Finger finger) => _classifier.GetFingerJointColinearity(finger); // short for "Colinearity"

                string Ext(Finger finger) // short for "Extension"
                {
                    FingerPairExtension ext = _classifier.GetFingerPairExtension(finger);
                    return ext == FingerPairExtension.ExtendedTogether ? "Ext" : ext == FingerPairExtension.NotExtendedTogether ? "Not" : "?";
                }

                float PairColin(Finger finger) => _classifier.GetFingerPairColinearity(finger);

                float EyeColin(Finger finger) => _classifier.GetFingerEyeColinearity(finger);
            }
            else
            {
                HoloDebug.Log($"HandPoseVisualizer.Update(): hand joint service is{(handJointService != null ? " not" : "")} null, IsHandTracked({handedness}) = {handJointService?.IsHandTracked(handedness)}");
            }
        }
    }
}
