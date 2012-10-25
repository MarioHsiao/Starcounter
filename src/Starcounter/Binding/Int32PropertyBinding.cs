// ***********************************************************************
// <copyright file="Int32PropertyBinding.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Binding
{

    /// <summary>
    /// Class Int32PropertyBinding
    /// </summary>
    public abstract class Int32PropertyBinding : IntPropertyBinding
    {

        /// <summary>
        /// Gets the type code.
        /// </summary>
        /// <value>The type code.</value>
        public override sealed DbTypeCode TypeCode { get { return DbTypeCode.Int32; } }

        /// <summary>
        /// Gets the value of an integer attribute as a 8-bits signed integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{SByte}.</returns>
        /// <exception cref="System.NotSupportedException">Attempt to convert a 32-bit integer value to a 8-bit integer value.</exception>
        protected override sealed SByte? DoGetSByte(object obj)
        {
            throw new NotSupportedException(
                "Attempt to convert a 32-bit integer value to a 8-bit integer value."
            );
        }

        /// <summary>
        /// Gets the value of an integer attribute as a 16-bits signed integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Int16}.</returns>
        /// <exception cref="System.NotSupportedException">Attempt to convert a 32-bit integer value to a 16-bit integer value.</exception>
        protected override sealed Int16? DoGetInt16(object obj)
        {
            throw new NotSupportedException(
                "Attempt to convert a 32-bit integer value to a 16-bit integer value."
            );
        }

        /// <summary>
        /// Gets the value of an integer attribute as a 64-bits signed integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Int64}.</returns>
        protected override sealed Int64? DoGetInt64(object obj)
        {
            return DoGetInt32(obj);
        }
    };
}
