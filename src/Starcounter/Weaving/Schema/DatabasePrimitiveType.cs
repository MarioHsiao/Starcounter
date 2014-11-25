// ***********************************************************************
// <copyright file="DatabasePrimitiveType.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;

namespace Sc.Server.Weaver.Schema
{
/// <summary>
/// Encapsulates a <see cref="DatabasePrimitive"/> into a <see cref="IDatabaseAttributeType"/>.
/// </summary>
[Serializable]
public class DatabasePrimitiveType : IDatabaseAttributeType
{
    static Dictionary<DatabasePrimitive, DatabasePrimitiveType> instances = new Dictionary<DatabasePrimitive, DatabasePrimitiveType>(16);

    private DatabasePrimitive primitive;

    /// <summary>
    /// Initializes a new <see cref="DatabasePrimitiveType"/>.
    /// </summary>
    /// <param name="primitive">The encapsulated <see cref="DatabasePrimitive"/>.</param>
    private DatabasePrimitiveType(DatabasePrimitive primitive)
    {
        this.primitive = primitive;
    }

    /// <summary>
    /// Gets a cached instance of <see cref="DatabasePrimitiveType"/>
    /// </summary>
    /// <param name="primitive">The encapsulated <see cref="DatabasePrimitive"/>.</param>
    /// <returns>The instance of <see cref="DatabasePrimitiveType"/> encapsulating <paramref name="primitive"/>.</returns>
    public static DatabasePrimitiveType GetInstance(DatabasePrimitive primitive)
    {
        DatabasePrimitiveType instance;
        if (!instances.TryGetValue(primitive, out instance))
        {
            instance = new DatabasePrimitiveType(primitive);
            instances.Add(primitive, instance);
        }
        return instance;
    }

    /// <summary>
    /// Gets the encapsulated <see cref="DatabasePrimitive"/>.
    /// </summary>
    public DatabasePrimitive Primitive
    {
        get
        {
            return primitive;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return this.primitive.ToString();
    }
}
}