// Copyright by Rob Jellinghaus. All rights reserved.

using DistributedStateLib;
using NowSoundLib;
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

        public void UpdateTimeInfo(TimeInfo timeInfo) => TimeInfo = timeInfo;

        public void SetBeatsPerMinute(float newBPM)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetBeatsPerMinute(newBPM);
            }
        }

        public void OnDelete()
        {
            // Nothing to do
        }

        #endregion   
    }
}
