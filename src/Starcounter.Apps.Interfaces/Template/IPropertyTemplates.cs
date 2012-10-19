// ***********************************************************************
// <copyright file="IPropertyTemplates.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Collections.Generic;
namespace Starcounter.Templates.Interfaces {
    /// <summary>
    /// Interface IPropertyTemplates
    /// </summary>
    public interface IPropertyTemplates : IList<ITemplate> {

        /// <summary>
        /// Gets the <see cref="ITemplate" /> with the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>ITemplate.</returns>
        ITemplate this[string id] { get; }

    }
}
