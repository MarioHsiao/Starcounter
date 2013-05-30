// ***********************************************************************
// <copyright file="CodeGenHelper.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query.Sql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
//using Sc.Properties;
using System.Threading;
using Starcounter.Internal;

namespace Starcounter.Query.Execution
{
    /// <summary>
    /// Used for generating compilable code.
    /// </summary>
    public class CodeGenStringGenerator
    {
        /// <summary>
        /// Represents types of code sections.
        /// </summary>
        public enum CODE_SECTION_TYPE
        {
            /// <summary>
            /// The INCLUDES
            /// </summary>
            INCLUDES,
            /// <summary>
            /// The DECLARATIONS
            /// </summary>
            DECLARATIONS,
            /// <summary>
            /// The GLOBA l_ DATA
            /// </summary>
            GLOBAL_DATA,
            /// <summary>
            /// The FUNCTIONS
            /// </summary>
            FUNCTIONS,
            /// <summary>
            /// The INI t_ DATA
            /// </summary>
            INIT_DATA
        }

        /// <summary>
        /// Total number of code section types.
        /// </summary>
        Int32 numCodeSectionTypes = Enum.GetValues(typeof(CODE_SECTION_TYPE)).Length;

        /// <summary>
        /// Pre-allocated size of each code section.
        /// </summary>
        Int32[] maxCodeSectionSizes = { 8192, 8192, 8192, 8192, 8192 };

        /// <summary>
        /// Strings containing each code section code.
        /// </summary>
        StringBuilder[] codeSections = null;

        /// <summary>
        /// Current number of whitespace indentation for each code section.
        /// </summary>
        Int32[] currentCodeSpaces = null;

        /// <summary>
        /// Identifies SQL query uniquely within database session.
        /// </summary>
        UInt64 uniqueQueryID = 0;

        /// <summary>
        /// Gets the unique query ID.
        /// </summary>
        /// <value>The unique query ID.</value>
        public UInt64 UniqueQueryID
        {
            get
            {
                return uniqueQueryID;
            }
        }

        /// <summary>
        /// Sequential number
        /// </summary>
        UInt64 seqNumber = 0;

