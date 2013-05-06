// ***********************************************************************
// <copyright file="ServerCommand.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Server.PublicModel.Commands {
    
    /// <summary>
    /// Base class of all server commands, possible to install processors for
    /// and execute via the server command queue / dispatcher.
    /// </summary>
    public abstract class ServerCommand {

        /// <summary>
        /// Initializes a new <see cref="ServerCommand" />.
        /// </summary>
        /// <param name="engine">The engine.</param>
        /// <param name="descriptionFormat">Formatting string of the command description.</param>
        /// <param name="descriptionArgs">Arguments for <paramref name="descriptionFormat" />.</param>
        protected ServerCommand(ServerEngine engine, string descriptionFormat, params object[] descriptionArgs) :
            this(engine, string.Format(descriptionFormat, descriptionArgs)) {
        }

        /// <summary>
        /// Initializes a new <see cref="ServerCommand" />.
        /// </summary>
        /// <param name="engine">The engine.</param>
        /// <param name="description">Human-readable description of the command.</param>
        /// <exception cref="System.ArgumentNullException">description</exception>
        protected ServerCommand(ServerEngine engine, string description) {
            if (string.IsNullOrEmpty(description)) {
                throw new ArgumentNullException("description");
            }
            if (engine == null) {
                throw new ArgumentNullException("engine");
            }

            this.Engine = engine;
            this.Description = description;
        }

        /// <summary>
        /// Gets the <see cref="ServerEngine"/> this command is being
        /// executed on.
        /// </summary>
        protected ServerEngine Engine {
            get;
            private set;
        }

        /// <summary>
        /// Gets a human-readable description of this command.
        /// </summary>
        public string Description {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets a value indicating if the server should enable
        /// wait handle based waiting for the current command.
        /// </summary>
        public bool EnableWaiting {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value that represents the fingerprint of the
        /// principal entity the current command address.
        /// </summary>
        /// <remarks>
        /// The fingerprint is opaque and can be used by specializations
        /// at will.
        /// </remarks>
        public string Fingerprint {
            get;
            set;
        }

        /// <summary>
        /// Infrastructure method invoked by the server engine just before a
        /// command is enqued with the dispatcher (i.e. in the hosts calling
        /// thread). The command gets a chance to either fill in defaults
        /// and/or validate it's values and raise an exception if they violate
        /// constraints.
        /// </summary>
        internal virtual void GetReadyToEnqueue() {
            // Default implementation does nothing.
        }
    }
}