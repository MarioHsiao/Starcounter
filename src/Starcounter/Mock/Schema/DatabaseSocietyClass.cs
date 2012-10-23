// ***********************************************************************
// <copyright file="DatabaseSocietyClass.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Sc.Server.Weaver.Schema
{
/// <summary>
/// Represents a database society object class, i.e. a class derived
/// from <b>Concepts.Ring1.Something</b>.
/// </summary>
[Serializable]
public class DatabaseSocietyClass : DatabaseEntityClass
{
    private DatabaseKindClass kindClass;

    /// <summary>
    /// Initializes a new <see cref="DatabaseKindClass"/>.
    /// </summary>
    /// <param name="assembly">Assembly to which the class belong.</param>
    /// <param name="name">Class name.</param>
    public DatabaseSocietyClass(DatabaseAssembly assembly, string name)
    : base(assembly, name)
    {
    }

    /// <summary>
    /// Gets or sets the kind class <i>declared</i> by the current society object.
    /// </summary>
    public DatabaseKindClass KindClass
    {
        get
        {
            return kindClass;
        }
        set
        {
            kindClass = value;
        }
    }

    /// <summary>
    /// Gets the the kind class <i>declared or inherited</i> by the curent object.
    /// </summary>
    public DatabaseKindClass InheritedKindClass
    {
        get
        {
            if (this.kindClass != null)
            {
                return this.kindClass;
            }
            else
            {
                DatabaseSocietyClass parentDatabaseSocietyClass = this.BaseClass as DatabaseSocietyClass;
                if (parentDatabaseSocietyClass != null)
                {
                    return parentDatabaseSocietyClass.InheritedKindClass;
                }
                else
                {
                    return null;
                }
            }
        }
    }

}
}