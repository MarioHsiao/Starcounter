
using System;

namespace Starcounter.Server.PublicModel.Commands {
    
    /// <summary>
    /// Base class of all server commands, possible to install processors for
    /// and execute via the server command queue / dispatcher.
    /// </summary>
    public abstract class ServerCommand {
        /// <summary>
        /// Initializes a new <see cref="ServerCommand"/>.
        /// </summary>
        /// <param name="descriptionFormat">Formatting string of the command description.</param>
        /// <param name="descriptionArgs">Arguments for <paramref name="descriptionFormat"/>.</param>
        protected ServerCommand(string descriptionFormat, params object[] descriptionArgs) :
            this(string.Format(descriptionFormat, descriptionArgs)) {
        }


        /// <summary>
        /// Initializes a new <see cref="ServerCommand"/>.
        /// </summary>
        /// <param name="description">Human-readable description of the command.</param>
        protected ServerCommand(string description) {
            if (string.IsNullOrEmpty(description)) {
                throw new ArgumentNullException("description");
            }
            this.Description = description;
        }

        /// <summary>
        /// Gets a human-readable description of this command.
        /// </summary>
        public string Description {
            get;
            private set;
        }
    }
}