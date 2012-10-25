// ***********************************************************************
// <copyright file="IDecimalTemplate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Templates.Interfaces {
    /// <summary>
    /// Interface IDecimalTemplate
    /// </summary>
    public interface IDecimalTemplate : IValueTemplate {

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        /// <value>The default value.</value>
        decimal DefaultValue { get; set; }
    }
}
