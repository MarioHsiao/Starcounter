// ***********************************************************************
// <copyright file="IAppListTemplate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Templates.Interfaces {

    /// <summary>
    /// Interface IAppListTemplate
    /// </summary>
    public interface IAppListTemplate : OldIParentTemplate {

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        IAppTemplate Type { get; set; }

     //   IAppTemplate Set<T>() where T : IAppTemplate, new();

    }

}
