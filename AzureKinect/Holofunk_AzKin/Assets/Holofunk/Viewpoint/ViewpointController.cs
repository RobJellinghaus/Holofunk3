// Copyright by Rob Jellinghaus. All rights reserved.

using com.rfilkov.kinect;
using Holofunk.Distributed;
using Holofunk.Sound;
using UnityEngine;

namespace Holofunk.Viewpoint
{
    public class ViewpointController : MonoBehaviour
    {
        public void Start()
        {
            DistributedViewpoint.InitializeTheViewpoint(
                DistributedObjectFactory.FindPrototypeComponent<DistributedViewpoint>(
                    DistributedObjectFactory.DistributedType.Viewpoint));
        }

        public void Update()
        {
            KinectManager kinectManager = KinectManager.Instance;
            if (SoundManager.Instance == null)
            {
                kinectManager.statusInfoText.text = "No sound";
            }
            else
            {
                // TODO: make this more useful? or conditional? something
                kinectManager.statusInfoText.text = $"";
            }
        }
    }
}
