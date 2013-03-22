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
    public sealed class SysColumn
    {
        public ObjectRef ThisRef;

        /// <summary>
        /// </summary>
        public ulong TableId {
            get { return DbState.ReadUInt64(this.ThisRef.ObjectID, ThisRef.ETI, 0); }
        }

        /// <summary>
        /// </summary>
        public ulong Index {
            get { return DbState.ReadUInt64(ThisRef.ObjectID, ThisRef.ETI, 1); }
        }

        /// <summary>
        /// </summary>
        public string Name {
            get { return DbState.ReadString(ThisRef.ObjectID, ThisRef.ETI, 2); }
        }
    }
}
