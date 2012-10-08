

using Starcounter.Templates;
using Starcounter.Templates.Interfaces;

namespace Starcounter.Internal.Application.CodeGeneration {
    public class CodeGenerationModule : ITemplateCodeGeneratorModule {

        public ITemplateCodeGenerator CreateGenerator(string dotNetLanguage, IAppTemplate template, object metadata) {
            var gen = new DomGenerator(this, (AppTemplate)template);
            return new CSharpGenerator(gen.GenerateDomTree((AppTemplate)template, (CodeBehindMetadata)metadata));
        }

    }
}
