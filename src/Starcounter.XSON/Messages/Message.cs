// ***********************************************************************
// <copyright file="Mesasge.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Advanced;
using System;


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
    /// <typeparam name="T"></typeparam>
    public class Message<T> : Obj<T> where T : IBindable {

        /// <summary>
        /// As messages are not kept at the server, it does not make sense to interact with
        /// them using "user input".
        /// </summary>
        /// <typeparam name="V">The type of the input value</typeparam>
        /// <param name="template">The property having changed</param>
        /// <param name="value">The new value of the property</param>
        public override void ProcessInput<V>(TValue<V> template, V value) {
            // TODO! SCERR????
            throw new Exception("You should not send input to a Message object. Use Puppets instead.");
        }
    }

    /// <summary>
    /// <see cref="Message"/>
    /// </summary>
    public class Message : Message<NullData> {
    }

    /// <summary>
    /// 
    /// </summary>
    public class NullData : IBindable {
        /// <summary>
        /// 
        /// </summary>
        public UInt64 UniqueID { get { return 0; } }
    }

}
