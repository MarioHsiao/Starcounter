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
    public interface IValueTemplate : IStatefullTemplate {

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

    }
}
