using System;

namespace Starcounter.Apps.CodeGeneration.Tests {
    internal class GenereratedJsonCodeCompiler {
        //internal static object CompileCode(string generatedCode, string fullClassName) {
        //    Assembly assembly;
        //    Compilation compilation;
        //    CompilationOptions copts;
        //    MemoryStream ms;
        //    SyntaxTree generatedTree;

        //    generatedTree = SyntaxTree.ParseText(generatedCode);
        //    copts = new CompilationOptions(
        //           outputKind: OutputKind.DynamicallyLinkedLibrary,
        //           allowUnsafe: true,
        //           warningLevel: 1
        //     );

        //    compilation = Compilation.Create("jsonserializer", copts);
        //    compilation = compilation.AddReferences(
        //                        new MetadataFileReference(typeof(object).Assembly.Location),                    // System.dll
        //                        new MetadataFileReference(typeof(Db).Assembly.Location),                        // Starcounter.dll
        //                        new MetadataFileReference(typeof(JsonHelper).Assembly.Location),                // Starcounter.JsonPatch.dll
        //                        new MetadataFileReference(typeof(IDynamicMetaObjectProvider).Assembly.Location) // System.Core.dll
        //                  );
        //    compilation = compilation.AddSyntaxTrees(generatedTree);

        //    ms = new MemoryStream();

        //    EmitResult result = compilation.Emit(ms);

        //    if (!result.Success) {
        //        StringBuilder errorMsg = new StringBuilder();
        //        errorMsg.AppendLine("Compilation of generated code failed.");
        //        foreach (var d in result.Diagnostics) {
        //            errorMsg.AppendLine(d.ToString());
        //        }
        //        throw new Exception(errorMsg.ToString());
        //    }

        //    assembly = Assembly.Load(ms.GetBuffer());
        //    return assembly.CreateInstance(fullClassName);
        //}

        //internal static object CompileCode(string generatedCode, string fullClassName) {
        //    object o = null;

        //    CompilerSettings settings = new CompilerSettings();
        //    settings.AssemblyReferences.Add("Starcounter.dll");
        //    settings.AssemblyReferences.Add("Starcounter.Apps.JsonPatch.dll");
        //    settings.AssemblyReferences.Add("Starcounter.Internal.dll");
        //    settings.AssemblyReferences.Add("Starcounter.BitsAndBytes.Native.dll");
        //    settings.AssemblyReferences.Add("Starcounter.XSON.dll");
        //    settings.Unsafe = true;
        //    settings.GenerateDebugInfo = true;
            
        //    CompilerContext ctx = new CompilerContext(settings, new ConsoleReportPrinter());
            
        //    Evaluator eval = new Evaluator(ctx);

        //    bool b = eval.Run(generatedCode);
        //    o = eval.Evaluate("new " + fullClassName + "();");

        //    return o;
        //}
    }
}
