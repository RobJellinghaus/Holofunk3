// Copyright by Rob Jellinghaus. All rights reserved.

using DistributedStateLib;
using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Sound;
using LiteNetLib;
using UnityEngine;

namespace Holofunk.Perform
{
    public class PerformerMessages : Messages
    {
        public class Create : CreateMessage
        {
            public PerformerState Performer { get; set; }
            public Create() : base() { }
            public Create(DistributedId id, PerformerState performer) : base(id) { Performer = performer; }
        }

        public class SetTouchedLoopies : ReliableMessage
        {
            public DistributedId[] TouchedLoopieIds { get; set; }
            public SetTouchedLoopies() : base() { }
            public SetTouchedLoopies(DistributedId id, bool isRequest, DistributedId[] distributedIds) : base(id, isRequest) { TouchedLoopieIds = distributedIds; }
            public override void Invoke(IDistributedInterface target) => ((IDistributedPerformer)target).SetTouchedLoopies(TouchedLoopieIds);
        }

        public class AlterSoundEffect : ReliableMessage
        {
            public EffectId Effect { get; set; }
            public float Alteration { get; set; }
            public bool Commit { get; set; }
            public AlterSoundEffect() : base() { }
            public AlterSoundEffect(DistributedId id, bool isRequest, EffectId effect, float alteration, bool commit) : base(id, isRequest) { Effect = effect; Alteration = alteration; Commit = commit; }
            public override void Invoke(IDistributedInterface target) => ((IDistributedPerformer)target).AlterSoundEffect(Effect, Alteration, Commit);
        }

        public class PopSoundEffect : ReliableMessage
        {
            public PopSoundEffect() : base() { }
            public PopSoundEffect(DistributedId id, bool isRequest) : base(id, isRequest) { }
            public override void Invoke(IDistributedInterface target) => ((IDistributedPerformer)target).PopSoundEffect();
        }

        public class ClearSoundEffects : ReliableMessage
        {
            public ClearSoundEffects() : base() { }
            public ClearSoundEffects(DistributedId id, bool isRequest) : base(id, isRequest) { }
            public override void Invoke(IDistributedInterface target) => ((IDistributedPerformer)target).ClearSoundEffects();
        }

        public static void RegisterTypes(DistributedHost.ProxyCapability proxyCapability)
        {
            proxyCapability.RegisterType<PerformerState>();
        }

        public static void Register(DistributedHost.ProxyCapability proxyCapability)
        {
            Registrar.RegisterCreateMessage<Create, DistributedPerformer, LocalPerformer, IDistributedPerformer>(
                proxyCapability,
                DistributedObjectFactory.DistributedType.Performer,
                (local, message) => local.Initialize(message.Performer));

            Registrar.RegisterReliableMessage<SetTouchedLoopies, DistributedPerformer, LocalPerformer, IDistributedPerformer>(
                proxyCapability);
            Registrar.RegisterReliableMessage<AlterSoundEffect, DistributedPerformer, LocalPerformer, IDistributedPerformer>(
                proxyCapability);
            Registrar.RegisterReliableMessage<PopSoundEffect, DistributedPerformer, LocalPerformer, IDistributedPerformer>(
                proxyCapability);
            Registrar.RegisterReliableMessage<ClearSoundEffects, DistributedPerformer, LocalPerformer, IDistributedPerformer>(
                proxyCapability);
        }
    }
}
