// ***********************************************************************
// <copyright file="IConfigurationElement.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Advanced.Configuration {
    /// <summary>
    /// Defines the semantics of an element of a configuration file.
    /// </summary>
    internal interface IConfigurationElement : ICloneable, IKeyPathAware {
        /// <summary>
        /// Gets the parent element.
        /// </summary>
        IConfigurationElement Parent {
            get;
        }

        /// <summary>
        /// Gets the role of the current element in the parent element.
        /// </summary>
        string Role {
            get;
        }

        /// <summary>
        /// Sets the parent element and the role of the current element
        /// in the parent element.
        /// </summary>
        /// <param name="parent">Parent element.</param>
        /// <param name="role">Role of the current element in the parent element.</param>
        void SetParent(IConfigurationElement parent, string role);

        IConfigurationElement Clone(IConfigurationElement newParent);
    }

    /// <summary>
    /// Defines a method <see cref="GetKeyPath"/> that uniquely
    /// identifies the current object in an object tree.
    /// </summary>
    public interface IKeyPathAware {
        /// <summary>
        /// Gets a key that uniquely identifies the current
        /// object in an object tree.
        /// </summary>
        /// <returns>A string.</returns>
        string GetKeyPath();
    }
}