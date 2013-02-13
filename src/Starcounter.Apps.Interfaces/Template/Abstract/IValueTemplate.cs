// ***********************************************************************
// <copyright file="IValueTemplate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
namespace Starcounter.Templates.Interfaces {
    /// <summary>
    /// Interface IValueTemplate
    /// </summary>
    public interface OldIValueTemplate : OldIStatefullTemplate {

        /// <summary>
        /// Gets or sets the default value as object.
        /// </summary>
        /// <value>The default value as object.</value>
        object DefaultValueAsObject { get; set; }

        /// <summary>
        /// Gets the type of the instance.
        /// </summary>
        /// <value>The type of the instance.</value>
        new Type InstanceType { get; }

        /// <summary>
        /// True if the property is bound to the underlying Entity.
        /// </summary>
        bool Bound { get; }

    }
}
