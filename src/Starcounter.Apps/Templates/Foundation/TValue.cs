// ***********************************************************************
// <copyright file="ValueTemplate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Templates.Interfaces;
using Starcounter.Templates;
using System.Collections.Generic;
using Starcounter.Templates.DataBinding;
using System.Diagnostics;

namespace Starcounter {
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">The primitive system type of this property.</typeparam>
    public abstract class TValue<T> : TValue {
        internal Func<Obj, TValue<T>, T, Input<T>> CustomInputEventCreator = null;
        internal List<Action<Obj,Input<T>>> CustomInputHandlers = new List<Action<Obj,Input<T>>>();

        private DataBinding<T> dataBinding;
        
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataGetter"></param>
        public void AddDataBinding(Func<Obj, T> dataGetter) {
            dataBinding = new DataBinding<T>(dataGetter);
            Bound = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataGetter"></param>
        /// <param name="dataSetter"></param>
        public void AddDataBinding(Func<Obj, T> dataGetter, Action<Obj, T> dataSetter) {
            dataBinding = new DataBinding<T>(dataGetter, dataSetter);
            Bound = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="value"></param>
        public void SetBoundValue(Obj app, T value) {
            dataBinding.SetValue(app, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public T GetBoundValue(Obj app) {
            return dataBinding.GetValue(app);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override object GetBoundValueAsObject(Obj obj) {
            return dataBinding.GetValue(obj);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public override void SetBoundValueAsObject(Obj obj, object value) {
            dataBinding.SetValue(obj, (T)value);
        }
    }


    /// <summary>
    /// Class Property
    /// </summary>
    public abstract class TValue : Template
    {
        /// <summary>
        /// Gets a value indicating whether this instance has instance value on client.
        /// </summary>
        /// <value><c>true</c> if this instance has instance value on client; otherwise, <c>false</c>.</value>
        public override bool HasInstanceValueOnClient {
            get { return true; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// TODO!
        /// Should be changed to accept IntPtr to rawValue using an int size parameter. This way is not fast enough.
        /// </remarks>
        /// <param name="obj"></param>
        /// <param name="rawValue"></param>
        internal abstract void ProcessInput(Obj obj, Byte[] rawValue);
    }
}
