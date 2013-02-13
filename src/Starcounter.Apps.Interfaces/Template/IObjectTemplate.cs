// ***********************************************************************
// <copyright file="IObjectTemplate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;

namespace Starcounter.Templates.Interfaces {
    /// <summary>
    /// Interface IObjectTemplate
    /// </summary>
    public interface OldIObjectTemplate : OldIValueTemplate {

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        /// <value>The default value.</value>
        Entity DefaultValue { get; set; }
    }
}
