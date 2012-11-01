// ***********************************************************************
// <copyright file="RequestProcessorCompiler.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Starcounter.Internal.Uri {

    /// <summary>
    /// Class RequestProcessorCompiler
    /// </summary>
    public class RequestProcessorCompiler {

        /// <summary>
        /// The handlers
        /// </summary>
        public List<RequestProcessorMetaData> Handlers = new List<RequestProcessorMetaData>();

        /// <summary>
        /// Generates the request processor source code.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <returns>System.String.</returns>
        public string GenerateRequestProcessorSourceCode(AstNamespace tree) {
           // tree.Namespace = namesp; // = "__urimatcher" + Generation + "__";
            return tree.GenerateCsSourceCode();
        }

        /// <summary>
        /// Creates a request processor (uri and verb matcher, parser and executor) for all registred
        /// http handlers (Get,Post,Put etc. delegate registrations)
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="path">If not full, the generated code will be saved to an assembly with the given path</param>
        /// <returns>The new matcher/executor/parser</returns>
        internal TopLevelRequestProcessor CreateMatcher(AstNamespace node, string path ) {

            var code = GenerateRequestProcessorSourceCode(node);

            //Console.WriteLine(code);
            SyntaxTree tree = SyntaxTree.ParseText(code);

            var compOptions = new CompilationOptions(
                   outputKind: OutputKind.DynamicallyLinkedLibrary,
                   allowUnsafe: true
             );

            var compilation = Compilation.Create("Starcounter.GeneratedCode", compOptions)
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
            //            compilation.

            MemoryStream ms = new MemoryStream();
            var result1 = compilation.Emit(ms);
            if (!result1.Success) {
                foreach (var d in result1.Diagnostics) {
                    Console.WriteLine(d);
                }
                return null;
            }

            var ilCode = ms.GetBuffer();
            Console.WriteLine(String.Format("Wrote IL code ({0} bytes) to {1}.", ilCode.Length, path));
            var fs = File.Create(path);
            fs.Write(ilCode, 0, ilCode.Length);
            fs.Close();

            AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(
                    new AssemblyName(node.Namespace), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder uriMatcherModuleBuilder = ab.DefineDynamicModule("UriMatcherModule");
            ReflectionEmitResult result = compilation.Emit(uriMatcherModuleBuilder);

            if (!result.Success) {
                foreach (var d in result.Diagnostics) {
                    Console.WriteLine(d);
                }
                return null;
            }
            var topRp = (TopLevelRequestProcessor)Activator.CreateInstance(
                uriMatcherModuleBuilder.GetType(node.Namespace + ".GeneratedRequestProcessor"),
                null, null);



            return topRp;
        }

    }
}
