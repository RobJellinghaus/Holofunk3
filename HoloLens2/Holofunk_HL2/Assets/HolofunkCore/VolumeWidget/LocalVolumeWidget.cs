// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Holofunk.VolumeWidget
{
    /// <summary>
    /// The local implementation of a VolumeWidget.
    /// </summary>
    /// <remarks>
    /// This just displays a Bicone, scaled properly for the current VolumeRatio of this widget.
    /// </remarks>
    public class LocalVolumeWidget : MonoBehaviour, IDistributedVolumeWidget, ILocalObject
    {
        private VolumeWidgetState state;

        public IDistributedObject DistributedObject => gameObject.GetComponent<DistributedVolumeWidget>();

        internal void Initialize(VolumeWidgetState state) => this.state = state;

        #region MonoBehaviour

        public void Update()
        {
            // Look up the state, to determine how tall which cone should be.
            if (state.VolumeRatio >= 1)
            {
                // the volume is increasing to some extent.
                // What fraction of MaxVolumeRatio is it?
                float upwardsFraction = state.VolumeRatio;

                // set the upward cone's scale equal to upwardsFraction
                // we set the z scale because of the cone's rotation
                Transform upCone = transform.GetChild(0).GetChild(0);
                upCone.localScale = new Vector3(upCone.localScale.x, upCone.localScale.y, upwardsFraction);
                upCone.localPosition = new Vector3(upCone.localPosition.x, upwardsFraction, upCone.localPosition.z);

                // set the downwards cone's Z scale to 0 (to flatten it)
                Transform downCone = transform.GetChild(0).GetChild(1);
                downCone.localScale = new Vector3(downCone.localScale.x, downCone.localScale.y, 0);
                downCone.localPosition = new Vector3(upCone.localPosition.x, -1, upCone.localPosition.z);
            }
            else
            {
                // ratio is between 1/MaxVolumeRatio and Ratio.
                // just take the reciprocal!
                float downwardsFraction = 1 / state.VolumeRatio;

                // set the upward cone's scale equal to upwardsFraction
                // we set the z scale because of the cone's rotation
                Transform upCone = transform.GetChild(0).GetChild(0);
                upCone.localScale = new Vector3(upCone.localScale.x, upCone.localScale.y, 0);
                upCone.localPosition = new Vector3(upCone.localPosition.x, 1, upCone.localPosition.z);

                // set the downwards cone's Z scale to 0 (to flatten it)
                Transform downCone = transform.GetChild(0).GetChild(1);
                downCone.localScale = new Vector3(downCone.localScale.x, downCone.localScale.y, -downwardsFraction);
                downCone.localPosition = new Vector3(downCone.localPosition.x, -downwardsFraction, downCone.localPosition.z);
            }
        }

        #endregion

        #region IDistributedViewpoint

        /// <summary>
        /// Get the player with a given index.
        /// </summary>
        /// <param name="index">Zero-based index of player to retrieve.</param>
        /// <remarks>
        /// Note that the index of the player here has nothing to do with the PlayerId field of the player;
        /// this index is semantically meaningless and only used for iterating over currently known players.
        /// </remarks>
        public VolumeWidgetState State => state;

        public void OnDelete()
        {
            HoloDebug.Log($"LocalVolumeWidget.OnDelete: Deleting {DistributedObject.Id}");

            // and we blow ourselves awaaaay
            Destroy(gameObject);
        }


        /// <summary>
        /// Update the state.
        /// </summary>
        public void UpdateState(VolumeWidgetState state)
        {
            this.state = state;
        }

        #endregion
    }
}
