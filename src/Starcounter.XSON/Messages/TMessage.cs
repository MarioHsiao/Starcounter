// ***********************************************************************
// <copyright file="TMessage.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Templates {

    /// <summary>
    /// Defines the schema (properties) for a Message object.
    /// </summary>
    /// <remarks>
    /// Schemas for all Obj objects (like Puppets and Messages) are done in the same way.
    /// By creating a tree of TObj, TValue and TArr objects (and their derived classes), you
    /// create a schema to which each instance object (Obj) belongs.
    /// </remarks>
    public class TMessage : TObj {

        /// <summary>
        /// Creates a new Message using the schema defined by this template
        /// </summary>
        /// <param name="parent">The parent for the new message (if any)</param>
        /// <returns>The new message</returns>
        public override object CreateInstance(Container parent) {
            return new Message() { Template = this, Parent = parent };
        }


        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        public override Type InstanceType {
            get {
                if (_AppType == null) {
                    return typeof(Message);
                }
                return _AppType;
            }
            set { _AppType = value; }
        }

    }
}