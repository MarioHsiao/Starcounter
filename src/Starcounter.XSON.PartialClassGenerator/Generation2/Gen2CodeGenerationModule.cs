using System;
using Starcounter.Templates;
using Starcounter.XSON.Interfaces;
using Starcounter.XSON.Metadata;

namespace Starcounter.XSON.PartialClassGenerator {
    /// <summary>
    /// 
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
        public ITemplateCodeGenerator CreateGenerator(Type defaultChildTemplateType, string dotNetLanguage, object template, object metadata) {
            var tvalue = (TValue)template;
            var gen = new Gen2DomGenerator(this, tvalue, defaultChildTemplateType, (CodeBehindMetadata)metadata );
            return new Gen2CSharpGenerator( gen, gen.GenerateDomTree(tvalue));
        }
    }
}
