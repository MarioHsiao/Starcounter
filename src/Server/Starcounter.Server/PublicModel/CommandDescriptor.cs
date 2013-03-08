// ***********************************************************************
// <copyright file="CommandDescriptor.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Xml.Serialization;

namespace Starcounter.Server.PublicModel {
    
    /// <summary>
    /// Describes a command and provides possibly information about it's
    /// underlying tasks.
    /// </summary>
    public sealed class CommandDescriptor {
        /// <summary>
        /// The empty
        /// </summary>
        public static readonly CommandDescriptor Empty = new CommandDescriptor();

        /// <summary>
        /// Gets or sets a description that describes - on the type level -
        /// what the command represented by this information object does.
        /// </summary>
        /// <example>"Starts a database"</example>
        public string CommandDescription {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets an identity of the type of command represented.
        /// This type identity can be used to connect executing command
        /// information with a metdata construct (i.e. an instance of
        /// this class) representing the command type.
        /// </summary>
        public int CommandToken {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a set of tasks describing in more detail what
        /// the command represented by <see cref="CommandDescriptor"/>
        /// instances does.
        /// </summary>
        public TaskInfo[] Tasks {
            get;
            set;
        }

        /// <summary>
        /// Finds a <see cref="TaskInfo"/> by it's identity.
        /// </summary>
        /// <param name="id">
        /// The identity of the task information to retreive.
        /// </param>
        /// <returns>
        /// A <see cref="TaskInfo"/> representing the task identified
        /// by <paramref name="id"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when
        /// no <see cref="TaskInfo"/> with the given <paramref name="id"/>
        /// could be found.
        /// </exception>
        public TaskInfo this[int id] {
            get {
                foreach (TaskInfo taskInfo in this.Tasks) {
                    if (taskInfo.ID == id) {
                        return taskInfo;
                    }
                }
                throw new ArgumentOutOfRangeException("id");
            }
        }
    }
}