        /// <summary>
        /// Seqs the number.
        /// </summary>
        /// <returns>UInt64.</returns>
        public UInt64 SeqNumber()
        {
            return seqNumber++;
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        public CodeGenStringGenerator(UInt64 newUniqueQueryID)
        {
            // Checking for equal length of each array.
            if (numCodeSectionTypes != maxCodeSectionSizes.Length)
                throw new ArgumentException("Failed to initialize CodeGen string generator. Wrong code sections number.");

            // Copying query unique ID.
            uniqueQueryID = newUniqueQueryID;

            // Allocating space.
            codeSections = new StringBuilder[numCodeSectionTypes];
            currentCodeSpaces = new Int32[numCodeSectionTypes];

            // Just pre-allocating strings for each code section.
            for (Int32 i = 0; i < numCodeSectionTypes; i++)
            {
                codeSections[i] = new StringBuilder(maxCodeSectionSizes[i]);
                currentCodeSpaces[i] = 0;
            }

            // Data initialization function.
            AppendLine(CODE_SECTION_TYPE.INIT_DATA, "DLL_EXPORT INT32 CALL_CONV InitData_" + uniqueQueryID + "()");
            AppendLine(CODE_SECTION_TYPE.INIT_DATA, "{");
        }

        /// <summary>
        /// Appends line to specified code section.
        /// </summary>
        /// <param name="codeSectiontype">Type of code section.</param>
        /// <param name="code">Line to append.</param>
        public void AppendLine(CODE_SECTION_TYPE codeSectiontype, String code)
        {
            // Automatically un-indenting code if needed.
            if (code[0] == '}')
                UnIndent(codeSectiontype);

            Int32 sectionType = (Int32) codeSectiontype;
            codeSections[sectionType].Append(GetSpaces(currentCodeSpaces[sectionType]) + code + ENDL);

            // Automatically indenting code if needed.
            if (code[0] == '{')
                Indent(codeSectiontype);
        }

        /// <summary>
        /// Increases the indentation for the specified code section.
        /// </summary>
        public void Indent(CODE_SECTION_TYPE codeSectiontype)
        {
            currentCodeSpaces[(Int32) codeSectiontype] += 2;
        }

        /// <summary>
        /// Decreases the indentation for the specified code section.
        /// </summary>
        public void UnIndent(CODE_SECTION_TYPE codeSectiontype)
        {
            currentCodeSpaces[(Int32) codeSectiontype] -= 2;
        }

        /// <summary>
        /// Returns the string filled with spaces.
        /// </summary>
        public String GetSpaces(Int32 numOfSpaces)
        {
            return new String(' ', numOfSpaces);
        }

        /// <summary>
        /// End-line symbol.
        /// </summary>
        public static String ENDL
        {
            get
            {
                return "\n";
            }
        }

#if false
        /// <summary>
        /// Entry point to get the complete generated code.
        /// </summary>
        /// <returns>String containing generated code.</returns>
        public String GetGeneratedCode()
        {
            String completeCode = "// GENERATED CODE, DO NOT EDIT." + ENDL;

            // Finalizing the entry point init code.
            AppendLine(CODE_SECTION_TYPE.INIT_DATA, "return 0;");
            AppendLine(CODE_SECTION_TYPE.INIT_DATA, "}" + ENDL);

            // Adding external template static code file as a first entry.
            String staticGenCode = Resources.GenCodeStatic.Replace("REPLACE_ME_ID", uniqueQueryID.ToString());
            completeCode += staticGenCode;

            // Just combining all sections together.
            for (Int32 i = 0; i < numCodeSectionTypes; i++)
                completeCode += codeSections[i].ToString() + ENDL;

            completeCode += "// END OF GENERATED CODE." + ENDL;
            return completeCode;
        }
#endif
    }

    /// <summary>
    /// Class CompilerHelper
    /// </summary>
    public class CompilerHelper
    {
        /// <summary>
        /// Path to Starcounter installation folder.
        /// </summary>
        static String installationDir = GetInstalledDirFromEnv();

        /// <summary>
        /// Returns the directory path where Starcounter is installed,
        /// obtained from environment variables.
        /// </summary>
        static String GetInstalledDirFromEnv()
        {
            // First checking the user-wide installation directory.
            String scInstDir = Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory,
                                                                  EnvironmentVariableTarget.User);
            if (scInstDir != null) return scInstDir;

            // Then checking the system-wide installation directory.
            scInstDir = Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory,
                                                           EnvironmentVariableTarget.Machine);
            return scInstDir;
        }

