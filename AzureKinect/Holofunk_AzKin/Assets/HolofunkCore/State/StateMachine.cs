/// Copyright by Rob Jellinghaus.  All rights reserved.

using Holofunk.Core;
using System.Collections.Generic;

namespace Holofunk.StateMachines
{
    /// <summary>A hierarchical state machine description.</summary>
    /// <remarks>This reacts to TEvents by transitioning between states.
    /// 
    /// The StateMachine contains the transitions between States, rather than keeping them in the 
    /// States themselves.  This supports more centralized control of transitioning.
    /// 
    /// StateMachine is an immutable class providing functional operations for returning one state from
    /// another, given a particular transition.  To actually run a state machine, use a
    /// StateMachineInstance.
    /// 
    /// Subclasses of StateMachine are intended to construct the states in a static constructor for a
    /// singleton instance.  Hence the fact that all mutators are protected.</remarks>
    public abstract class StateMachine<TEvent> 
    {
        // The single initial state of this machine.
        readonly State<TEvent> _initialState;

        // The root state of this machine.  (This is not verified; machines without a unique
        // root state will fail when making transitions between disjoint roots.)
        readonly State<TEvent> _rootState;

        // A state -> event -> state transition map.
        readonly Dictionary<State<TEvent>, List<Transition<TEvent>>> _transitions
            = new Dictionary<State<TEvent>, List<Transition<TEvent>>>();

        // Func to use when comparing transition events.
        readonly IComparer<TEvent> _eventMatcher;

        protected StateMachine(State<TEvent> initialState, IComparer<TEvent> eventMatcher)
        {
            Contract.Assert(initialState != null);

            _initialState = initialState;
            _rootState = initialState;
            while (_rootState.Parent != null) {
                _rootState = _rootState.Parent;
            }
            _eventMatcher = eventMatcher;
        }

        public State<TEvent> RootState { get { return _rootState; } }

        protected void AddTransition<TModel>(
            State<TEvent, TModel> source,
            Transition<TEvent, TModel> transition)
            where TModel : IModel
        {
            //Contract.Assert(m_states.Contains(source));
            //Contract.Assert(m_states.Contains(transition.Destination));

            List<Transition<TEvent>> list;
            if (_transitions.ContainsKey(source))
            {
                list = _transitions[source];
            }
            else
            {
                list = new List<Transition<TEvent>>();
                _transitions[source] = list;
            }

            foreach (Transition<TEvent, TModel> t in list)
            {
                // if this assertion fires, it is because you tried to add a duplicate transition on the same event from the same state
                Contract.Assert(_eventMatcher.Compare(t.Event, transition.Event) != 0);
            }

            list.Add(transition);
        }

        /// <summary>Get the initial state.</summary>
        public State<TEvent> InitialState { get { return _initialState; } }

        /// <summary>If a transition exists from the source state on the given event, return that transition's
        /// destination (or null if no such transition).</summary>
        public State<TEvent> TransitionFrom(
            State<TEvent> source,
            TEvent evt, 
            IModel model)
        {
            //Contract.Assert(m_states.Contains(source));

            // just walk down transitions in order...
            // arguably we should use IEqualityComparer here, and it might even let us optimize
            // our transition dispatch?!
            // For now, we mildly hack by ONLY testing the comparer result against 0, freeing us
            // from both having to generate a hashcode and having to get a total ordering right.
            // ... and we would like foreach, but it forces allocation, and we are trying to
            // avoid that as a rule, so we don't have terrible cleanup issues later.
            if (_transitions.ContainsKey(source)) {
                List<Transition<TEvent>> transitions = _transitions[source];
                for (int i = 0; i < transitions.Count; i++) {
                    if (_eventMatcher.Compare(evt, transitions[i].Event) == 0) {
                        // this one be it.  up to user to avoid overlapping subscriptions
                        Option<State<TEvent>> destination = source.ComputeDestination(transitions[i], evt, model);

                        if (destination.HasValue)
                        {
                            return destination.Value;
                        }
                        // else, continue searching
                    }
                }
            }

            // if there is a super-state, and it has a transition from this event, then find it
            if (source.Parent != null) {
                return TransitionFrom(source.Parent, evt, model.Parent);
            }
            else {
                return null;
            }
        }
    }
}
