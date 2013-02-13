// ***********************************************************************
// <copyright file="IParent.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Templates.Interfaces {
    /// <summary>
    /// Interface IContainer
    /// </summary>
    public interface IContainer {

        /// <summary>
        /// The schema element of this app instance
        /// </summary>
        /// <value>The template.</value>
        OldIParentTemplate Template { get; set; }
        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        IContainer Parent { get; set; }

    }
}
