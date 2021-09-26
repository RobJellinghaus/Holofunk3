// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using LiteNetLib;
using UnityEngine;
using static Holofunk.VolumeWidget.VolumeWidgetMessages;

namespace Holofunk.VolumeWidget
{
    /// <summary>
    /// The distributed interface of a volume widget in the current system.
    /// </summary>
    public class DistributedVolumeWidget : DistributedComponent, IDistributedVolumeWidget
    {
        #region MonoBehaviours

        public void Start()
        {
            // If we have no ID yet, then we are an owning object that has not yet gotten an initial ID.
            // So, initialize ourselves as an owner.
            if (!Id.IsInitialized)
            {
                InitializeOwner();
            }
        }

        #endregion

        #region IDistributedVolumeWidget

        public override ILocalObject LocalObject => GetLocalVolumeWidget();

        private LocalVolumeWidget GetLocalVolumeWidget() => gameObject.GetComponent<LocalVolumeWidget>();

        /// <summary>
        /// Get the count of currently known Players.
        /// </summary>
        public VolumeWidgetState State => GetLocalVolumeWidget().State;

        /// <summary>
        /// Get the player with a given index.
        /// </summary>
        public void UpdateState(VolumeWidgetState state)
            => RouteReliableMessage(isRequest => new UpdateState(Id, isRequest, state));

        #endregion

        #region Standard meta-operations

        public override void OnDelete()
        {
            HoloDebug.Log($"DistributedVolumeWidget.OnDelete: Deleting {Id}");
            // propagate locally
            GetLocalVolumeWidget().OnDelete();
        }

        protected override void SendCreateMessage(NetPeer netPeer)
        {
            HoloDebug.Log($"Sending DistributedVolumeWidget.Create for id {Id} to peer {netPeer.EndPoint}");
            Host.SendReliableMessage(new Create(Id, GetLocalVolumeWidget().State), netPeer);
        }

        protected override void SendDeleteMessage(NetPeer netPeer, bool isRequest)
        {
            // don't do it!
        }

        #endregion

        #region Instantiation

        /// <summary>
        /// Create a new VolumeWidget at this position in viewpoint space.
        /// </summary>
        public static GameObject Create(Vector3 viewpointPosition)
        {
            GameObject prototypeWidget = DistributedObjectFactory.FindPrototypeContainer(
                DistributedObjectFactory.DistributedType.VolumeWidget);
            GameObject localContainer = DistributedObjectFactory.FindLocalhostInstanceContainer(
                DistributedObjectFactory.DistributedType.VolumeWidget);

            GameObject newWidget = Instantiate(prototypeWidget, localContainer.transform);
            newWidget.SetActive(true);
            DistributedVolumeWidget distributedWidget = newWidget.GetComponent<DistributedVolumeWidget>();
            LocalVolumeWidget localWidget = distributedWidget.GetLocalVolumeWidget();

            // First set up the Loopie state in distributed terms.
            localWidget.Initialize(new VolumeWidgetState
            {
                ViewpointPosition = viewpointPosition,
                VolumeRatio = 1 // 1 == multiply by 1 == no change in volume (yet)
            });

            // Then enable the distributed behavior.
            distributedWidget.InitializeOwner();

            // And finally set the loopie name.
            newWidget.name = $"{distributedWidget.Id}";

            return newWidget;
        }

        #endregion

    }
}
