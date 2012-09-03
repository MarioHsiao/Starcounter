
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
        /// Estimates relative cost to access the property where 0 representing
        /// a direct access property and the best in terms of performance
        /// (simple database object access with no code).
        /// </summary>
        Int32 AccessCost
        {
            get;
        }

        /// <summary>
        /// Property index.
        /// </summary>
        Int32 Index
        {
            get;
        }

        /// <summary>
        /// Inverse property if any.
        /// </summary>
        IPropertyBinding InversePropertyBinding
        {
            get;
        }

        /// <summary>
        /// Estimates relative cost to mutate the property where 0 representing
        /// a direct access property and the best in terms of performance
        /// (simple database object access with no code).
        /// </summary>
        Int32 MutateCost
        {
            get;
        }

        /// <summary>
        /// Property name.
        /// </summary>
        String Name
        {
            get;
        }

        /// <summary>
        /// Binding used by the property type if any.
        /// </summary>
        /// <returns>
        /// A type binding. Null if the target is a literal, only set if a
        /// reference property.
        /// </returns>
        ITypeBinding TypeBinding
        {
            get;
        }

        /// <summary>
        /// Property value type code.
        /// </summary>
        DbTypeCode TypeCode
        {
            get;
        }
    }
}
