using System;
using System.Collections.Generic;

namespace Starcounter {
    /// <summary>
    /// A key identifying a triggering hook type, including its
    /// identity and the operation being hooked (e.g. Insert);
    /// </summary>
    internal sealed class HookKey {
        class DefaultEqualityComparer : IEqualityComparer<HookKey> {

            public bool Equals(HookKey x, HookKey y) {
                if (x == null) return y == null;
                else if (y == null) return false;
                return x.TypeId == y.TypeId && x.Operation == y.Operation;
            }

            public int GetHashCode(HookKey obj) {
                return string.Format("{0}-{1}", obj.TypeId, obj.Operation).GetHashCode();
            }
        }

        /// <summary>
        /// Gets the type id of this hook, normally a table id.
        /// </summary>
        public readonly uint TypeId;
        
        /// <summary>
        /// Gets the operation
        /// </summary>
        public readonly uint Operation;

        /// <summary>
        /// Gets an <see cref="IEqualityComparer<HookKey>"/> that can be used
        /// to compare <see cref="HookKey"/> instances for equality.
        /// </summary>
        public static IEqualityComparer<HookKey> EqualityComparer {
            get {
                return new HookKey.DefaultEqualityComparer();
            }
        }

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
