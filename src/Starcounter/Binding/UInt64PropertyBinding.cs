// ***********************************************************************
// <copyright file="UInt64PropertyBinding.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Binding
{

    /// <summary>
    /// Class UInt64PropertyBinding
    /// </summary>
    public abstract class UInt64PropertyBinding : UTLongBinding
    {

        /// <summary>
        /// Gets the type code.
        /// </summary>
        /// <value>The type code.</value>
        public override sealed DbTypeCode TypeCode { get { return DbTypeCode.UInt64; } }

        /// <summary>
        /// Gets the value of an integer attribute as a 8-bits unsigned integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Byte}.</returns>
        /// <exception cref="System.NotSupportedException">Attempt to convert a 64-bit unsigned value to a 8-bit unsigned value.</exception>
        protected override sealed Byte? DoGetByte(object obj)
        {
            throw new NotSupportedException(
                "Attempt to convert a 64-bit unsigned value to a 8-bit unsigned value."
            );
        }

        /// <summary>
        /// Gets the value of an integer attribute as a 16-bits signed integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Int16}.</returns>
        protected override sealed Int16? DoGetInt16(object obj)
        {
            throw ExceptionForInvalidType();
        }

        /// <summary>
        /// Gets the value of an integer attribute as a 32-bits signed integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Int32}.</returns>
        protected override sealed Int32? DoGetInt32(object obj)
        {
            throw ExceptionForInvalidType();
        }

        /// <summary>
        /// Gets the value of an integer attribute as a 64-bits signed integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Int64}.</returns>
        protected override sealed Int64? DoGetInt64(object obj)
        {
            throw ExceptionForInvalidType();
        }

        /// <summary>
        /// Gets the value of an integer attribute as a 16-bits unsigned integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{UInt16}.</returns>
        /// <exception cref="System.NotSupportedException">Attempt to convert a 64-bit unsigned value to a 16-bit unsigned value.</exception>
        protected override sealed UInt16? DoGetUInt16(object obj)
        {
            throw new NotSupportedException(
                "Attempt to convert a 64-bit unsigned value to a 16-bit unsigned value."
            );
        }

        /// <summary>
        /// Gets the value of an integer attribute as a 32-bits unsigned integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{UInt32}.</returns>
        /// <exception cref="System.NotSupportedException">Attempt to convert a 64-bit unsigned value to a 32-bit unsigned value.</exception>
        protected override sealed UInt32? DoGetUInt32(object obj)
        {
            throw new NotSupportedException(
                "Attempt to convert a 64-bit unsigned value to a 32-bit unsigned value."
            );
        }
    }
}
