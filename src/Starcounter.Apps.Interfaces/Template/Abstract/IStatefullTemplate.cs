// ***********************************************************************
// <copyright file="IStatefullTemplate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
namespace Starcounter.Templates.Interfaces {

    /// <summary>
    /// Template for elements that can be edited or have their state changed or that can contain child
    /// elements (properties or array elements) that can be edited or have their state changed. This is
    /// true for all elements exept for Action elements (i.e. the ActionTemplate does not inherit this class).
    /// </summary>
    public interface IStatefullTemplate : ITemplate {

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="IStatefullTemplate" /> is editable.
        /// </summary>
        /// <value><c>true</c> if editable; otherwise, <c>false</c>.</value>
        bool Editable { get; set; }

        /// <summary>
        /// As this template is represented by a runtime statefull object or value, we need to know how to create
        /// a that object or value.
        /// </summary>
        /// <param name="parent">The host of the new object. Either a App or a AppList</param>
        /// <returns>The value or object. For instance, if this is a StringTemplate, the default string
        /// for the property to be in the new App object is returned.</returns>
        object CreateInstance( IContainer parent );

        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        Type InstanceType { get; }

    }
}
