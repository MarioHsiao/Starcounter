using GenerateMetadataClasses.Model;
using Microsoft.CSharp;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;

namespace GenerateMetadataClasses.CodeGenerator {

    public class MetadataCodeGenerator {
        const string singleFileName = "GeneratedMetadataClasses.cs";
        public readonly string OutputDirectory;

        public MetadataCodeGenerator(string dir) {
            OutputDirectory = dir;
        }

        public void Generate(Schema schema) {
            var unit = Build(schema);
            Write(unit);
        }

        CodeCompileUnit Build(Schema schema) {
            var unit = new CodeCompileUnit();

            var publicNamespace = new CodeNamespace("Starcounter.Metadata");
            var internalNamespace = new CodeNamespace("Starcounter.Internal.Metadata");

            var starcounter = new CodeNamespaceImport("Starcounter");
            var starcounterInternal = new CodeNamespaceImport("Starcounter.Internal");
            var starcounterBinding = new CodeNamespaceImport("Starcounter.Binding");
            var system = new CodeNamespaceImport("System");

            var usings = new[] { starcounter, starcounterInternal, starcounterBinding, system };

            publicNamespace.Imports.Add(new CodeNamespaceImport(internalNamespace.Name));
            publicNamespace.Imports.AddRange(usings);

            internalNamespace.Imports.Add(new CodeNamespaceImport(publicNamespace.Name));
            internalNamespace.Imports.AddRange(usings);

            var classGenerator = new ClassGenerator(publicNamespace, internalNamespace);

            foreach (var table in schema.Tables.Values) {
                classGenerator.Generate(table);
            }

            unit.Namespaces.AddRange(new[] { publicNamespace, internalNamespace });
            return unit;
        }

        void Write(CodeCompileUnit unit) {
            var outPath = Path.GetFullPath(Path.Combine(OutputDirectory, singleFileName));
            var provider = new CSharpCodeProvider();

            provider = new CSharpCodeProvider();
            using (StreamWriter sw = new StreamWriter(outPath, false)) {
                var options = new CodeGeneratorOptions();
                options.BlankLinesBetweenMembers = false;
                var tw = new IndentedTextWriter(sw, "  ");

                provider.GenerateCodeFromCompileUnit(
                    new CodeSnippetCompileUnit("#pragma warning disable 0649, 0169"),
                    sw, options);
                provider.GenerateCodeFromCompileUnit(unit, tw, options);
                provider.GenerateCodeFromCompileUnit(
                    new CodeSnippetCompileUnit("#pragma warning restore 0649, 0169"),
                    sw, options);
                tw.Close();
            }
        }
    }
}