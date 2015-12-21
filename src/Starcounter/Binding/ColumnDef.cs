// ***********************************************************************
// <copyright file="ColumnDef.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Binding
{

    /// <summary>
    /// Class ColumnDef
    /// </summary>
    public sealed class ColumnDef
    {

        /// <summary>
        /// The name
        /// </summary>
        public string Name;
        /// <summary>
        /// The type
        /// </summary>
        public byte Type;
        /// <summary>
        /// The is nullable
        /// </summary>
        public bool IsNullable;
        /// <summary>
        /// The is inherited
        /// </summary>
        public bool IsInherited;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnDef" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="isNullable">if set to <c>true</c> [is nullable].</param>
        /// <param name="isInherited">if set to <c>true</c> [is inherited].</param>
        public ColumnDef(string name, byte type, bool isNullable, bool isInherited)
        {
            Name = name;
            Type = type;
            IsNullable = isNullable;
            IsInherited = isInherited;
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>ColumnDef.</returns>
        public ColumnDef Clone()
        {
            return new ColumnDef(Name, Type, IsNullable, IsInherited);
        }

        /// <summary>
        /// Equalses the specified column def.
        /// </summary>
        /// <param name="columnDef">The column def.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool Equals(ColumnDef columnDef)
        {
            return
                Name.Equals(columnDef.Name, StringComparison.InvariantCultureIgnoreCase) &&
                Type == columnDef.Type &&
                IsNullable == columnDef.IsNullable &&
                IsInherited == columnDef.IsInherited
                ;
        }
    }
}
