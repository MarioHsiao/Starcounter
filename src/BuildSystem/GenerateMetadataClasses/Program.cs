
namespace GenerateMetadataClasses {
    using CodeGenerator;
    using Deserializer;
    using System;

    class Program {
        public string SchemaFile;
        public string OutputDirectory;

        static void Main(string[] args) {
            if (args.Length < 1) {
                Console.WriteLine("Usage: GenerateMetadataClasses schema.json <output_directory>");
                Environment.Exit(1);
            }

            var p = new Program() {
                SchemaFile = args[0],
                OutputDirectory = args.Length > 1 ? args[1] : Environment.CurrentDirectory
            };

            p.Generate();
        }

        public void Generate() {
            try {
                Console.WriteLine("{0} -> {1}", SchemaFile, OutputDirectory);
                var schema = new MetadataSchemaDeserializer(SchemaFile).Deserialize();
                new MetadataCodeGenerator(OutputDirectory).Generate(schema);
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
                Environment.Exit(1);
            }
        }
    }
}