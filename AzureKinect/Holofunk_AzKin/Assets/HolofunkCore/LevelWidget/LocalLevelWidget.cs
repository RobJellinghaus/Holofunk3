// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Viewpoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Holofunk.LevelWidget
{
    /// <summary>
    /// The local implementation of a LevelWidget.
    /// </summary>
    /// <remarks>
    /// This just displays a Bicone, scaled properly for the current VolumeRatio of this widget.
    /// </remarks>
    public class LocalLevelWidget : MonoBehaviour, IDistributedLevelWidget, ILocalObject
    {
        private LevelWidgetState state;

        private Vector3 lastViewpointPosition;

        public IDistributedObject DistributedObject => gameObject.GetComponent<DistributedLevelWidget>();

        internal void Initialize(LevelWidgetState state) => this.state = state;

        #region MonoBehaviour

        public void Update()
        {
            UpdateViewpointPosition();

            UpdateConeScale();
        }

        private void UpdateViewpointPosition()
        {
            if (lastViewpointPosition != state.ViewpointPosition)
            {
                Vector3 viewpointPosition = state.ViewpointPosition;

                if (DistributedViewpoint.Instance != null)
                {
                    Matrix4x4 viewpointToLocalMatrix = DistributedViewpoint.Instance.ViewpointToLocalMatrix();
                    Vector3 localPosition = viewpointToLocalMatrix.MultiplyPoint(viewpointPosition);

                    transform.localPosition = localPosition;

                    HoloDebug.Log($"LocalLevelWidget.UpdateViewpointPosition: lastViewpointPosition {lastViewpointPosition}, stateViewpointPosition {state.ViewpointPosition}, local position {transform.localPosition}");

                    lastViewpointPosition = viewpointPosition;
                }
            }
        }

        /// <summary>
        /// Update the visual scale of the bi-cones appropriately.
        /// </summary>
        private void UpdateConeScale()
        {
            // Look up the state, to determine how tall which cone should be.
            if (state.Adjustment >= 0)
            {
                // the adjustment is upwards.
                // set the upward cone's scale equal to upwardsFraction
                // we set the z scale because of the cone's rotation
                Transform upCone = transform.GetChild(0).GetChild(0);
                upCone.localScale = new Vector3(upCone.localScale.x, upCone.localScale.y, state.Adjustment);
                upCone.localPosition = new Vector3(upCone.localPosition.x, state.Adjustment, upCone.localPosition.z);

                // set the downwards cone's Z scale to almost 0 (to flatten it)
                Transform downCone = transform.GetChild(0).GetChild(1);
                downCone.localScale = new Vector3(downCone.localScale.x, downCone.localScale.y, 0.0001f);
                downCone.localPosition = new Vector3(downCone.localPosition.x, 0, downCone.localPosition.z);
            }
            else
            {
                // set the upwards cone's Z scale to almost 0 (to flatten it)
                Transform upCone = transform.GetChild(0).GetChild(0);
                upCone.localScale = new Vector3(upCone.localScale.x, upCone.localScale.y, 0.0001f);
                upCone.localPosition = new Vector3(upCone.localPosition.x, 0, upCone.localPosition.z);

                // set the downward cone's scale equal to downwardsFraction
                // we set the z scale because of the cone's rotation
                Transform downCone = transform.GetChild(0).GetChild(1);
                downCone.localScale = new Vector3(downCone.localScale.x, downCone.localScale.y, -state.Adjustment);
                downCone.localPosition = new Vector3(downCone.localPosition.x, state.Adjustment, downCone.localPosition.z);
            }
        }

        #endregion

        #region IDistributedLevelWidget

        /// <summary>
        /// Get the state.
        /// </summary>
        public LevelWidgetState State => state;

        public void OnDelete()
        {
            HoloDebug.Log($"LocalLevelWidget.OnDelete: Deleting {DistributedObject.Id}");

            // and we blow ourselves awaaaay
            Destroy(gameObject);
        }


        /// <summary>
        /// Update the state.
        /// </summary>
        public void UpdateState(LevelWidgetState state)
        {
            //HoloDebug.Log($"LocalLevelWidget.UpdateState: Updating to state {state}");
            this.state = state;
        }

        #endregion
    }
}