        /// <summary>
        /// Compiles the library and loads it.
        /// </summary>
        /// <param name="generatedCode">String containing code that has been generated.</param>
        /// <param name="uniqueQueryID">The unique query ID.</param>
        /// <returns>String containing error description or 'null' if everything is fine.</returns>
        public static String CompileAndVerifyLibrary(String generatedCode, UInt64 uniqueQueryID)
        {
            // Checking that Starcounter is properly installed.
            if (installationDir == null)
                return "Starcounter installation directory was not found. Corrupted Starcounter installation.";

            // Creating temporary code generation directory.
            String dbName = "CurrentDB";
            String savingDir = System.IO.Path.Combine(installationDir, System.IO.Path.Combine("GeneratedCode", dbName));

            // Creating empty directory if does not exist.
            if (!Directory.Exists(savingDir))
                Directory.CreateDirectory(savingDir);

            // Path to generated code file.
            String generatedName = "GeneratedCode_" + uniqueQueryID;
            String cppFilePath = System.IO.Path.Combine(savingDir, generatedName + ".cpp"),
                   dllFilePath = System.IO.Path.Combine(savingDir, generatedName + ".dll");

            // Obtaining a list of previously generated files.
            String[] prevGenList = Directory.GetFiles(savingDir, generatedName + ".*");

            // Deleting existing generated files.
            foreach (String genFile in prevGenList)
                File.Delete(genFile);

            // Writing generated code to a file.
            File.WriteAllText(cppFilePath, generatedCode);

            // Starting the compiler.
            Process compilerTool = new Process();
#if DEBUG
            try
            {
                // Compiler executable options.
                compilerTool.StartInfo.FileName = @"C:\Program Files (x86)\Microsoft Visual Studio 10.0\VC\bin\amd64\cl.exe";
                compilerTool.StartInfo.UseShellExecute = false;
                compilerTool.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                compilerTool.StartInfo.CreateNoWindow = true;
                compilerTool.StartInfo.WorkingDirectory = savingDir;

                // Adding needed paths to resolve CL library dependencies.
                compilerTool.StartInfo.EnvironmentVariables.Add("LIB", @"C:\Program Files (x86)\Microsoft Visual Studio 10.0\VC\lib\amd64");
                compilerTool.StartInfo.EnvironmentVariables.Add("Platform", "X64");
                compilerTool.StartInfo.Arguments = "\"" + cppFilePath + "\"" + " /Od /Zi /LDd /MDd /link /ENTRY:JustEntryPoint_" + uniqueQueryID + " /SUBSYSTEM:WINDOWS /MACHINE:X64 /DEBUG /DLL";
                compilerTool.StartInfo.RedirectStandardError = true;
                compilerTool.StartInfo.RedirectStandardOutput = true;

                // Starting compilation.
                compilerTool.Start();

                // Getting error code string if any.
                String errorOutput = compilerTool.StandardError.ReadToEnd();
                String stdOutput = compilerTool.StandardOutput.ReadToEnd();

                // Waiting until the process finishes.
                compilerTool.WaitForExit();

                // Checking compile error if any.
                if (compilerTool.ExitCode != 0)
                    return "Error during generated code compilation:" + CodeGenStringGenerator.ENDL + errorOutput + CodeGenStringGenerator.ENDL + stdOutput;
            }
            finally
            {
                compilerTool.Close();
            }
#else
            try
            {
                // Path to compiler executable.
                String compilerPath = System.IO.Path.Combine(installationDir, @"MinGW\bin\x86_64-w64-mingw32-gcc.exe");
                if (!File.Exists(compilerPath))
                    return "Compiler was not found. Corrupted Starcounter installation.";

                // Creating command line arguments.
                String compilerParams = "\"" + cppFilePath + "\"" + " -nostdlib -eJustEntryPoint_" + uniqueQueryID + " -shared -pipe -O2 -o " + "\"" + dllFilePath + "\"";

                // Compiler executable options.
                compilerTool.StartInfo.FileName = compilerPath;
                compilerTool.StartInfo.UseShellExecute = false;
                compilerTool.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                compilerTool.StartInfo.CreateNoWindow = true;
                compilerTool.StartInfo.WorkingDirectory = savingDir;
                compilerTool.StartInfo.Arguments = compilerParams;
                compilerTool.StartInfo.RedirectStandardError = true;

                // Starting compilation..
                compilerTool.Start();

                // Getting error code string if any.
                String errorOutput = compilerTool.StandardError.ReadToEnd();

                // Waiting until the process finishes.
                compilerTool.WaitForExit();

                // Checking compile error if any.
                if (compilerTool.ExitCode != 0)
                    return "Error during generated code compilation:" + CodeGenStringGenerator.ENDL + errorOutput;
            }
            finally
            {
                compilerTool.Close();
            }
#endif

            // Now loading library into Blue.
            NewCodeGen.NewCodeGen_LoadGenCodeLibrary(uniqueQueryID, dllFilePath);

            // Compilation/loading process went just fine.
            return null;
        }
    }
}