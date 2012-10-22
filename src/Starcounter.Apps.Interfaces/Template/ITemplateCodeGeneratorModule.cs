// ***********************************************************************
// <copyright file="ITemplateCodeGeneratorModule.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates.Interfaces;
namespace Starcounter.Internal {
    /// <summary>
    /// Interface ITemplateCodeGeneratorModule
    /// </summary>
    public interface ITemplateCodeGeneratorModule {
        /// <summary>
        /// Creates the generator.
        /// </summary>
        /// <param name="lang">The lang.</param>
        /// <param name="template">The template.</param>
        /// <param name="metadata">The metadata.</param>
        /// <returns>ITemplateCodeGenerator.</returns>
        ITemplateCodeGenerator CreateGenerator( string lang, IAppTemplate template, object metadata );
    }
}
