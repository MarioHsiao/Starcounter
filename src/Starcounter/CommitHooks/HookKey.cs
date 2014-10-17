using System;

namespace Starcounter {
    /// <summary>
    /// A key identifying a triggering hook type, including its
    /// identity and the operation being hooked (e.g. Insert);
    /// </summary>
    internal sealed class HookKey {
        /// <summary>
        /// Gets the type id of this hook, normally a table id.
        /// </summary>
        public readonly uint TypeId;
        /// <summary>
        /// Gets the operation
        /// </summary>
        public readonly uint Operation;

        /// <summary>
        /// Initializes
        /// </summary>
        /// <param name="typeId"></param>
        /// <param name="operation"></param>
        private HookKey(uint typeId, uint operation) {
            TypeId = typeId;
            Operation = operation;
        }

        /// <summary>
        /// Creates a key based on a table id.
        /// </summary>
        /// <param name="tableId">The table id that identifies the type
        /// of the key.</param>
        /// <param name="operation">The operation the key identifies.</param>
        /// <returns>A new <see cref="HookKey"/> based on the given values.
        /// </returns>
        public static HookKey FromTable(ushort tableId, uint operation) {
            // TODO: Change to INVALID_TABLE_ID constant.
            if (tableId == ushort.MaxValue) throw new ArgumentOutOfRangeException();
            return new HookKey(tableId, operation);
        }
    }
}
