// ***********************************************************************
// <copyright file="SysTable.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;

namespace Starcounter.Metadata
{

    /// <summary>
    /// </summary>
    public sealed class SysColumn : Entity
    {

        /// <summary>
        /// </summary>
        public SysColumn(Uninitialized u) : base(u) { }

        /// <summary>
        /// </summary>
        public ulong TableId {
            get { return DbState.ReadUInt64(this, 0); }
        }

        /// <summary>
        /// </summary>
        public ulong Index {
            get { return DbState.ReadUInt64(this, 1); }
        }

        /// <summary>
        /// </summary>
        public string Name {
            get { return DbState.ReadStringFromEntity(this, 2); }
        }
    }
}
