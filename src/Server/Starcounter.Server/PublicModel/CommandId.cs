
using System;

namespace Starcounter.Server.PublicModel {
    
    /// <summary>
    /// Unique identifier of a command.
    /// </summary>
    public sealed class CommandId : IEquatable<CommandId> {
        
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

        /// <summary>
        /// Reconstructs a <see cref="CommandId"/> based on the
        /// given value.
        /// </summary>
        /// <param name="value">The value of a previously created
        /// command.</param>
        /// <returns>A new <see cref="CommandId"/> based on the
        /// given value.</returns>
        public static CommandId Parse(string value) {
            return new CommandId(Guid.Parse(value));
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