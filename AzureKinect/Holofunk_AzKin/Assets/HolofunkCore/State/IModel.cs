// Copyright by Rob Jellinghaus. All rights reserved.

using System;

namespace Holofunk.StateMachines
{
    /// <summary>
    /// Marker interface for objects which can be manipulated by state machines.
    /// </summary>
    /// <remarks>
    /// In practice we simply downcast these objects, relying on safe downcasting to catch errors (which are
    /// programmer fail-fast bugs if they occur).
    /// </remarks>
    public interface IModel
    {
        /// <summary>
        /// The parent model; null if none.
        /// </summary>
        public IModel Parent { get; }

        /// <summary>
        /// Update this Model for this frame.
        /// </summary>
        public void ModelUpdate();
    }

    public abstract class BaseModel<TModel> : IModel
        where TModel : BaseModel<TModel>
    {
        private Action<TModel> updateAction;

        public BaseModel(Action<TModel> updateAction)
        {
            this.updateAction = updateAction;
        }

        public void ModelUpdate()
        {
            updateAction((TModel)this);
        }

        public abstract IModel Parent { get; }
    }

    public class RootModel<TModel> : BaseModel<TModel>
        where TModel : RootModel<TModel>
    {
        public override IModel Parent => null;

        public RootModel(Action<TModel> updateAction) : base(updateAction)
        {}            
    }

    internal class ChildModel<TModel, TParentModel> : BaseModel<TModel>
        where TModel : BaseModel<TModel>
        where TParentModel : BaseModel<TParentModel>
    {
        public override IModel Parent => parent;
        private TParentModel parent;
        protected ChildModel(TParentModel parent, Action<TModel> updateAction) : base(updateAction)
        {
            this.parent = parent;
        }
    }
}
