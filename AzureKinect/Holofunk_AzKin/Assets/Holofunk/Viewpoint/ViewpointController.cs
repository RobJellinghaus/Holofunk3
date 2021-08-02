// Copyright by Rob Jellinghaus. All rights reserved.

using com.rfilkov.kinect;
using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Sound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                SoundManager sm = SoundManager.Instance;
                kinectManager.statusInfoText.text = $"AudioNow: {Clock.Instance.AudioNow}";
            }
        }
    }
}
