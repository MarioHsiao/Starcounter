// ***********************************************************************
// <copyright file="TDoubleBinding.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Binding
{

    /// <summary>
    /// Class TDoubleBinding
    /// </summary>
    public abstract class TDoubleBinding : RealPropertyBinding
    {

        /// <summary>
        /// Gets the type code.
        /// </summary>
        /// <value>The type code.</value>
        public override sealed DbTypeCode TypeCode { get { return DbTypeCode.Double; } }

        /// <summary>
        /// Gets the value of a 32-bits floating point attribute.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Single}.</returns>
        /// <exception cref="System.NotSupportedException">Attempt to convert a single to a double.</exception>
        protected override sealed Single? DoGetSingle(object obj)
        {
            throw new NotSupportedException(
                "Attempt to convert a single to a double."
            );
        }
    }
}
