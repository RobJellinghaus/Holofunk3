// Copyright by Rob Jellinghaus. All rights reserved.

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
            // TODO: make KinectManager status info text public again (with a red hat)
            /*
            if (SoundManager.Instance == null)
            {
                kinectManager.statusInfoText.text = "No sound";
            }
            else
            {
                // TODO: make this more useful? or conditional? something
                kinectManager.statusInfoText.text = $"";
            }
            */
        }
    }
}
