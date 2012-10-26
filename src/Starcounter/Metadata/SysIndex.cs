﻿// ***********************************************************************
// <copyright file="SysIndex.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;

namespace Starcounter.Metadata
{

    /// <summary>
    /// Class SysIndex
    /// </summary>
    public sealed class SysIndex : Entity
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="SysIndex" /> class.
        /// </summary>
        /// <param name="u">The u.</param>
        public SysIndex(Uninitialized u) : base(u) { }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return DbState.ReadString(this, 2); }
        }

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        /// <value>The name of the table.</value>
        public string TableName
        {
            get { return DbState.ReadString(this, 3); }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get { return DbState.ReadString(this, 4); }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="SysIndex" /> is unique.
        /// </summary>
        /// <value><c>true</c> if unique; otherwise, <c>false</c>.</value>
        public bool Unique
        {
            get { return DbState.ReadBoolean(this, 5); }
        }
    }
}
