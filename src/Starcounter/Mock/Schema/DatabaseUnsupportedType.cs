// ***********************************************************************
// <copyright file="DatabaseUnsupportedType.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Text;

namespace Sc.Server.Weaver.Schema
{
/// <summary>
/// Represents a type that is not supported by the database,
/// and that was consequently not parsed.
/// </summary>
[Serializable]
public class DatabaseUnsupportedType : IDatabaseAttributeType
{
    private readonly  string typeName;

    /// <summary>
    /// Initializes a new <see cref="DatabaseUnsupportedType" />.
    /// </summary>
    /// <param name="typeName">Name of the type.</param>
    public DatabaseUnsupportedType(string typeName)
    {
        this.typeName = typeName;
    }

    /// <summary>
    /// Ges the name of the unsupported field.
    /// </summary>
    public string TypeName
    {
        get
        {
            return typeName;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return string.Format("Unsupported {0}", this.typeName);
    }

}
}
