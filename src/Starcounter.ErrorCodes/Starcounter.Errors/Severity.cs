// ***********************************************************************
// Assembly         : Starcounter.Errors
// Author           : Starcounter AB
// Created          : 10-17-2012
//
// Last Modified By : Starcounter AB
// Last Modified On : 10-17-2012
// ***********************************************************************
// <copyright file="Severity.cs" company="Starcounter AB">
//     . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace Starcounter.Errors
{
    /// <summary>
    /// Enum Severity
    /// </summary>
    public enum Severity : uint
    {
        /// <summary>
        /// The success
        /// </summary>
        Success = 0x0,
        /// <summary>
        /// The informational
        /// </summary>
        Informational = 0x1,
        /// <summary>
        /// The warning
        /// </summary>
        Warning = 0x2,
        /// <summary>
        /// The error
        /// </summary>
        Error = 0x3,
    }
}
