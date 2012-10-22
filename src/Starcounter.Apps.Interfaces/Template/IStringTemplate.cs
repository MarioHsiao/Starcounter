// ***********************************************************************
// <copyright file="IStringTemplate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Templates.Interfaces {
    /// <summary>
    /// Interface IStringTemplate
    /// </summary>
    public interface IStringTemplate : IValueTemplate {

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        /// <value>The default value.</value>
        string DefaultValue { get; set; }
    }
}
