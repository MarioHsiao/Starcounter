// ***********************************************************************
// <copyright file="IBoolTemplate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Templates.Interfaces {
    /// <summary>
    /// Interface IBoolTemplate
    /// </summary>
    public interface OldIBoolTemplate : OldIValueTemplate {

        /// <summary>
        /// Gets or sets a value indicating whether [default value].
        /// </summary>
        /// <value><c>true</c> if [default value]; otherwise, <c>false</c>.</value>
        bool DefaultValue { get; set; }
    }
}
