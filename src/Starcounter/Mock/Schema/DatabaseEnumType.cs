// ***********************************************************************
// <copyright file="DatabaseEnumType.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;

namespace Sc.Server.Weaver.Schema
{
/// <summary>
/// Represents an enumeration type (<b>enum</b> in C#).
/// </summary>
[Serializable]
public class DatabaseEnumType : IDatabaseAttributeType
{
    static Dictionary<string, DatabaseEnumType> instances = new Dictionary<string, DatabaseEnumType>();
    private DatabasePrimitive underlyingType;
    private string name;
    //        private readonly IList<DatabaseEnumValue> values = new List<DatabaseEnumValue>();

    /// <summary>
    /// Initializes a new <see cref="DatabaseEnumType"/>.
    /// </summary>
    /// <param name="name">Name of the enumeration.</param>
    /// <param name="underlyingType">Underlying type of the enumeration (typically <see cref="Int32"/>).</param>
    private DatabaseEnumType(string name, DatabasePrimitive underlyingType)
    {
        this.name = name;
        this.underlyingType = underlyingType;
    }

    /// <summary>
    /// Gets a cached instance of <see cref="DatabaseEnumType"/>.
    /// </summary>
    /// <param name="name">Name of the enumeration.</param>
    /// <param name="underlyingType">Underlying type of the enumeration (typically <see cref="Int32"/>).</param>
    /// <returns>The instance <see cref="DatabaseEnumType"/> representing this enumeration.</returns>
    public static DatabaseEnumType GetInstance(string name, DatabasePrimitive underlyingType)
    {
        DatabaseEnumType instance;
        if (!instances.TryGetValue(name, out instance))
        {
            instance = new DatabaseEnumType(name, underlyingType);
            instances.Add(name, instance);
        }
        else
        {
            // Verify that the underlying type is the same.
            if (instance.underlyingType != underlyingType)
            {
                throw new ArgumentException("Unexpected underlying type.");
            }
        }
        return instance;
    }

    /// <summary>
    /// Gets the name of the current enumeration.
    /// </summary>
    public string Name
    {
        get
        {
            return name;
        }
    }

    /// <summary>
    /// Gets the underlying type of the
    /// </summary>
    public DatabasePrimitive UnderlyingType
    {
        get
        {
            return underlyingType;
        }
    }

    //        public IList<DatabaseEnumValue> Values
    //        {
    //            get { return this.values; }
    //        }


    /// <summary>
    /// Returns a <see cref="System.String" /> that represents this instance.
    /// </summary>
    /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
    public override string ToString()
    {
        return string.Format("Enum {0} (underlying {1})", this.name, this.underlyingType);
    }
}
}