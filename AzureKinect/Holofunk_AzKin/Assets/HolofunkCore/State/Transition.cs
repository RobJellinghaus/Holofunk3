/// Copyright by Rob Jellinghaus.  All rights reserved.

using Holofunk.Core;
using System;

namespace Holofunk.StateMachines
{
    public class Transition<TEvent>
    {
        readonly TEvent _event;

        public Transition(TEvent evt)
        {
            _event = evt;
        }

        public TEvent Event { get { return _event; } }

    }

    /// <summary>A transition in a StateMachine.</summary>
    /// <remarks>
    /// Is labeled with an event, and contains a means to compute an optional destination state.
    /// 
    /// Guarded transitions with an active guard simply return no destination state at all.
    /// Computed transitions calculate the state to return.
    /// </remarks>
    public class Transition<TEvent, TModel> : Transition<TEvent>
    {
        readonly Func<TEvent, TModel, Option<State<TEvent>>> _destinationFunc;

        public Transition(
            TEvent evt,
            Func<TEvent, TModel, Option<State<TEvent>>> destinationFunc)
            : base(evt)
        {
            _destinationFunc = destinationFunc;
        }

        public Transition(
            TEvent evt,
            State<TEvent> destinationState)
            : this(evt, (ignoreModel, ignoreEvent) => destinationState)
        {
        }

        public Transition(
            TEvent evt,
            State<TEvent> destinationState,
            Func<bool> guardFunc)
            : this(evt, (ignoreModel, ignoreEvent) => guardFunc() ? destinationState : Option<State<TEvent>>.None)
        {
        }

        /// <summary>
        /// Compute the destination state.
        /// </summary>
        /// <remarks>
        /// If this returns None, this transition is ignored and a search continues for other outer transitions.
        /// </remarks>
        public Option<State<TEvent>> ComputeDestination(TEvent evt, TModel model)
        {
            return _destinationFunc(evt, model);
        }
    }
}
