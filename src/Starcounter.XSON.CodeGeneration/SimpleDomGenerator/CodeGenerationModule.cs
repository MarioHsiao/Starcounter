// ***********************************************************************
// <copyright file="CodeGenerationModule.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using Starcounter.Templates.Interfaces;
using System;
using Starcounter.XSON.Metadata;

namespace Starcounter.Internal.Application.CodeGeneration {
    /// <summary>
    /// Class CodeGenerationModule
    /// </summary>
    public class CodeGenerationModule : ITemplateCodeGeneratorModule {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="defaultChildObjTemplateType"></param>
        /// <param name="dotNetLanguage"></param>
        /// <param name="objTemplate"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        public ITemplateCodeGenerator CreateGenerator(Type defaultChildObjTemplateType, string dotNetLanguage, object objTemplate, object metadata) {
            var templ = (TObj)objTemplate;
            var gen = new DomGenerator(this, templ, defaultChildObjTemplateType );
            return new CSharpGenerator( gen, gen.GenerateDomTree( templ, (CodeBehindMetadata)metadata));
        }

    }
}
