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
        /// <param name="defaultObjTemplate">The template.</param>
        /// <param name="metadata">The metadata.</param>
        /// <returns>ITemplateCodeGenerator.</returns>
        public ITemplateCodeGenerator CreateGenerator(string dotNetLanguage, object defaultObjTemplate, object metadata) {
            var templ = (TObj)defaultObjTemplate;
            var gen = new DomGenerator(this, templ );
            return new CSharpGenerator( templ, gen.GenerateDomTree( templ, (CodeBehindMetadata)metadata));
        }

    }
}
