// ***********************************************************************
// <copyright file="JsonToCsMsBuildTask.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Starcounter.XSON.Templates.Factory;
using SXP = Starcounter.XSON.PartialClassGenerator;

namespace Starcounter.Internal.MsBuild {
    /// <summary>
    /// Class responsible for generating partial class for TypedJSON
    /// </summary>
    internal static class JsonToCsMsBuildTask {
        static JsonToCsMsBuildTask() {
            Bootstrapper.Bootstrap();
        }

        /// <summary>
        /// 
        /// </summary>
        internal static bool ExecuteTask(ITaskItem[] InputFiles, ITaskItem[] OutputFiles, TaskLoggingHelper msbuildLog) {
            bool success = true;
            string jsonFilename;
            string codeBehindFilename;
            string generatedCodeStr;

            for (int i = 0; i < InputFiles.Length; i++) {
                jsonFilename = InputFiles[i].ItemSpec;
                codeBehindFilename = jsonFilename + ".cs";

                try {
                    msbuildLog.LogMessage("Creating " + OutputFiles[i].ItemSpec);
                    generatedCodeStr = SXP.PartialClassGenerator.GenerateTypedJsonCode(jsonFilename, codeBehindFilename).GenerateCode();

                    string dir = Path.GetDirectoryName(OutputFiles[i].ItemSpec);
                    if (!Directory.Exists(dir)) {
                        Directory.CreateDirectory(dir);
                    }
                    File.WriteAllText(OutputFiles[i].ItemSpec, generatedCodeStr);
                } catch (TemplateFactoryException ce) {
                    msbuildLog.LogError("json", null, null, jsonFilename, ce.SourceInfo.Line, ce.SourceInfo.Column, 0, 0, ce.Message);
                    success = false;
                } catch (SXP.GeneratorException genEx) {
                    msbuildLog.LogError("json", null, null, jsonFilename, genEx.SourceInfo.Line, genEx.SourceInfo.Column, 0, 0, genEx.Message);
                    success = false;
                } catch (SXP.InvalidCodeBehindException icb) {
                    // Positions in exception are zero-based, translate them all to
                    // 1-based as we expect in the IDE.
                    msbuildLog.LogError("json", null, null, codeBehindFilename, icb.Line + 1, icb.Column + 1, icb.EndLine + 1, icb.EndColumn + 1, icb.Message);
                    success = false;
                } catch (Exception ex) {
                    msbuildLog.LogError("json", null, null, codeBehindFilename, 0, 0, 0, 0, ex.Message);
                    success = false;
                }
            }
            return success;
        }
    }
}
