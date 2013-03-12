// ***********************************************************************
// <copyright file="IObjectView.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Binding;
using System;

namespace Starcounter
{

    /// <summary>
    /// Interface to an object that acts as a view of a data object. Provides
    /// methods for accessing the data of the viewed object.
    /// </summary>
    public interface IObjectView
    {
        /// <summary>
        /// View type binding.
        /// </summary>
        ITypeBinding TypeBinding { get; }

        /// <summary>
        /// Determines if the current object is equal to or derived from the
        /// specified object.
        /// </summary>
        /// <param name="obj">
        /// The object to compare to.
        /// </param>
        /// <returns>
        /// True if the current object equals or is directly or indirectly
        /// derived from the specified object.
        /// </returns>
        Boolean EqualsOrIsDerivedFrom(IObjectView obj); // TODO: Not supported!

        /// <summary>
        /// Gets the value of a Binary field.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        Nullable<Binary> GetBinary(Int32 index);

        /// <summary>
        /// Gets the value of a boolean attribute.
        /// </summary>
        Nullable<Boolean> GetBoolean(Int32 index);

        /// <summary>
        /// Gets the value of an integer attribute as a unsigned 8-bits integer.
        /// </summary>
        Nullable<Byte> GetByte(Int32 index);

        /// <summary>
        /// Gets the value of a timestamp attribute.
        /// </summary>
        Nullable<DateTime> GetDateTime(Int32 index);

        /// <summary>
        /// Gets the value of a decimal attribute.
        /// </summary>
        Nullable<Decimal> GetDecimal(Int32 index);

        /// <summary>
        /// Gets the value of a 64-bits floating point attribute.
        /// </summary>
        Nullable<Double> GetDouble(Int32 index);

        /// <summary>
        /// Gets the value of an integer attribute as a signed 16-bits integer.
        /// </summary>
        Nullable<Int16> GetInt16(Int32 index);

        /// <summary>
        /// Gets the value of an integer attribute as a signed 32-bits integer.
        /// </summary>
        Nullable<Int32> GetInt32(Int32 index);

        /// <summary>
        /// Gets the value of an integer attribute as a signed 64-bits integer.
        /// </summary>
        Nullable<Int64> GetInt64(Int32 index);

        /// <summary>
        /// Gets the value of a reference attribute.
        /// </summary>
        IObjectView GetObject(Int32 index);

        /// <summary>
        /// Gets the value of an integer attribute as a signed 8-bits integer.
        /// </summary>
        Nullable<SByte> GetSByte(Int32 index);

        /// <summary>
        /// Gets the value of a 32-bits floating point attribute.
        /// </summary>
        Nullable<Single> GetSingle(Int32 index);

        /// <summary>
        /// Gets the value of a string attribute.
        /// </summary>
        String GetString(Int32 index);

        /// <summary>
        /// Gets the value of an integer attribute as a unsigned 16-bits integer.
        /// </summary>
        Nullable<UInt16> GetUInt16(Int32 index);

        /// <summary>
        /// Gets the value of an integer attribute as a unsigned 32-bits integer.
        /// </summary>
        Nullable<UInt32> GetUInt32(Int32 index);

        /// <summary>
        /// Gets the value of an integer attribute as a unsigned 64-bits integer.
        /// </summary>
        Nullable<UInt64> GetUInt64(Int32 index);

        #region Temporary extension methods from Entity
        void Attach(ObjectRef objectRef, TypeBinding typeBinding);
        void Attach(ulong addr, ulong oid, TypeBinding typeBinding);
        ObjectRef ThisRef {get;set;}
        #endregion

#if DEBUG
        /// <summary>
        /// Comparing this and given objects and asserting that they are equal.
        /// </summary>
        /// <param name="other">The given object to compare with this object.</param>
        /// <returns>True if the objects are equals and false otherwise.</returns>
        bool AssertEquals(IObjectView other);
#endif
    };
}
