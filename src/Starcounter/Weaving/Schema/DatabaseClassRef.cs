// ***********************************************************************
// <copyright file="DatabaseClassRef.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Sc.Server.Weaver.Schema
{
/// <summary>
/// Serializable reference to a <see cref="DatabaseClass"/>.
/// </summary>
/// <remarks>
/// The purpose of this object is to store a cross-assembly reference that is serializable
/// without hard-linking to the assembly, so that assemblies can be serialized separately.
/// References are resolved transparently and are never exposed to consumer code.
/// </remarks>
[Serializable]
internal sealed class DatabaseClassRef : IEquatable<DatabaseClassRef>, IDatabaseAttributeType
{
    // Cached referenced DatabaseClass (null if the reference has not yet been resolved).
    [NonSerialized] private DatabaseClass resolvedTarget;

    private readonly string assemblyName;
    private readonly string className;

    /// <summary>
    /// Initializes a new <see cref="DatabaseClassRef"/>.
    /// </summary>
    /// <param name="target">Referenced <see cref="DatabaseClass"/>.</param>
    private DatabaseClassRef(DatabaseClass target)
    {
        this.resolvedTarget = target;
        this.assemblyName = target.Assembly.Name;
        this.className = target.Name;
    }

    /// <summary>
    /// Makes a <see cref="DatabaseClassRef"/>.
    /// </summary>
    /// <param name="databaseClass">Referenced <see cref="DatabaseClass"/>.</param>
    /// <returns>A <see cref="DatabaseClassRef"/> referencing <paramref name="databaseClass"/>,
    /// or <b>null</b> if <paramref name="databaseClass"/> is <b>null</b>.</returns>
    public static DatabaseClassRef MakeRef(DatabaseClass databaseClass)
    {
        return databaseClass == null ? null : new DatabaseClassRef(databaseClass);
    }

    /// <summary>
    /// Makes the ref.
    /// </summary>
    /// <param name="attributeType">Type of the attribute.</param>
    /// <returns>IDatabaseAttributeType.</returns>
    public static IDatabaseAttributeType MakeRef(IDatabaseAttributeType attributeType)
    {
        DatabaseClass dbClass = attributeType as DatabaseClass;
        if (dbClass != null)
        {
            return MakeRef(dbClass);
        }
        else
        {
            return attributeType;
        }
    }

    /// <summary>
    /// Gets the <see cref="DatabaseClass"/> referenced to by the a <see cref="DatabaseClassRef"/>.
    /// </summary>
    /// <param name="databaseClassRef">Reference to be resolved.</param>
    /// <param name="element">Element of the schema w.r.t. which the referenced should be resolved.
    /// Theoritically only the schema is used, but since we cache the reference, passing the schema
    /// element allows to avoid taking the schema from the element in case of a cache hit.</param>
    /// <returns>The <see cref="DatabaseClass"/> represented by <paramref name="databaseClassRef"/></returns>
    internal static DatabaseClass Resolve(DatabaseClassRef databaseClassRef,
                                          DatabaseSchemaElement element)
    {
        if (databaseClassRef == null)
        {
            return null;
        }
        if (databaseClassRef.resolvedTarget == null)
            databaseClassRef.resolvedTarget = element.Schema.
                                              Assemblies[databaseClassRef.assemblyName].
                                              DatabaseClasses[databaseClassRef.className];
        return databaseClassRef.resolvedTarget;
    }

    internal static IDatabaseAttributeType Resolve(IDatabaseAttributeType attributeType, DatabaseSchemaElement element)
    {
        DatabaseClassRef classRef = attributeType as DatabaseClassRef;
        if (classRef != null)
        {
            return Resolve(classRef, element);
        }
        else
        {
            return attributeType;
        }
    }

    /// <summary>
    /// Returns a <see cref="System.String" /> that represents this instance.
    /// </summary>
    /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
    public override string ToString()
    {
        return "@" + this.className;
    }

    /// <summary>
    /// Equalses the specified other.
    /// </summary>
    /// <param name="other">The other.</param>
    /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
    public bool Equals(DatabaseClassRef other)
    {
        if (other == null)
        {
            return false;
        }
        return this.assemblyName == other.assemblyName &&
               this.className == other.className;
    }

    /// <summary>
    /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
    public override bool Equals(object obj)
    {
        return this.Equals(obj as DatabaseClassRef);
    }


    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
    public override int GetHashCode()
    {
        return this.className.GetHashCode();
    }
}
}