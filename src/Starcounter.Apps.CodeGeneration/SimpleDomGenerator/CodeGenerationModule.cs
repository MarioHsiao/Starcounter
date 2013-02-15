// ***********************************************************************
// <copyright file="CodeGenerationModule.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using Starcounter.Templates.Interfaces;

namespace Starcounter.Internal.Application.CodeGeneration {
    /// <summary>
    /// Class CodeGenerationModule
    /// </summary>
    public class CodeGenerationModule : ITemplateCodeGeneratorModule {

        /// <summary>
        /// Creates the generator.
        /// </summary>
        /// <param name="dotNetLanguage">The dot net language.</param>
        /// <param name="template">The template.</param>
        /// <param name="metadata">The metadata.</param>
        /// <returns>ITemplateCodeGenerator.</returns>
        public ITemplateCodeGenerator CreateGenerator(string dotNetLanguage, object template, object metadata) {
            var gen = new DomGenerator(this, (TPuppet)template);
            return new CSharpGenerator(gen.GenerateDomTree((TPuppet)template, (CodeBehindMetadata)metadata));
        }

    }
}
