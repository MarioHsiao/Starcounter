// ***********************************************************************
// <copyright file="DatabaseExtensionClass.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Runtime.Serialization;

namespace Sc.Server.Weaver.Schema
{
/// <summary>
/// Represents a database extension class (i.e. a class derived from <b>Extension&lt;T&gt;</b>).
/// </summary>
[Serializable]
public class DatabaseExtensionClass : DatabaseClass
{
    private DatabaseClassRef extends;

    /// <summary>
    /// Initializes a new <see cref="DatabaseExtensionClass"/>.
    /// </summary>
    /// <param name="assembly">Assembly to which the class belong.</param>
    /// <param name="name">Full name of the class.</param>
    public DatabaseExtensionClass(DatabaseAssembly assembly, string name)
    : base(assembly, name)
    {
    }

    internal override void  OnSchemaComplete()
    {
        ((DatabaseEntityClass) DatabaseClassRef.Resolve(this.extends, this)).ExtensionClasses.Add(this);
    }

    /// <summary>
    /// Gets of sets the database class that this extension extends.
    /// </summary>
    public DatabaseClass Extends
    {
        get
        {
            return DatabaseClassRef.Resolve(this.extends, this);
        }
        set
        {
            this.extends = DatabaseClassRef.MakeRef(value);
        }
    }
}
}