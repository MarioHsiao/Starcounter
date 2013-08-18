// ***********************************************************************
// <copyright file="CodeGenerationModule.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using Starcounter.Templates.Interfaces;
using System;
using Starcounter.XSON.Metadata;

namespace Starcounter.Internal.MsBuild.Codegen {

    /// <summary>
    /// Class CodeGenerationModule
    /// </summary>
    public class Gen2CodeGenerationModule : ITemplateCodeGeneratorModule {

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
            var gen = new Gen2DomGenerator(this, templ, defaultChildObjTemplateType );
            return new Gen2CSharpGenerator( gen, gen.GenerateDomTree( templ, (CodeBehindMetadata)metadata));
        }

    }
}
