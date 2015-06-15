using System;
using System.Collections.Generic;

namespace Starcounter {
    /// <summary>
    /// A key identifying a triggering hook type, including its
    /// identity and the operation being hooked (e.g. Insert);
    /// </summary>
    /// <example>
    /// Here, the <see cref="HookKey"/> will contain a TypeID
    /// that is the table ID of the "Foo" table (e.g 25), and
    /// the TypeOfHook will have a value equal to the 
    /// <see cref="HookType.CommitInsert"/> constant.
    /// <code>
    /// // Register an insertion hook on class/table Foo
    /// Hook{Foo}.OnInsert(f => {...});
    /// </code>
    /// </example>
    internal sealed class HookKey {
        class DefaultEqualityComparer : IEqualityComparer<HookKey> {

            public bool Equals(HookKey x, HookKey y) {
                if (x == null) return y == null;
                else if (y == null) return false;
                return x.TypeId == y.TypeId && x.TypeOfHook == y.TypeOfHook;
            }

            public int GetHashCode(HookKey obj) {
                return string.Format("{0}-{1}", obj.TypeId, obj.TypeOfHook).GetHashCode();
            }
        }

        /// <summary>
        /// Gets the type id of this hook, normally a table id.
        /// </summary>
        public uint TypeId { get; private set; }
        
        /// <summary>
        /// Gets the type of hook the current key represent.
        /// <see cref="HookType"/>.
        /// </summary>
        public uint TypeOfHook { get; private set; }

        /// <summary>
        /// Gets an <see cref="IEqualityComparer<HookKey>"/> that can be used
        /// to compare <see cref="HookKey"/> instances for equality.
        /// </summary>
        public static IEqualityComparer<HookKey> EqualityComparer {
            get {
                return new HookKey.DefaultEqualityComparer();
            }
        }

        private HookKey() {
        }

        /// <summary>
        /// Creates a key based on a table id.
        /// </summary>
        /// <param name="tableId">The table id that identifies the type
        /// of the key.</param>
        /// <param name="hookType">The type of hook the key are to
        /// represent. Valid values are from <see cref="HookType"/>.
        /// </param>
        /// <param name="keyToReuse">Optional key to reuse. If not given,
        /// a new key is instantiated.</param>
        /// <returns>A new <see cref="HookKey"/> based on the given values.
        /// </returns>
        public static HookKey FromTable(ushort tableId, uint hookType, HookKey keyToReuse = null) {
            // TODO: Change to INVALID_TABLE_ID constant.
            if (tableId == ushort.MaxValue) throw new ArgumentOutOfRangeException();
            var key = keyToReuse ?? new HookKey();
            key.TypeId = tableId;
            key.TypeOfHook = hookType;
            return key;
        }
    }
}
