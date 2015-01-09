// ***********************************************************************
// <copyright file="DatabaseEnumerableType.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

//using System;
//using System.Collections.Generic;

//namespace Sc.Server.Weaver.Schema
//{
///// <summary>
///// Represents a database enumerable type (<see cref="IEnumerable{T}"/>).
///// Enumerables are the 'many' end of a one-to-many relationship.
///// </summary>
//[Serializable]
//public class DatabaseEnumerableType : DatabaseSchemaElement, IDatabaseAttributeType
//{
//    private readonly DatabaseClassRef itemType;

//    private bool relaxTypeCheck;
//    private readonly DatabaseAttribute parent;

//    /// <summary>
//    /// Initializes a new <see cref="DatabaseEnumerableType"/>.
//    /// </summary>
//    /// <param name="itemType">Type of enumerated items, i.e. type of the
//    /// other end of the relationship.</param>
//    public DatabaseEnumerableType(DatabaseAttribute parent, DatabaseClass itemType)
//    {
//        this.itemType = DatabaseClassRef.MakeRef(itemType);
//        this.parent = parent;
//    }

//    /// <summary>
//    /// Gets the type of enumerated items, i.e. the type of the other
//    /// end of the one-to-many relationship.
//    /// </summary>
//    public DatabaseClass ItemType
//    {
//        get
//        {
//            return DatabaseClassRef.Resolve(itemType, this);
//        }
//    }

//    /// <summary>
//    /// Specifies that type compatibility should not be checked.
//    /// </summary>
//    public bool RelaxTypeCheck
//    {
//        get
//        {
//            return relaxTypeCheck;
//        }
//        set
//        {
//            relaxTypeCheck = value;
//        }
//    }

//    public override DatabaseSchema Schema
//    {
//        get
//        {
//            return this.parent.Schema;
//        }
//    }

//    public override string ToString()
//    {
//        return "IEnumerable<" + this.itemType.ToString() + ">";
//    }
//}
//}