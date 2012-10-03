using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Reflection;
using BuildSystemHelper;

namespace CheckBuildSystem
{
    class Program
    {
        static String FindIncorrectFiles(
            String searchDirectory,
            String[] fileTypes,
            String[] fileExceptions,
            String[] incorrectPatterns)
        {
            String incorrString = null;
            if (fileTypes == null)
            {
                throw new ArgumentException("fileTypes argument is null");
            }

            // Working with each file type.
            foreach (String fileType in fileTypes)
            {
                // Retrieving all matching files recursively.
                String[] allMatchingFiles = Directory.GetFiles(searchDirectory, fileType, SearchOption.AllDirectories);

                // Running through each incorrect pattern.
                if (incorrectPatterns != null)
                {
                    foreach (String incorrPattern in incorrectPatterns)
                    {
                        // Running through every incorrect pattern.
                        Regex rgx = new Regex(incorrPattern, RegexOptions.IgnoreCase);
                        foreach (String file in allMatchingFiles)
                        {
                            Boolean ignoreFile = false;

                            // Checking if the file is an exception.
                            if (fileExceptions != null)
                            {
                                foreach (String fileExc in fileExceptions)
                                {
                                    if (file.EndsWith(fileExc, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        // Ignoring this file from exemption.
                                        ignoreFile = true;
                                        break;
                                    }
                                }
                            }

                            // Find matches in file if its not skipped.
                            if (!ignoreFile)
                            {
                                String fileText = File.ReadAllText(file);

                                // Find matches in text.
                                MatchCollection matches = rgx.Matches(fileText);
                                if (matches.Count > 0)
                                {
                                    incorrString += "File \"" + file + "\"" +
                                        Environment.NewLine + "Does not satisfy the build system policy:" +
                                        Environment.NewLine + "File type: " + fileType +
                                        Environment.NewLine + "Found incorrect pattern: " + incorrPattern +
                                        Environment.NewLine +
                                        Environment.NewLine;
                                }
                            }
                        }
                    }
                }
                else if (allMatchingFiles.Length > 0)
                {
                    String badFileList = null;

                    // Checking if there is an exception file.
                    if (fileExceptions != null)
                    {
                        foreach (String matchedFile in allMatchingFiles)
                        {
                            Boolean fileIgnored = false;
                            foreach (String fileExc in fileExceptions)
                            {
                                if (matchedFile.EndsWith(fileExc, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    // Excluding this file.
                                    fileIgnored = true;
                                    break;
                                }
                            }

                            // Checking if file was an exception.
                            if (!fileIgnored)
                            {
                                badFileList += matchedFile + Environment.NewLine;
                            }
                        }
                    }
                    else
                    {
                        foreach (String matchedFile in allMatchingFiles)
                        {
                            badFileList += matchedFile + Environment.NewLine;
                        }
                    }

                    if (badFileList != null)
                    {
                        // File type is banned even without a string pattern.
                        incorrString += "File type \"" + fileType + "\"" +
                            Environment.NewLine + "Must not be submitted into source repository:" +
                            Environment.NewLine + "File list:" + Environment.NewLine +
                            badFileList +
                            Environment.NewLine;
                    }
                }
            }

            return incorrString;
        }

        // Definitions of incorrect text patterns.
        static String[][] incorrPatterns = 
        {
            /*new String[] { @"\<ProjectReference.+\.csproj",
                            @"Configuration\)\|\$\(Platform\)",
                            @"\<\s*OutputPath",
                            @"\<\s*IntermediateOutputPath",
                            @"\<\s*PlatformTarget",
                            @"\<\s*DontImportPostSharp",
                            @"\<\s*TargetFrameworkVersion",
                            @"\<\s*SccProjectName",
                            "AnyCPU"
                            },*/

            new String[] { "Mixed Platforms" },

            null,

            new String[] { @"Threaded\<\/RuntimeLibrary\>",
                            @"Debug\<\/RuntimeLibrary\>" },

            new String[] { @"\<TargetFrameworkVersion\>v4\.0\<\/TargetFrameworkVersion\>",
                            @"\<TargetFrameworkVersion\>v3\.5\<\/TargetFrameworkVersion\>" },

            new String[] { @"\<Prefer32Bit\>true\<\/Prefer32Bit\>" },

            new String[] { @"\<PlatformToolset\>v100\<\/PlatformToolset\>", @"\<PlatformToolset\>v90\<\/PlatformToolset\>" },

            new String[] { "TargetFrameworkProfile" }
        };

        // Definitions of files that are skipped from evaluation.
        static String[][] fileExceptions =
        {
            /*new String[] { "Application.csproj",
                            "HelloWorld.csproj",
                            "BuildLevel0.csproj",
                            "Starcounter.MSBuild.Tasks.csproj",
                            "Starcounter.VisualStudio.2010.csproj",
                            "SCBuildCommon.targets",
                            "LoadAndLatencyClient.csproj",
                            "SQLTestClient.csproj",
                            "SQLTest1Client.csproj",
                            "SQLTest2Client.csproj",
                            "SQLTest3Client.csproj",
                            "PolePositionClient.csproj",
                            "SqlCacheTrasherClient.csproj",
                            "scerrcc.csproj",
                            "Starcounter.Errors.csproj",
                            "Bookkeeping.csproj"
                            },*/

            new String[] { "Starcounter.VisualStudio.2010.sln" }, 

            new String[] { "Starcounter.VisualStudio.2010.pdb",
                            "Blast_s.pdb",
                            "PimpMyBits.WPF.Components.SpeedGrid.pdb",
                            "RapidMinds.Controls.Wpf.SpeedGrid.pdb",
                            "WPFToolkit.Design.pdb",
                            "WPFToolkit.pdb",
                            "WPFToolkit.VisualStudio.Design.pdb"
                            },

            null,

            null,

            null,

            null,

            null
        };

        // Definitions of file types that are searched for.
        static String[][] fileTypes =
        {
            //new String[] { "*.csproj", "*.proj", "*.targets" },

            new String[] { "*.sln" },

            new String[] { "*.ilk", "*.force", "*.pdb", "*.suo", "*.ncb", "*.generated.cs", "*.cache", "*.cs.dll" },

            new String[] { "*.vcxproj" },

            new String[] { "*.csproj" },

            new String[] { "*.csproj" },

            new String[] { "*.vcxproj" },

            new String[] { "*.csproj" }
        };

        // Description of errors.
        static String[] errorDescriptions = 
        {
            //"Direct project references are not allowed. Only concrete project platform can be used (x86 or x64). Output path and target platform version definitions are not allowed.",

            "Yellow solution file should NOT have any Mixed Platforms configurations.",

            "Only binaries that are added to exceptions list can be submitted to source control system.",

            "Visual Studio C Runtime can only be linked dynamically by native projects.",

            "Only .NET v4.5 is allowed for managed projects.",

            "No true Prefer32Bit flag is allowed in managed projects.",

            "Visual Studio 2012 Build Toolset(v110) should be used for native projects.",

            "Managed projects must not target client .NET profile."
        };

        static int Main(string[] args)
        {
            // Printing tool welcome message.
            BuildSystem.PrintToolWelcome("Build System Policy Enforcement Tool");

            String searchDirectory = BuildSystem.GetAssemblyDir();
            if (args.Length > 0)
                searchDirectory = args[0];

            TextWriter errorOut = Console.Error;

            String errorStr = null;
            Boolean errorFound = false;
            for (Int32 i = 0; i < incorrPatterns.Length; i++)
            {
                errorStr = FindIncorrectFiles(searchDirectory, fileTypes[i], fileExceptions[i], incorrPatterns[i]);
                if (errorStr != null)
                {
                    errorFound = true;
                    errorOut.Write(errorStr);
                    errorOut.WriteLine("Error description: " + errorDescriptions[i]);
                    errorOut.WriteLine();
                    errorOut.WriteLine();
                }
            }

            // Checking if any error has been found.
            if (errorFound)
                return 1;

            errorOut.WriteLine("Build system policy enforcement tool finished successfully.");
            errorOut.WriteLine("---------------------------------------------------------------");
            return 0;
        }
    }
}
