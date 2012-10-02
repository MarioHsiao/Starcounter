
using System;

namespace Starcounter.Server.PublicModel {
    
    /// <summary>
    /// Unique identifier of a command.
    /// </summary>
    internal class CommandId : IEquatable<CommandId> {
        
        /// <summary>
        /// The null/empty command identifier.
        /// </summary>
        public static readonly CommandId Null = new CommandId(Guid.Empty);

        /// <summary>
        /// Gets an opaque string holding the value of the
        /// unique command ID.
        /// </summary>
        public string Value {
            get;
            private set;
        }

        private CommandId(Guid guid) {
            this.Value = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Makes a new command identifier.
        /// </summary>
        /// <returns>A new command identifier.</returns>
        public static CommandId MakeNew() {
            return new CommandId(Guid.NewGuid());
        }

        /// <inheritdoc />
        public bool Equals(CommandId other) {
            return other.Value == this.Value;
        }

        /// <inheritdoc />
        public override bool Equals(object obj) {
            return this.Value == ((CommandId)obj).Value;
        }

        /// <inheritdoc />
        public override int GetHashCode() {
            return this.Value == null ? 0 : this.Value.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString() {
            return this.Value;
        }
    }
}