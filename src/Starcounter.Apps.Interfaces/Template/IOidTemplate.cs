// ***********************************************************************
// <copyright file="IOidTemplate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
namespace Starcounter.Templates.Interfaces {
    /// <summary>
    /// Interface IOidTemplate
    /// </summary>
    public interface IOidTemplate : IValueTemplate {

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        /// <value>The default value.</value>
        UInt64 DefaultValue { get; set; }

    }
}
