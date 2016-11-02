// ***********************************************************************
// <copyright file="Input.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Templates;

namespace Starcounter {
    /// <summary>
    /// Base class for Input events. Input events are events triggered by the client
    /// and catched on the server when Objs receive input from the end user.
    /// </summary>
    public class Input {
        /// <summary>
        /// </summary>
        private bool _cancelled = false;

        /// <summary>
        /// Cancels this instance.
        /// </summary>
        public void Cancel() {
            _cancelled = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Input" /> is cancelled.
        /// </summary>
        /// <value><c>true</c> if cancelled; otherwise, <c>false</c>.</value>
        public bool Cancelled { get { return _cancelled; } set { _cancelled = value; } }

        /// <summary>
        /// Calls the base handler (if any).
        /// </summary>
        public virtual void Base() { }
    }

    /// <summary>
    /// An event that encapsulates a single incomming update for a specific value in
    /// a Obj. Used as base class for incomming event data in Objs.
    /// </summary>
    /// <typeparam name="TValue">The type of the value that is being updated</typeparam>
    public class Input<TValue> : Input {
        private TValue v;
  
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public TValue Value {
            get {
                return v;
            }
            set {
                v = value;
                ValueChanged = true;
            }
        }

        /// <summary>
        /// Gets the old value (the current value, i.e. before the input is accepted).
        /// </summary>
        /// <value>The old value.</value>
        /// <exception cref="System.NotImplementedException"></exception>
        public TValue OldValue { get; internal set; }

        /// <summary>
        /// Returns true if the value have been changed in the inputhandler.
        /// </summary>
        /// <remarks>
        /// This property will be true even if the value set is the same as before.
        /// </remarks>
        public bool ValueChanged { get; internal set; }
    }
    
    /// <summary>
    /// An event that encapsulates a single incomming update for a specific value in
    /// a Obj. Used as base class for incomming event data in Objs.
    /// </summary>
    /// <typeparam name="TApp">The type of the Obj.</typeparam>
    /// <typeparam name="TTemplate">The type of the ....TODO</typeparam>
    /// <typeparam name="TValue">The type of the value that is being updated</typeparam>
    public class Input<TApp, TTemplate, TValue> : Input<TValue> where TApp : Json 
                                                                where TTemplate : Property<TValue> {
        private TApp _app;
        private TTemplate _template;
        
        public TApp App { get { return _app; } set { _app = value; } }
        public TTemplate Template { get { return _template; } set { _template = value; } }

        public override void Base() {
            if (_template != null) {
                var baseTemplate = _template.BasedOn as TTemplate;
                if (baseTemplate != null)
                    baseTemplate.ProcessInput(App, this);
            }
        }
    }
}
