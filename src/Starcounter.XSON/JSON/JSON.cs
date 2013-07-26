// ***********************************************************************
// <copyright file="Mesasge.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Advanced;
using System;
using System.ComponentModel;
using Starcounter.Templates;
using System.Diagnostics;


namespace Starcounter {

    /// <summary>
    /// A message is a data object that can contain properties. The property can be a simple
    /// value, a nested message or an array of messages.
    /// 
    /// Messages are used to retrieve data from incomming communication calls or to return
    /// content to callers.
    /// 
    /// The REST handling mechanism of Starcounter uses Messages to conveniently allow the
    /// programmer to retreive parameters and data as well as returning data.
    ///
    /// A message can be automatically populated by a bound data object (typically a
    /// database Entity).
    /// </summary>
    /// <remarks>
    /// A message can be serialized to a Json text as part
    /// of a response to a request or deserialized from a Json text as a part of data
    /// in a request.
    /// </remarks>
    public class Json : Obj {
        /// <summary>
        /// As messages are not kept at the server, it does not make sense to interact with
        /// them using "user input".
        /// </summary>
        /// <typeparam name="V">The type of the input value</typeparam>
        /// <param name="template">The property having changed</param>
        /// <param name="value">The new value of the property</param>
        public override void ProcessInput<V>(TValue<V> template, V value) {
            Input<V> input = null;

            if (template.CustomInputEventCreator != null)
                input = template.CustomInputEventCreator.Invoke(this, template, value);

            if (input != null) {
                foreach (var h in template.CustomInputHandlers) {
                    h.Invoke(this, input);
                }
                if (!input.Cancelled) {
                    Debug.WriteLine("Setting value after custom handler: " + input.Value);
                    this.Set<V>((TValue<V>)template, input.Value);
                } else {
                    Debug.WriteLine("Handler cancelled: " + value);
                }
            } else {
                Debug.WriteLine("Setting value after no handler: " + value);
                this.Set<V>((TValue<V>)template, value);
            }
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>Action.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Json Get(TJson property) {
            return Get<Json>(property);
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Set(TJson property, Json value) {
            Set((TObj)property, value);
        }

        public string View { get; set; }
    }

    ///// <summary>
    ///// <see cref="Json"/>
    ///// </summary>
    public class Json<T> : Json {
        public new T Data {
            get { return (T)base.Data; }
            set { base.Data = (IBindable)value; }
        }
    }

}
