// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using UnityEngine;

namespace Holofunk.Sound
{
    public class LocalSoundClock : MonoBehaviour, ILocalObject, IDistributedSoundClock
    {
        public IDistributedObject DistributedObject => gameObject.GetComponent<DistributedSoundEffect>();

        internal void Initialize(TimeInfo timeInfo)
        {
            TimeInfo = timeInfo;
        }

        #region ILocalSoundClock

        public TimeInfo TimeInfo { get; private set; }

        public void Update(TimeInfo timeInfo) => TimeInfo = timeInfo;

        public void OnDelete()
        {
            // Nothing to do
        }

        #endregion   
    }
}
