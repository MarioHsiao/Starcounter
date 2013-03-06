// ***********************************************************************
// <copyright file="ValueTemplate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Templates;
using System.Collections.Generic;
using System.Diagnostics;
using Starcounter.XSON;
using Starcounter.Advanced;

namespace Starcounter {
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">The primitive system type of this property.</typeparam>
    public abstract class TValue<T> : TValue {
        public Func<Obj, TValue<T>, T, Input<T>> CustomInputEventCreator = null;
        public List<Action<Obj,Input<T>>> CustomInputHandlers = new List<Action<Obj,Input<T>>>();

        private DataValueBinding<T> dataBinding;

        internal DataValueBinding<T> GetBinding(IBindable data) {
            dataBinding = DataBindingFactory.VerifyOrCreateBinding<T>(dataBinding, data.GetType(), Bind);
            return dataBinding;
        }

        /// <summary>
        /// Adds an inputhandler to this property.
        /// </summary>
        /// <param name="createInputEvent"></param>
        /// <param name="handler"></param>
        public void AddHandler(
            Func<Obj, TValue<T>, T, Input<T>> createInputEvent = null,
            Action<Obj, Input<T>> handler = null) {
            this.CustomInputEventCreator = createInputEvent;
            this.CustomInputHandlers.Add(handler);
        }

        internal override object GetBoundValueAsObject(Obj obj) {
            return obj.GetBound<T>(this);
        }

        internal override void SetBoundValueAsObject(Obj obj, object value) {
            obj.SetBound<T>(this, (T)value);
        }
    }

    /// <summary>
    /// Class Property
    /// </summary>
    public abstract class TValue : Template {
        /// <summary>
        /// Gets a value indicating whether this instance has instance value on client.
        /// </summary>
        /// <value><c>true</c> if this instance has instance value on client; otherwise, <c>false</c>.</value>
        public override bool HasInstanceValueOnClient {
            get { return true; }
        }

        /// <summary>
        /// Gets or sets the name of the property this template is bound to.
        /// </summary>
        /// <value>The bind.</value>
        public string Bind { get; set; }

        /// <summary>
        /// Gets a value indicating whether this template is bound.
        /// </summary>
        /// <value><c>true</c> if bound; otherwise, <c>false</c>.</value>
        public bool Bound { get { return (Bind != null); } }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// TODO!
        /// Should be changed to accept IntPtr to rawValue using an int size parameter. This way is not fast enough.
        /// </remarks>
        /// <param name="obj"></param>
        /// <param name="rawValue"></param>
        public abstract void ProcessInput(Obj obj, Byte[] rawValue);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="toTemplate"></param>
        public override void CopyTo(Template toTemplate) {
            base.CopyTo(toTemplate);
            ((TValue)toTemplate).Bind = Bind;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal virtual object GetBoundValueAsObject(Obj obj) {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        internal virtual void SetBoundValueAsObject(Obj obj, object value) {
            throw new NotSupportedException();
        }
    }
}
