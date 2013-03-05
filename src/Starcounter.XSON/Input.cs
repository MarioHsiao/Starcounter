// ***********************************************************************
// <copyright file="Input.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Templates;

namespace Starcounter {

    /// <summary>
    /// An event that encapsulates a single incomming update for a specific value in
    /// a Obj. Used as base class for incomming event data in Objs.
    /// </summary>
    /// <typeparam name="TValue">The type of the value that is being updated</typeparam>
    public class Input<TValue> : Input {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public TValue Value { get; set; }

        /// <summary>
        /// Gets the old value (the current value, i.e. before the input is accepted).
        /// </summary>
        /// <value>The old value.</value>
        /// <exception cref="System.NotImplementedException"></exception>
        public TValue OldValue {
            get {
                throw new NotImplementedException();
                //                App.GetValue<TTemplate>(Template);
            }
        }
    }

    /// <summary>
    /// An event that encapsulates a single incomming action trigger for a specific value in
    /// a Obj. Used as base class for incomming trigger events in Objs.
    /// </summary>
    /// <typeparam name="TApp">The type of the Obj.</typeparam>
    /// <typeparam name="TTemplate">The type of the ....TODO</typeparam>
    public class Input<TApp, TTemplate> : Input
        where TApp : Obj
        where TTemplate : Template {

            /// <summary>
            /// The _app
            /// </summary>
        private TApp _app = null;
        /// <summary>
        /// The _template
        /// </summary>
        private TTemplate _template = null;
        /// <summary>
        /// Gets or sets the app.
        /// </summary>
        /// <value>The app.</value>
        public TApp App { get { return _app; } set { _app = value; } }
        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The template.</value>
        public TTemplate Template { get { return _template; } set { _template = value; }  }
    }

    /// <summary>
    /// An event that encapsulates a single incomming update for a specific value in
    /// a Obj. Used as base class for incomming event data in Objs.
    /// </summary>
    /// <typeparam name="TApp">The type of the Obj.</typeparam>
    /// <typeparam name="TTemplate">The type of the ....TODO</typeparam>
    /// <typeparam name="TValue">The type of the value that is being updated</typeparam>
    public class Input<TApp, TTemplate, TValue> : Input<TValue> where TApp : Obj where TTemplate : Template {

        /// <summary>
        /// The _app
        /// </summary>
        private TApp _app = null;
        /// <summary>
        /// The _template
        /// </summary>
        private TTemplate _template = null;
        /// <summary>
        /// Gets or sets the app.
        /// </summary>
        /// <value>The app.</value>
        public TApp App { get { return _app; } set { _app = value; } }
        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The template.</value>
        public TTemplate Template { get { return _template; } set { _template = value; } }

        /// <summary>
        /// Gets the parent.
        /// </summary>
        /// <value>The parent.</value>
        public TApp Parent {
            get {
                return null;
            }
        }

        /// <summary>
        /// Finds the parent.
        /// </summary>
        /// <param name="parentProperty">The parent property.</param>
        /// <returns>App.</returns>
        public Obj FindParent(TContainer parentProperty) {
            return null;
        }

        /// <summary>
        /// Finds the parent.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>``0.</returns>
        public T FindParent<T>() where T:Obj {
            return null;
        }

    }

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
            Cancelled = true;
        }

        /// <summary>
        /// Calls the other handlers.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void CallOtherHandlers() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Input" /> is cancelled.
        /// </summary>
        /// <value><c>true</c> if cancelled; otherwise, <c>false</c>.</value>
        public bool Cancelled {
            get {
                return _cancelled;
            }
            set {
                _cancelled = value;
            }
        }

    }

    /// <summary>
    /// </summary>
    public class SchemaAttribute : System.Attribute {
    }
}
