// ***********************************************************************
// <copyright file="IDoubleTemplate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Templates.Interfaces {
    /// <summary>
    /// Interface IDoubleTemplate
    /// </summary>
    public interface OldIDoubleTemplate : OldIValueTemplate {

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        /// <value>The default value.</value>
        double DefaultValue { get; set; }
    }
}
