
using Starcounter;
using System;
using Starcounter.Binding;

namespace Starcounter
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
        Int32 Index { get; }

        /// <summary>
        /// Property name.
        /// </summary>
        String Name { get; }

        /// <summary>
        /// Binding used by the property type if any.
        /// </summary>
        /// <returns>
        /// A type binding. Null if the target is a literal, only set if a
        /// reference property.
        /// </returns>
        ITypeBinding TypeBinding { get; }

        /// <summary>
        /// Property value type code.
        /// </summary>
        DbTypeCode TypeCode { get; }
    }
}
