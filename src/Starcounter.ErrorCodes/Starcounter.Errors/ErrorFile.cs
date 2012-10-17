// ***********************************************************************
// Assembly         : Starcounter.Errors
// Author           : Starcounter AB
// Created          : 10-17-2012
//
// Last Modified By : Starcounter AB
// Last Modified On : 10-17-2012
// ***********************************************************************
// <copyright file="ErrorFile.cs" company="Starcounter AB">
//     . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

using System.Collections.Generic;

namespace Starcounter.Errors
{
    /// <summary>
    /// Class ErrorFile
    /// </summary>
    public sealed class ErrorFile
    {
        /// <summary>
        /// The error codes
        /// </summary>
        public readonly IList<ErrorCode> ErrorCodes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorFile" /> class.
        /// </summary>
        /// <param name="codes">The codes.</param>
        internal ErrorFile(IList<ErrorCode> codes)
        {
            this.ErrorCodes = codes;
        }
    }
}