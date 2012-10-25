// ***********************************************************************
// <copyright file="DatabaseKindClass.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Sc.Server.Weaver.Schema
{
/// <summary>
/// Represents a kind class, i.e. a class derived from <b>Concepts.Ring.Something+Kind</b>.
/// </summary>
[Serializable]
public partial class DatabaseKindClass : DatabaseSocietyClass
{
    private DatabaseSocietyClass kindOf;

    /// <summary>
    /// Initializes a <see cref="DatabaseKindClass"/>.
    /// </summary>
    /// <param name="assembly">Assembly to which the class belong.</param>
    /// <param name="name">Class name.</param>
    public DatabaseKindClass(DatabaseAssembly assembly, string name)
    : base(assembly, name)
    {
    }


    /// <summary>
    /// Gets or sets the database class of which the current class is the kind.
    /// </summary>
    public DatabaseSocietyClass KindOf
    {
        get
        {
            return kindOf;
        }
        set
        {
            kindOf = value;
        }
    }
}
}