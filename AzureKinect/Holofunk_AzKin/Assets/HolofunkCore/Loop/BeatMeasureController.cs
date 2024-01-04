// Copyright by Rob Jellinghaus. All rights reserved.

using Holofunk.Core;
using Holofunk.Sound;
using NowSoundLib;
using UnityEngine;

namespace Holofunk.Loop
{
    public class BeatMeasureController : MonoBehaviour
    {
        [Tooltip("Starting measure")]
        public int startingMeasure;

        public LocalLoopie localLoopie;

        private SpriteRenderer[] hollowQuarterCircles;
        private SpriteRenderer[] filledQuarterCircles;

        // Use this for initialization
        void Start()
        {
            hollowQuarterCircles = new SpriteRenderer[DistributedSoundClock.Instance.BeatsPerMeasure];
            filledQuarterCircles = new SpriteRenderer[DistributedSoundClock.Instance.BeatsPerMeasure];
            for (int i = 0; i < DistributedSoundClock.Instance.BeatsPerMeasure; i++)
            {
                filledQuarterCircles[i] = transform.GetChild(i).gameObject.GetComponent<SpriteRenderer>();
                filledQuarterCircles[i].enabled = false;
                hollowQuarterCircles[i] = transform.GetChild(i + 4).gameObject.GetComponent<SpriteRenderer>();
                hollowQuarterCircles[i].enabled = false;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (DistributedSoundClock.Instance == null)
            {
                return;
            }

            // local copy of struct
            NowSoundLib.TrackInfo trackInfo = localLoopie.TrackInfo;

            // How many beats long is the audio track?
            Duration<Beat> trackDuration = trackInfo.DurationInBeats;

            // And at what fractional beat position is it right now?
            ContinuousDuration<Beat> localClockBeat = trackInfo.LocalClockBeat;

            int startingBeat = startingMeasure * (int)trackInfo.BeatsPerMeasure;

            // How many segments should we display?
            int segmentCount = (int)trackInfo.BeatsPerMeasure;
            if (trackDuration > startingBeat && trackDuration <= startingBeat + (int)trackInfo.BeatsPerMeasure)
            {
                segmentCount = (int)trackDuration - startingBeat;
            }

            for (int i = 0; i < 4; i++)
            {
                hollowQuarterCircles[i].enabled = i < segmentCount;
            }

            // now fade in the appropriate filled quarter-circle (if any)
            int truncatedTrackPosition = (int)localClockBeat;
            if (truncatedTrackPosition >= startingBeat && truncatedTrackPosition < startingBeat + (int)trackInfo.BeatsPerMeasure)
            {
                int beatWithinMeasure = truncatedTrackPosition - startingBeat;
                float fraction = (float)localClockBeat - truncatedTrackPosition;
                for (int i = 0; i < (int)trackInfo.BeatsPerMeasure; i++)
                {
                    filledQuarterCircles[i].enabled = i == beatWithinMeasure;

                    filledQuarterCircles[i].color =
                        i == beatWithinMeasure
                        ? new Color(1 - fraction, 1 - fraction, 1 - fraction, 1 - fraction)
                        : new Color(0, 0, 0, 0);
                }
            }
            else
            {
                // make sure everything is faded out completely
                for (int i = 0; i < trackInfo.BeatsPerMeasure; i++)
                {
                    filledQuarterCircles[i].enabled = false;
                }
            }
        }
    }
}
