// ***********************************************************************
// <copyright file="PropertyDef.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Sc.Server.Weaver.Schema;
namespace Starcounter.Binding
{

    /// <summary>
    /// Class PropertyDef
    /// </summary>
    public sealed class PropertyDef
    {

        /// <summary>
        /// The name
        /// </summary>
        public string Name;
        /// <summary>
        /// The type
        /// </summary>
        public DbTypeCode Type;
        /// <summary>
        /// The is nullable
        /// </summary>
        public bool IsNullable;

        /// <summary>
        /// The target type name
        /// </summary>
        public string TargetTypeName;

        /// <summary>
        /// Name of column if representing a database column. NULL otherwise.
        /// </summary>
        public string ColumnName;

        /// <summary>
        /// Index of column if representing a database column. -1 otherwise.
        /// </summary>
        public int ColumnIndex;

        /// <summary>
        /// Flags that gives the current property extended semantics if applied.
        /// </summary>
        internal int SpecialFlags { get; set; }

        /// <summary>
        /// Gets a value indicating of the current property is to be
        /// considered a type reference, part of Starcounter dynamic
        /// types.
        /// </summary>
        public bool IsTypeReference {
            get {
                return (SpecialFlags & DatabaseAttributeFlags.TypeReference) > 0;
            }
        }

        /// <summary>
        /// Gets a value indicating if the current property is to be
        /// considered a reference to a base type, part of Starcounter
        /// dynamic types.
        /// </summary>
        public bool IsInheritsReference {
            get {
                return (SpecialFlags & DatabaseAttributeFlags.IneritsReference) > 0;
            }
        }

        /// <summary>
        /// Gets a value indicating if the current property is to be
        /// considered holding the type name of a class part of the new
        /// dynamic types domain.
        /// </summary>
        public bool IsTypeName {
            get {
                return (SpecialFlags & DatabaseAttributeFlags.TypeName) > 0;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyDef" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        public PropertyDef(string name, DbTypeCode type) : this(name, type, false, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyDef" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="targetTypeName">Name of the target type.</param>
        public PropertyDef(string name, DbTypeCode type, string targetTypeName) : this(name, type, false, targetTypeName) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyDef" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="isNullable">if set to <c>true</c> [is nullable].</param>
        public PropertyDef(string name, DbTypeCode type, bool isNullable) : this(name, type, isNullable, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyDef" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="isNullable">if set to <c>true</c> [is nullable].</param>
        /// <param name="targetTypeName">Name of the target type.</param>
        public PropertyDef(string name, DbTypeCode type, bool isNullable, string targetTypeName)
        {
            Name = name;
            Type = type;
            IsNullable = isNullable;
            TargetTypeName = targetTypeName;
            //ColumnName = null;
            ColumnIndex = -1;
            SpecialFlags = 0;
        }
    }
}
