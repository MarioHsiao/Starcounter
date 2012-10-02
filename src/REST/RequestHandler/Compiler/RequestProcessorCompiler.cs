
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Starcounter.Internal.Uri {

    public class RequestProcessorCompiler {

        public List<RequestProcessorMetaData> Handlers = new List<RequestProcessorMetaData>();

        static public int Generation = 0;

        public string GenerateRequestProcessorSourceCode(AstNamespace tree, out string namesp) {
            Generation++;
            tree.Namespace = namesp = "__urimatcher" + Generation + "__";
            return tree.GenerateCsSourceCode();
        }

        /// <summary>
        /// Creates a request processor (uri and verb matcher, parser and executor) for all registred
        /// http handlers (Get,Post,Put etc. delegate registrations)
        /// </summary>
        /// <returns>The new matcher/executor/parser</returns>
        internal TopLevelRequestProcessor CreateMatcher( AstNamespace node ) {
            string namesp;

            var code = GenerateRequestProcessorSourceCode(node,out namesp);

            //Console.WriteLine(code);
            SyntaxTree tree = SyntaxTree.ParseText(code);

            var compOptions = new CompilationOptions(
                   outputKind: OutputKind.DynamicallyLinkedLibrary,
                   allowUnsafe: true
             );

            var compilation = Compilation.Create("Hello", compOptions)
                             .AddReferences(
                                new MetadataFileReference(typeof(BitsAndBytes).Assembly.Location),
                                new MetadataFileReference(typeof(Func<>).Assembly.Location),
                                new MetadataFileReference(typeof(Utf8Helper).Assembly.Location),
                                new MetadataFileReference(typeof(IDynamicMetaObjectProvider).Assembly.Location),
                                new MetadataFileReference(typeof(object).Assembly.Location),
                                new MetadataFileReference(typeof(HttpStructs.HttpRequest).Assembly.Location),
                                new MetadataFileReference(typeof(RequestProcessor).Assembly.Location)
                             )
                             .AddSyntaxTrees(tree);
            ModuleBuilder uriMatcherModuleBuilder = AppDomain.CurrentDomain
                .DefineDynamicAssembly(new AssemblyName("UriMatcherAssembly"), AssemblyBuilderAccess.RunAndCollect)
                .DefineDynamicModule("UriMatcherModule");
            var result = compilation.Emit(uriMatcherModuleBuilder);
            if (!result.Success) {
                foreach (var d in result.Diagnostics) {
                    Console.WriteLine(d);
                }
            }
            //var m = new __urimatcher2__.GeneratedRequestProcessor();
            var m = (TopLevelRequestProcessor)Activator.CreateInstance(uriMatcherModuleBuilder.GetType(namesp + ".GeneratedRequestProcessor"), null, null);

            foreach (var h in Handlers) {
                m.Register(h.PreparedVerbAndUri, h.Code);
            }
            return m;
        }

    }
}
