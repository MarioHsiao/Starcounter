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
namespace Starcounter {
#endif

    /// <summary>
    /// Class Property
    /// </summary>
    /// <typeparam name="TValue">The type of the T value.</typeparam>
    public abstract class Property<TValue> : Property {
        /// <summary>
        /// The custom input event creator
        /// </summary>
        public Func<App, Property<TValue>, TValue, Input<TValue>> CustomInputEventCreator = null;
        /// <summary>
        /// The custom input handlers
        /// </summary>
        public List<Action<App,Input<TValue>>> CustomInputHandlers = new List<Action<App,Input<TValue>>>();


        /// <summary>
        /// Adds the handler.
        /// </summary>
        /// <param name="createInputEvent">The create input event.</param>
        /// <param name="handler">The handler.</param>
        public void AddHandler(
            Func<App, Property<TValue>, TValue, Input<TValue>> createInputEvent = null,
            Action<App, Input<TValue>> handler = null) {
            this.CustomInputEventCreator = createInputEvent;
            this.CustomInputHandlers.Add(handler);
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
                    Console.WriteLine("Setting value after custom handler: " + value);
                    app.SetValue(this, value);
                }
                else
                {
                    Console.WriteLine("Handler cancelled: " + value);
                }
            }
            else
            {
                Console.WriteLine("Setting value after no handler: " + value);
                app.SetValue(this, value);
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
