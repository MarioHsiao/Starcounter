// ***********************************************************************
// <copyright file="SinglePropertyBinding.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Binding
{

    /// <summary>
    /// Class SinglePropertyBinding
    /// </summary>
    public abstract class SinglePropertyBinding : RealPropertyBinding
    {

        /// <summary>
        /// Property value type code.
        /// </summary>
        /// <value>The type code.</value>
        public override sealed DbTypeCode TypeCode { get { return DbTypeCode.Single; } }

        /// <summary>
        /// Gets the value of a 64-bits floating point attribute.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Double}.</returns>
        protected override sealed Double? DoGetDouble(object obj)
        {
            return DoGetSingle(obj);
        }
    }
}
