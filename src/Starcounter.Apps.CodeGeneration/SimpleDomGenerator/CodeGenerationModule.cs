// ***********************************************************************
// <copyright file="CodeGenerationModule.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using Starcounter.Templates.Interfaces;
using System;

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
        public ITemplateCodeGenerator CreateGenerator(Type defaultChildObjTemplateType, string dotNetLanguage, object objTemplate, object metadata) {
            var templ = (TObj)objTemplate;
            var gen = new DomGenerator(this, templ, defaultChildObjTemplateType );
            return new CSharpGenerator( gen, gen.GenerateDomTree( templ, (CodeBehindMetadata)metadata));
        }

    }
}
