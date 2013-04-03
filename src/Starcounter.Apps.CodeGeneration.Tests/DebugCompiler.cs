using System;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Text;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Starcounter.Internal;
using Starcounter.Internal.JsonPatch;
using Starcounter.Internal.Uri;
using Starcounter.Templates;
using Starcounter.Templates.Interfaces;

namespace Starcounter.Apps.CodeGeneration.Tests {
    internal class GenereratedJsonCodeCompiler {
        internal static Puppet CompileCode(string generatedCode, string codebehindCode, string fullClassName) {
            Assembly assembly;
            Compilation compilation;
            CompilationOptions copts;
            MemoryStream ms;
            SyntaxTree generatedTree;
            SyntaxTree codebehindTree;
            
            generatedTree = SyntaxTree.ParseText(generatedCode);
            codebehindTree = SyntaxTree.ParseText(codebehindCode);

            copts = new CompilationOptions(
                   outputKind: OutputKind.DynamicallyLinkedLibrary,
                   allowUnsafe: true,
                   warningLevel: 1
             );

            compilation = Compilation.Create("jsonserializer", copts);
            compilation = compilation.AddReferences(
                                new MetadataFileReference(typeof(object).Assembly.Location),                    // System.dll
                                new MetadataFileReference(typeof(Db).Assembly.Location),                        // Starcounter.dll
                                new MetadataFileReference(typeof(JsonHelper).Assembly.Location),                // Starcounter.JsonPatch.dll
                                new MetadataFileReference(typeof(Template).Assembly.Location),                  // Starcounter.Apps.dll
                                new MetadataFileReference(typeof(ITemplateCodeGenerator).Assembly.Location),        // Starcounter.Apps.Interfaces.dll
                                new MetadataFileReference(typeof(RequestHandler).Assembly.Location),            // Starcounter.REST.dll
                                new MetadataFileReference(typeof(IDynamicMetaObjectProvider).Assembly.Location) // System.Core.dll
                          );   
            compilation = compilation.AddSyntaxTrees(codebehindTree, generatedTree);
            
            ms = new MemoryStream();

            // TODO:
            // Compilation of current TestMessage.g.cs (and TestMessage.json.cs) generates a StackOverFlowException for
            // some reason I havent figured out. The exact same code works when used in a real app though.
            EmitResult result = compilation.Emit(ms);

            if (!result.Success) {
                StringBuilder errorMsg = new StringBuilder();
                errorMsg.AppendLine("Compilation of generated code failed.");
                foreach (var d in result.Diagnostics) {
                    errorMsg.AppendLine(d.ToString());
                }
                throw new Exception(errorMsg.ToString());
            }

            assembly = Assembly.Load(ms.GetBuffer());

            // TODO:
            // Trying to create an instance of the class just halts the test.
            return null;// (App)assembly.CreateInstance(fullClassName);
        }
    }
}
