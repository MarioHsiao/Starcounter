
using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Starcounter.Internal;
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Internal.Application.JsonReader;
using Starcounter.Templates;
using Starcounter.Templates.Interfaces;

namespace Starcounter.Internal.MsBuild
{
	public class JsonToCsMsBuildTask : Task, ITask
	{
		[Required]
		public ITaskItem[] InputFiles { get; set; }

		[Output]
		public ITaskItem[] OutputFiles { get; set; }

		public override bool Execute()
		{
			bool success = true;
			string jsonFilename;
            string codeBehindFilename;
            string generatedCodeStr;

			for (int i = 0; i < InputFiles.Length; i++)
			{
                jsonFilename = InputFiles[i].ItemSpec;
                codeBehindFilename = jsonFilename + ".cs";

                try
                {
                    Log.LogMessage("Creating " + OutputFiles[i].ItemSpec);
                    
                    generatedCodeStr = ProcessJsTemplateFile(jsonFilename, codeBehindFilename);
                    File.WriteAllText(OutputFiles[i].ItemSpec, generatedCodeStr);
                }
                catch (Starcounter.Internal.Error.CompileError ce)
                {
                    Log.LogError("json", null, null, jsonFilename, ce.Position.Item1, ce.Position.Item2, 0, 0, ce.Message);
                    success = false;
                }
                catch (Exception ex)
                {
                    Log.LogError("json", null, null, codeBehindFilename, 0, 0, 0, 0, ex.Message);
                    success = false;
                }
			}
			return success;
		}

		private string ProcessJsTemplateFile(string jsonFilename, string codeBehindFilename)
		{
			AppTemplate t;
            CodeBehindMetadata metadata;
			ITemplateCodeGenerator codegen;
			ITemplateCodeGeneratorModule codegenmodule;
            String jsonContent = File.ReadAllText(jsonFilename);

            var className = Paths.StripFileNameWithoutExtention(jsonFilename);
            metadata = CodeBehindAnalyzer.Analyze(className, codeBehindFilename);
			t = TemplateFromJs.CreateFromJs(jsonContent, false);
            if (t.ClassName == null)
			{
				t.ClassName = className;
			}

            if (String.IsNullOrEmpty(t.Namespace))
                t.Namespace = metadata.RootNamespace;

			codegenmodule = new CodeGenerationModule();
			codegen = codegenmodule.CreateGenerator("C#", t, metadata);

			return codegen.GenerateCode();
		}
	}
}
