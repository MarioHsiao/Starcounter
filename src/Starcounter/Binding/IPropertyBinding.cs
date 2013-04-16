// ***********************************************************************
// <copyright file="IPropertyBinding.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using System;

namespace Starcounter.Binding
{

    /// <summary>
    /// Interface to an object representing a binding between a propery (or
    /// field before enhanced) of a managed type and an attribute of a
    /// database type. The managed type could either be a static or dynamic
    /// type.
    /// </summary>
    public interface IPropertyBinding
    {

        /// <summary>
        /// Property index.
        /// </summary>
        /// <value>The index.</value>
        Int32 Index { get; }

        /// <summary>
        /// Property name.
        /// </summary>
        /// <value>The name.</value>
        String Name { get; }

        /// <summary>
        /// Gets name friendly for displaying
        /// </summary>
        String DisplayName { get; }

        /// <summary>
        /// Binding used by the property type if any.
        /// </summary>
        /// <value>The type binding.</value>
        /// <returns>
        /// A type binding. Null if the target is a literal, only set if a
        /// reference property.
        ///   </returns>
        ITypeBinding TypeBinding { get; } // TODO: Rename: TargetTypeBinding

        /// <summary>
        /// Property value type code.
        /// </summary>
        /// <value>The type code.</value>
        DbTypeCode TypeCode { get; }

#if DEBUG
        /// <summary>
        /// Comparing this and given objects and asserting that they are equal.
        /// </summary>
        /// <param name="other">The given object to compare with this object.</param>
        /// <returns>True if the objects are equals and false otherwise.</returns>
        bool AssertEquals(IPropertyBinding other);
#endif
    }
}
