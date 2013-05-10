// ***********************************************************************
// <copyright file="JsonToCsMsBuildTask.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Starcounter.Internal;
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Internal.JsonTemplate;
using Starcounter.Templates;
using Starcounter.Templates.Interfaces;
using Starcounter.XSON.Metadata;

namespace Starcounter.Internal.MsBuild
{
    /// <summary>
    /// Class that holds code to MsBuild tasks.
    /// </summary>
    public class BuildCustomObjClass<TObjType>
    {
        private static IJsonFactory factory = new JsonFactoryImpl();

        /// <summary>
        /// When overridden in a derived class, executes the task.
        /// </summary>
        /// <returns>true if the task successfully executed; otherwise, false.</returns>
        public static bool ExecuteTask(ITaskItem[] InputFiles, ITaskItem[] OutputFiles, TaskLoggingHelper msbuildLog)
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
                    msbuildLog.LogMessage("Creating " + OutputFiles[i].ItemSpec);

                    generatedCodeStr = ProcessJsTemplateFile(typeof(TObjType),jsonFilename, codeBehindFilename);
                    File.WriteAllText(OutputFiles[i].ItemSpec, generatedCodeStr);
                }
                catch (Starcounter.Internal.JsonTemplate.Error.CompileError ce)
                {
                    msbuildLog.LogError("json", null, null, jsonFilename, ce.Position.Item1, ce.Position.Item2, 0, 0, ce.Message);
                    success = false;
                }
                catch (Exception ex)
                {
                    msbuildLog.LogError("json", null, null, codeBehindFilename, 0, 0, 0, 0, ex.Message);
                    success = false;
                }
            }
            return success;
        }

        /// <summary>
        /// Processes the js template file.
        /// </summary>
        /// <param name="objTemplateType">The object template type.</param>
        /// <param name="jsonFilename">The json filename.</param>
        /// <param name="codeBehindFilename">The code behind filename.</param>
        /// <returns>System.String.</returns>
        private static string ProcessJsTemplateFile(Type objTemplateType, string jsonFilename, string codeBehindFilename)
        {
            TObj t;
            CodeBehindMetadata metadata;
            ITemplateCodeGenerator codegen;
            ITemplateCodeGeneratorModule codegenmodule;
            String jsonContent = File.ReadAllText(jsonFilename);

            var className = Paths.StripFileNameWithoutExtention(jsonFilename);
            metadata = (CodeBehindMetadata)factory.Compiler.AnalyzeCodeBehind(className, codeBehindFilename);

            // TODO: 
            // Add functionality to factory to get the sourcecode.

            t = TemplateFromJs.CreateFromJs(jsonContent, false);
            if (t.ClassName == null)
            {
                t.ClassName = className;
            }

            if (String.IsNullOrEmpty(t.Namespace))
                t.Namespace = metadata.RootNamespace;

            codegenmodule = new CodeGenerationModule();
            codegen = codegenmodule.CreateGenerator(objTemplateType,"C#", t, metadata);

            return codegen.GenerateCode();
        }
    }

}
