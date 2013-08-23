// ***********************************************************************
// <copyright file="CodeGenerationModule.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using Starcounter.Templates.Interfaces;
using System;
using Starcounter.XSON.Metadata;
using TJson = Starcounter.Templates.Schema<Starcounter.Json<object>>;


namespace Starcounter.Internal.Application.CodeGeneration {
    /// <summary>
    /// Class CodeGenerationModule
    /// </summary>
    public class Gen1CodeGenerationModule : ITemplateCodeGeneratorModule {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="defaultChildObjTemplateType"></param>
        /// <param name="dotNetLanguage"></param>
        /// <param name="objTemplate"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        public ITemplateCodeGenerator CreateGenerator(Type defaultChildObjTemplateType, string dotNetLanguage, object objTemplate, object metadata) {
            var templ = (TJson)objTemplate;
            var gen = new Gen1DomGenerator(this, templ, defaultChildObjTemplateType );
            return new Gen1CSharpGenerator( gen, gen.GenerateDomTree( templ, (CodeBehindMetadata)metadata));
        }

    }
}
