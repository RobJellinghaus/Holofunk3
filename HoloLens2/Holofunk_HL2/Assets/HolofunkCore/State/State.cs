/// Copyright by Rob Jellinghaus.  All rights reserved.

using Holofunk.Core;
using System;
using System.Collections.Generic;

namespace Holofunk.StateMachines
{
    public abstract class State<TEvent>
    {
        readonly string _label;

        protected State(string label)
        {
            _label = label;
        }

        public String Label { get { return _label; } }

        public override string ToString() => $"State[{Label}]";

        /// <summary>
        /// Enter this state, passing in the model from the parent, and obtaining the new model.
        /// </summary>
        public abstract IModel Enter(TEvent evt, IModel parentModel);

        /// <summary>
        /// Exit this state, passing in the current model, and obtaining the model for the parent state.
        /// </summary>
        public abstract IModel Exit(TEvent evt, IModel parentModel);

        public abstract State<TEvent> Parent { get; }

        /// <summary>
        /// Compute where this transition goes.  Must use the dynamic IModel type to avoid issues with finding transitions from
        /// parent states whose TParentModel is unknowable.
        /// </summary>
        /// <remarks>
        /// If this returns None, the transition was guarded and should be ignored.
        /// </remarks>
        public abstract Option<State<TEvent>> ComputeDestination(Transition<TEvent> transition, TEvent evt, IModel model);


        /// <summary>
        /// Get the parent model of this State, given the current model.
        /// </summary>
        /// <returns></returns>
        public abstract IModel GetParentModel(IModel thisModel);
    }

    public abstract class State<TEvent, TModel> : State<TEvent>
        where TModel : IModel
    {
        protected State(string label)
            : base(label)
        {
        }
    }

    /// <summary>A state in a StateMachine.</summary>
    /// <remarks>Contains entry and exit actions that reference the model.
    /// 
    /// The transitions are kept at the StateMachine level.
    /// 
    /// Note that the State has no idea that transitions even exist, nor that the
    /// StateMachine itself exists!  This lets States be constructed independently
    /// (modulo parent states being created before children).  Then the 
    /// StateMachine can be created with the full set of available states.</remarks>
    public class State<TEvent, TModel, TParentModel> : State<TEvent, TModel>
        where TModel : IModel
        where TParentModel : IModel
    {
        readonly State<TEvent, TParentModel> _parent;

        readonly List<Action<TEvent, TModel>> _entryActions;
        readonly List<Action<TEvent, TModel>> _exitActions;

        readonly Func<TParentModel, TModel> _entryConversionFunc;
        readonly Func<TModel, TParentModel> _exitConversionFunc;

        public State(
            string label,
            State<TEvent, TParentModel> parent,
            Action<TEvent, TModel> entryAction,
            Action<TEvent, TModel> exitAction,
            Func<TParentModel, TModel> entryConversionFunc = null,
            Func<TModel, TParentModel> exitConversionFunc = null)
            : this(label, parent, new[] { entryAction }, new[] { exitAction }, entryConversionFunc, exitConversionFunc)
        {
        }

        public State(
            string label,
            State<TEvent, TParentModel> parent,
            Action<TEvent, TModel> entryAction,   
            Func<TParentModel, TModel> entryConversionFunc = null,
            Func<TModel, TParentModel> exitConversionFunc = null)
            : this(label, parent, new[] { entryAction }, new Action<TEvent, TModel>[0], entryConversionFunc, exitConversionFunc)
        {
        }

        public State(
            string label,
            State<TEvent, TParentModel> parent,
            Action<TEvent, TModel>[] entryActions, 
            Action<TEvent, TModel>[] exitActions,
            Func<TParentModel, TModel> entryConversionFunc = null,
            Func<TModel, TParentModel> exitConversionFunc = null)
            : base(label)
        {
            _parent = parent;
            _entryActions = new List<Action<TEvent, TModel>>(entryActions);
            _exitActions = new List<Action<TEvent, TModel>>(exitActions);

            _entryConversionFunc = entryConversionFunc;
            _exitConversionFunc = exitConversionFunc;
        }

        public override Option<State<TEvent>> ComputeDestination(Transition<TEvent> transition, TEvent evt, IModel model)
        {
            // Now we know our TModel and we can use it to regain strong typing on the Transition's destination computation.
            Transition<TEvent, TModel> modelTransition = (Transition<TEvent, TModel>)transition;
            return modelTransition.ComputeDestination(evt, (TModel)model);
        }

        public override IModel Enter(TEvent evt, IModel parentModel)
        {
            Spam.Model.WriteLine("State.Enter: state " + Label + ", event: " + evt + ", parentModel: " + parentModel.GetType());
            TModel thisState;
            if (_entryConversionFunc != null) {
                thisState = _entryConversionFunc((TParentModel)parentModel);
            }
            else {
                // had better be the same type!
                thisState = (TModel)parentModel;
            }
            for (int i = 0; i < _entryActions.Count; i++) {
                _entryActions[i](evt, thisState);
            }
            return thisState;
        }

        public override IModel Exit(TEvent evt, IModel model)
        {
            Spam.Model.WriteLine("State.Exit: state " + Label + ", event type: " + evt.GetType() + ", model.GetType(): " + model.GetType());
            TModel thisModel = (TModel)model;
            for (int i = 0; i < _exitActions.Count; i++) {
                _exitActions[i](evt, thisModel);
            }
            TParentModel parentModel;
            if (_exitConversionFunc != null) {
                parentModel = _exitConversionFunc(thisModel);
            }
            else {
                // Terrible, but meets the expectation: these must be dynamically the same type.
                parentModel = (TParentModel)(object)thisModel;
            }
            return parentModel;
        }

        public override State<TEvent> Parent { get { return _parent; } }

        public override IModel GetParentModel(IModel thisModel)
        {
            if (_exitConversionFunc != null) {
                return _exitConversionFunc((TModel)thisModel);
            }
            else {
                return thisModel;
            }
        }
    }
}
