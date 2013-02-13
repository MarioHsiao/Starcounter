// ***********************************************************************
// <copyright file="ActionProperty.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using Starcounter.Templates.Interfaces;
#if CLIENT
namespace Starcounter.Client.Template {
#else
namespace Starcounter.Templates {
#endif

    /// <summary>
    /// Class ActionProperty
    /// </summary>
    public class ActionProperty : Property
#if IAPP
        , IActionTemplate
#endif
    {
        /// <summary>
        /// The custom input event creator
        /// </summary>
        public Func<App, Property, Input> CustomInputEventCreator = null;
     
        /// <summary>
        /// The custom input handlers
        /// </summary>
        public List<Action<App, Input>> CustomInputHandlers = new List<Action<App, Input>>();

        /// <summary>
        /// Gets a value indicating whether this instance has instance value on client.
        /// </summary>
        /// <value><c>true</c> if this instance has instance value on client; otherwise, <c>false</c>.</value>
        public override bool HasInstanceValueOnClient {
            get { return true; }
        }

        /// <summary>
        /// Gets the type of the json.
        /// </summary>
        /// <value>The type of the json.</value>
        public override string JsonType {
            get { return "function"; }
        }

        /// <summary>
        /// Gets or sets the on run.
        /// </summary>
        /// <value>The on run.</value>
        public string OnRun { get; set; }

        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>System.Object.</returns>
        public override object CreateInstance(Container parent) {
            return false;
        }

        /// <summary>
        /// Adds the handler.
        /// </summary>
        /// <param name="createInputEvent">The create input event.</param>
        /// <param name="handler">The handler.</param>
        public void AddHandler(
            Func<Obj, Property, Input> createInputEvent = null,
            Action<Obj, Input> handler = null) {
            this.CustomInputEventCreator = createInputEvent;
            this.CustomInputHandlers.Add(handler);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="rawValue"></param>
        public override void ProcessInput(App app, byte[] rawValue) {
            Input input = null;

            if (CustomInputEventCreator != null)
                input = CustomInputEventCreator.Invoke(app, this);

            if (input != null) {
                foreach (var h in CustomInputHandlers) {
                    h.Invoke(app, input);
                }
            } 
        }
    }
}
