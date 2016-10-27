// ***********************************************************************
// <copyright file="IMessageHandler.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using PostSharp.Extensibility;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// Handles messages coming from the weaver. To be implementing by the host server.
    /// </summary>
    /// <remarks>Implementations of this interface should be derived from <see cref="MarshalByRefObject" />.</remarks>
    public interface IMessageHandler {
        /// <summary>
        /// Method called when a message is emitted in the weaver.
        /// </summary>
        /// <param name="message">Details of the message.</param>
        void OnMessage(Message message);
    }
}