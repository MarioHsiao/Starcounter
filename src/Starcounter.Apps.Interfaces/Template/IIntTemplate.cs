// ***********************************************************************
// <copyright file="IIntTemplate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Templates.Interfaces {
    /// <summary>
    /// Interface IIntTemplate
    /// </summary>
    public interface IIntTemplate : IValueTemplate {

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        /// <value>The default value.</value>
        long DefaultValue { get; set; }
    }
}
