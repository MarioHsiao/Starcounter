// ***********************************************************************
// <copyright file="JsonToCsMsBuildTask.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Starcounter.XSON.PartialClassGenerator;

namespace Starcounter.Internal.MsBuild {
    /// <summary>
    /// Class that holds code to MsBuild tasks.
    /// </summary>
    internal static class JsonToCsMsBuildTask {
        static JsonToCsMsBuildTask() {
            Bootstrapper.Bootstrap();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if the task successfully executed; otherwise, false.</returns>
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
                    generatedCodeStr = Starcounter.Internal.XSON.PartialClassGenerator.GenerateTypedJsonCode(jsonFilename, codeBehindFilename).GenerateCode();

                    string dir = Path.GetDirectoryName(OutputFiles[i].ItemSpec);
                    if (!Directory.Exists(dir)) {
                        Directory.CreateDirectory(dir);
                    }
                    File.WriteAllText(OutputFiles[i].ItemSpec, generatedCodeStr);
                }
                catch (Starcounter.Internal.JsonTemplate.Error.CompileError ce) {
                    msbuildLog.LogError("json", null, null, jsonFilename, ce.Position.Item1, ce.Position.Item2, 0, 0, ce.Message);
                    success = false;
                } catch (InvalidCodeBehindException icb) {
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
