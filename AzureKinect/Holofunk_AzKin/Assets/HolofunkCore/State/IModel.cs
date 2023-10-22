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
        private IModel optParent;
        private Action<TModel> updateAction;

        public BaseModel(IModel optParent, Action<TModel> updateAction)
        {
            this.optParent = optParent;
            this.updateAction = updateAction;
        }

        public void ModelUpdate()
        {
            updateAction((TModel)this);
        }

        public IModel Parent => optParent;        
    }
}
