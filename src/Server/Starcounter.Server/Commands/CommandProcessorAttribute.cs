// ***********************************************************************
// <copyright file="CommandProcessorAttribute.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Server.Commands {
    /// <summary>
    /// Custom attribute that, when applied on a class derived from <see cref="CommandProcessor"/>,
    /// specifies which type of command the current <see cref="CommandProcessor"/> is able
    /// to process.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class CommandProcessorAttribute : Attribute {
        private readonly Type commandType;

        /// <summary>
        /// Initializes a new <see cref="CommandProcessorAttribute"/>.
        /// </summary>
        /// <param name="commandType">A type derived from <see cref="Starcounter.Server.PublicModel.Commands.ServerCommand"/>.</param>
        public CommandProcessorAttribute(Type commandType) {
            this.commandType = commandType;
        }

        /// <summary>
        /// Gets the type of command that the related <see cref="CommandProcessor"/> can execute.
        /// </summary>
        public Type CommandType {
            get {
                return commandType;
            }
        }

        /// <summary>
        /// Gets or sets a value if the processor to which this attribute is
        /// applied should be considered an internal command processor, meaning
        /// that it is never represented in the public model. The execution of
        /// internal commands can hence never be seen or tracked by management
        /// clients.
        /// </summary>
        public Boolean IsInternal {
            get;
            set;
        }
    }
}