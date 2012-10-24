// ***********************************************************************
// <copyright file="IParent.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Templates.Interfaces {
    /// <summary>
    /// Interface IAppNode
    /// </summary>
    public interface IAppNode {

        /// <summary>
        /// The schema element of this app instance
        /// </summary>
        /// <value>The template.</value>
        IParentTemplate Template { get; set; }
        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        IAppNode Parent { get; set; }

    }
}
