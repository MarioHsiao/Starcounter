// ***********************************************************************
// <copyright file="ValueTemplate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Templates.Interfaces;
#if CLIENT
using Starcounter.Client;
namespace Starcounter.Client.Template {
#else
using Starcounter.Templates;
using System.Collections.Generic;
using Starcounter.Templates.DataBinding;
namespace Starcounter {
#endif
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TValue">The primitive system type of this property.</typeparam>
    public abstract class Property<TValue> : Property {
        internal Func<App, Property<TValue>, TValue, Input<TValue>> CustomInputEventCreator = null;
        internal List<Action<App,Input<TValue>>> CustomInputHandlers = new List<Action<App,Input<TValue>>>();

        private DataBinding<TValue> dataBinding;
        
        /// <summary>
        /// Adds an inputhandler to this property.
        /// </summary>
        /// <param name="createInputEvent"></param>
        /// <param name="handler"></param>
        public void AddHandler(
            Func<App, Property<TValue>, TValue, Input<TValue>> createInputEvent = null,
            Action<App, Input<TValue>> handler = null) {
            this.CustomInputEventCreator = createInputEvent;
            this.CustomInputHandlers.Add(handler);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataGetter"></param>
        public void AddDataBinding(Func<App, TValue> dataGetter) {
            dataBinding = new DataBinding<TValue>(dataGetter);
            Bound = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataGetter"></param>
        /// <param name="dataSetter"></param>
        public void AddDataBinding(Func<App, TValue> dataGetter, Action<App, TValue> dataSetter) {
            dataBinding = new DataBinding<TValue>(dataGetter, dataSetter);
            Bound = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="value"></param>
        public void SetBoundValue(App app, TValue value) {
            dataBinding.SetValue(app, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public TValue GetBoundValue(App app) {
            return dataBinding.GetValue(app);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public override object GetBoundValueAsObject(IApp app) {
            return dataBinding.GetValue((App)app);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="value"></param>
        public override void SetBoundValueAsObject(IApp app, object value) {
            dataBinding.SetValue((App)app, (TValue)value);
        }

        /// <summary>
        /// Processes the input.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="value">The value.</param>
        public void ProcessInput(App app, TValue value)
        {
            Input<TValue> input = null;

            if (CustomInputEventCreator != null)
                input = CustomInputEventCreator.Invoke(app, this, value);

            if (input != null)
            {
                foreach (var h in CustomInputHandlers)
                {
                    h.Invoke(app, input);
                }
                if (!input.Cancelled)
                {
                    Console.WriteLine("Setting value after custom handler: " + input.Value);
                    app.SetValue((Property<TValue>)this, input.Value);
                }
                else
                {
                    Console.WriteLine("Handler cancelled: " + value);
                }
            }
            else
            {
                Console.WriteLine("Setting value after no handler: " + value);
                app.SetValue((Property<TValue>)this, value);
            }
        }
    }


    /// <summary>
    /// Class Property
    /// </summary>
    public abstract class Property : Template
#if IAPP
        , IValueTemplate
#endif
    {
        /// <summary>
        /// Gets a value indicating whether this instance has instance value on client.
        /// </summary>
        /// <value><c>true</c> if this instance has instance value on client; otherwise, <c>false</c>.</value>
        public override bool HasInstanceValueOnClient {
            get { return true; }
        }

        /// <summary>
        /// Processes the input.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="rawValue">The raw value.</param>
        public abstract void ProcessInput(App app, Byte[] rawValue);
    }
}
