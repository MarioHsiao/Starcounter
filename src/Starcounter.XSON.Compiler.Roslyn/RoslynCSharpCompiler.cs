using System;
using Starcounter.XSON.Metadata;

namespace Starcounter.XSON.Compiler.Roslyn {
    public class RoslynCSharpCompiler {
        //public object Compile(string code) {
        //    return Compile(code, new string[0]);
        //}

        public CodeBehindMetadata AnalyzeCodeBehind(string className, string codeBehindFile) {
            return CodeBehindAnalyzer.Analyze(className, codeBehindFile);
        }

        public object GenerateJsonSerializer(string code, string typeName) {
            throw new NotImplementedException();

            //SyntaxTree tree = SyntaxTree.ParseText(code);

            //var compOptions = new CompilationOptions(
            //       outputKind: OutputKind.DynamicallyLinkedLibrary,
            //       allowUnsafe: true
            // );

            //var compilation = Compilation.Create("Starcounter.GeneratedCode", compOptions);

            //MetadataFileReference[] mrefs = new MetadataFileReference[assemblyRefs.Length];
            //for (int i = 0; i < assemblyRefs.Length; i++){
            //    mrefs[i] = new MetadataFileReference(assemblyRefs[i]);
            //}
            //compilation.AddReferences(mrefs);
            //compilation.AddSyntaxTrees(tree);

            //MemoryStream ms = new MemoryStream();
            //var result1 = compilation.Emit(ms);
            //if (!result1.Success) {
            //    StringBuilder errorMsg = new StringBuilder();
            //    errorMsg.AppendLine("Compilation of generated code failed.");
            //    foreach (var d in result1.Diagnostics) {
            //        errorMsg.AppendLine(d.ToString());
            //    }
            //    throw new Exception(errorMsg.ToString());
            //}

            //var a = Assembly.Load(ms.GetBuffer());
            //return a.GetType(typeName);
        }
    }
}
