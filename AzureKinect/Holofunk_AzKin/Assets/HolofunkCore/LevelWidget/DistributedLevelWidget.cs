// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using LiteNetLib;
using UnityEngine;
using static Holofunk.LevelWidget.LevelWidgetMessages;

namespace Holofunk.LevelWidget
{
    /// <summary>
    /// The distributed interface of a level widget in the current system.
    /// </summary>
    public class DistributedLevelWidget : DistributedComponent, IDistributedLevelWidget
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

        #region IDistributedLevelWidget

        public override ILocalObject LocalObject => GetLocalLevelWidget();

        private LocalLevelWidget GetLocalLevelWidget() => gameObject.GetComponent<LocalLevelWidget>();

        /// <summary>
        /// Get the count of currently known Players.
        /// </summary>
        public LevelWidgetState State => GetLocalLevelWidget().State;

        /// <summary>
        /// Get the player with a given index.
        /// </summary>
        public void UpdateState(LevelWidgetState state)
            => RouteReliableMessage(isRequest => new UpdateState(Id, isRequest, state));

        #endregion

        #region Standard meta-operations

        public override void OnDelete()
        {
            HoloDebug.Log($"DistributedLevelWidget.OnDelete: Deleting {Id}");
            // propagate locally
            GetLocalLevelWidget().OnDelete();
        }

        protected override void SendCreateMessage(NetPeer netPeer)
        {
            HoloDebug.Log($"Sending DistributedLevelWidget.Create for id {Id} to peer {netPeer.EndPoint}");
            Host.SendReliableMessage(new Create(Id, GetLocalLevelWidget().State), netPeer);
        }

        protected override void SendDeleteMessage(NetPeer netPeer, bool isRequest)
        {
            HoloDebug.Log($"DistributedLevelWidget.SendDeleteMessage: Sending LevelWidget.Delete for id {Id} to peer {netPeer.EndPoint} with state {GetLocalLevelWidget().State}");
            Host.SendReliableMessage(new Delete(Id, isRequest), netPeer);
        }

        #endregion

        #region Instantiation

        /// <summary>
        /// Create a new LevelWidget at this position in viewpoint space.
        /// </summary>
        public static GameObject Create(Vector3 viewpointPosition)
        {
            GameObject prototypeWidget = DistributedObjectFactory.FindPrototypeContainer(
                DistributedObjectFactory.DistributedType.LevelWidget);
            GameObject localContainer = DistributedObjectFactory.FindLocalhostInstanceContainer(
                DistributedObjectFactory.DistributedType.LevelWidget);

            GameObject newWidget = Instantiate(prototypeWidget, localContainer.transform);
            newWidget.SetActive(true);
            DistributedLevelWidget distributedWidget = newWidget.GetComponent<DistributedLevelWidget>();
            LocalLevelWidget localWidget = distributedWidget.GetLocalLevelWidget();

            // First set up the Loopie state in distributed terms.
            localWidget.Initialize(new LevelWidgetState
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
