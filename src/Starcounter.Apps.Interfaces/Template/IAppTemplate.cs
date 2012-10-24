// ***********************************************************************
// <copyright file="IAppTemplate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Templates.Interfaces {
    /// <summary>
    /// Interface IAppTemplate
    /// </summary>
    public interface IAppTemplate : IParentTemplate {

        /// <summary>
        /// Adds the specified name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The name.</param>
        /// <returns>``0.</returns>
        T Add<T>(string name) where T : ITemplate, new();
        /// <summary>
        /// Adds the specified name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <returns>``0.</returns>
        T Add<T>(string name, IAppTemplate type ) where T : IAppListTemplate, new();

        /// <summary>
        /// Gets the properties.
        /// </summary>
        /// <value>The properties.</value>
        IPropertyTemplates Properties { get; }
    }
}
