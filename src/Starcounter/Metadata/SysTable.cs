// ***********************************************************************
// <copyright file="SysTable.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;

namespace Starcounter.Metadata
{

    /// <summary>
    /// Class SysTable
    /// </summary>
    public sealed class SysTable : Entity
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="SysTable" /> class.
        /// </summary>
        /// <param name="u">The u.</param>
        public SysTable(Uninitialized u) : base(u) { }

        /// <summary>
        /// </summary>
        public ulong TableId {
            get { return DbState.ReadUInt64(0,0, 0); }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return DbState.ReadString(0,0, 1); }
        }

        /// <summary>
        /// Gets the name of the base.
        /// </summary>
        /// <value>The name of the base.</value>
        public string BaseName
        {
            get { return DbState.ReadString(0,0, 2); }
        }
    }
}
