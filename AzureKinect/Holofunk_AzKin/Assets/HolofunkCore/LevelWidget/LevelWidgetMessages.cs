// Copyright by Rob Jellinghaus. All rights reserved.

using DistributedStateLib;
using Holofunk.Distributed;

namespace Holofunk.LevelWidget
{
    public class LevelWidgetMessages : Messages
    {
        public class Create : CreateMessage
        {
            public LevelWidgetState State { get; set; }
            public Create() : base() { }
            public Create(DistributedId id, LevelWidgetState state) : base(id) { State = state; }
        }

        public class Delete : DeleteMessage
        {
            public Delete() : base() { }
            public Delete(DistributedId id, bool isRequest) : base(id, isRequest) { }
            public override string ToString() => $"{base.ToString()}{Id}";
        }

        public class UpdateState : ReliableMessage
        {
            public LevelWidgetState State { get; set; }
            public UpdateState() : base() { }
            public UpdateState(DistributedId id, bool isRequest, LevelWidgetState state) : base(id, isRequest) { State = state; }
            public override void Invoke(IDistributedInterface target) => ((IDistributedLevelWidget)target).UpdateState(State);
        }

        public static void RegisterTypes(DistributedHost.ProxyCapability proxyCapability)
        {
            proxyCapability.RegisterType<LevelWidgetState>();
        }

        public static void Register(DistributedHost.ProxyCapability proxyCapability)
        {
            Registrar.RegisterCreateMessage<Create, DistributedLevelWidget, LocalLevelWidget, IDistributedLevelWidget>(
                proxyCapability,
                DistributedObjectFactory.DistributedType.LevelWidget,
                (local, message) => local.Initialize(message.State));

            Registrar.RegisterDeleteMessage<Delete, DistributedLevelWidget, LocalLevelWidget, IDistributedLevelWidget>(proxyCapability);

            Registrar.RegisterReliableMessage<UpdateState, DistributedLevelWidget, LocalLevelWidget, IDistributedLevelWidget>(
                proxyCapability);
        }
    }
}
