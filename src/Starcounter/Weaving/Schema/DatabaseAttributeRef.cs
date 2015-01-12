// ***********************************************************************
// <copyright file="DatabaseAttributeRef.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;

namespace Sc.Server.Weaver.Schema
{
/// <summary>
/// Serializable reference to a <see cref="DatabaseAttribute"/>.
/// </summary>
/// <remarks>
/// The purpose of this object is to store a cross-assembly reference that is serializable
/// without hard-linking to the assembly, so that assemblies can be serialized separately.
/// References are resolved transparently and are never exposed to consumer code.
/// </remarks>
[Serializable]
internal sealed class DatabaseAttributeRef
{
    // Cached referenced DatabaseAttribute (null if the reference has not yet been resolved).
    [NonSerialized] private DatabaseAttribute resolvedTarget;

    private readonly string className;
    private readonly string assemblyName;
    private readonly string attributeName;

    /// <summary>
    /// Initialize a new <see cref="DatabaseAttributeRef"/>.
    /// </summary>
    /// <param name="target">Referenced <see cref="DatabaseAttribute"/>.</param>
    private DatabaseAttributeRef(DatabaseAttribute target)
    {
        this.resolvedTarget = target;
        this.attributeName = target.Name;
        this.className = target.DeclaringClass.Name;
        this.assemblyName = target.DeclaringClass.Assembly.Name;
    }

    /// <summary>
    /// Makes a <see cref="DatabaseAttributeRef"/>.
    /// </summary>
    /// <param name="attribute">Referenced <see cref="DatabaseAttribute"/>.</param>
    /// <returns>A <see cref="DatabaseAttributeRef"/> referencing <paramref name="attribute"/>,
    /// or <b>null</b> if <paramref name="attribute"/> is <b>null</b>.</returns>
    public static DatabaseAttributeRef MakeRef(DatabaseAttribute attribute)
    {
        return attribute == null ? null : new DatabaseAttributeRef(attribute);
    }

    /// <summary>
    /// Gets the <see cref="DatabaseAttribute"/> referenced to by the a <see cref="DatabaseAttributeRef"/>.
    /// </summary>
    /// <param name="databaseAttributeRef">Reference to be resolved.</param>
    /// <param name="element">Element of the schema w.r.t. which the referenced should be resolved.
    /// Theoritically only the schema is used, but since we cache the reference, passing the schema
    /// element allows to avoid taking the schema from the element in case of a cache hit.</param>
    /// <returns>The <see cref="DatabaseAttribute"/> represented by <paramref name="databaseAttributeRef"/></returns>
    public static DatabaseAttribute Resolve(DatabaseAttributeRef databaseAttributeRef,
                                            DatabaseSchemaElement element)
    {
        if (databaseAttributeRef == null)
        {
            return null;
        }
        if (databaseAttributeRef.resolvedTarget == null)
        {
            databaseAttributeRef.resolvedTarget = element.Schema.
                                                  Assemblies[databaseAttributeRef.assemblyName].
                                                  DatabaseClasses[databaseAttributeRef.className].
                                                  Attributes[databaseAttributeRef.attributeName];
        }
        return databaseAttributeRef.resolvedTarget;
    }


    public override string ToString()
    {
        return "@" + this.className + "." + this.attributeName;
    }
}

/// <summary>
/// 
/// </summary>
[Serializable]
public sealed class DatabaseAttributeRefCollection : DatabaseSchemaElement, ICollection<DatabaseAttribute>, ICollection<DatabaseAttributeRef>
{
    private readonly DatabaseSchemaElement parent;
    readonly List<DatabaseAttributeRef> list = new List<DatabaseAttributeRef>();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parent"></param>
    public DatabaseAttributeRefCollection(DatabaseSchemaElement parent)
    {
        this.parent = parent;
    }

    #region ICollection<DatabaseAttribute> Members

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    public void Add(DatabaseAttribute item)
    {
        list.Add(DatabaseAttributeRef.MakeRef(item));
    }

    /// <summary>
    /// 
    /// </summary>
    public void Clear()
    {
        list.Clear();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool Contains(DatabaseAttribute item)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="array"></param>
    /// <param name="arrayIndex"></param>
    public void CopyTo(DatabaseAttribute[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 
    /// </summary>
    public int Count
    {
        get
        {
            return this.list.Count;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public bool IsReadOnly
    {
        get
        {
            return false;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool Remove(DatabaseAttribute item)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region IEnumerable<DatabaseAttribute> Members

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IEnumerator<DatabaseAttribute> GetEnumerator()
    {
        foreach (DatabaseAttributeRef attributeRef in list)
        {
            yield return DatabaseAttributeRef.Resolve(attributeRef, this);
        }
    }

    #endregion

    #region IEnumerable Members

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    #endregion

    #region ICollection<DatabaseAttributeRef> Members

    void ICollection<DatabaseAttributeRef>.Add(DatabaseAttributeRef item)
    {
        this.list.Add(item);
    }

    void ICollection<DatabaseAttributeRef>.Clear()
    {
        this.list.Clear();
    }

    bool ICollection<DatabaseAttributeRef>.Contains(DatabaseAttributeRef item)
    {
        throw new NotImplementedException();
    }

    void ICollection<DatabaseAttributeRef>.CopyTo(DatabaseAttributeRef[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    int ICollection<DatabaseAttributeRef>.Count
    {
        get
        {
            return this.list.Count;
        }
    }

    bool ICollection<DatabaseAttributeRef>.IsReadOnly
    {
        get
        {
            return false;
        }
    }

    bool ICollection<DatabaseAttributeRef>.Remove(DatabaseAttributeRef item)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region IEnumerable<DatabaseAttributeRef> Members

    IEnumerator<DatabaseAttributeRef> IEnumerable<DatabaseAttributeRef>.GetEnumerator()
    {
        return this.list.GetEnumerator();
    }

    #endregion

    #region IDatabaseSchemaElement Members

    /// <summary>
    /// 
    /// </summary>
    public override DatabaseSchema Schema
    {
        get
        {
            return this.parent.Schema;
        }
    }

    #endregion
}
}