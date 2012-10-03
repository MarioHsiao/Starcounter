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

        // Class describing one build system policy.
        class BuildSystemPolicy
        {
            // Incorrect string pattern in file.
            public String[] IncorrectPatterns;

            // File exceptions, e.g. "HtmlAgilityPack.fx.4.0-CP.csproj"
            public String[] FileExceptions;

            // File types, e.g. "*.sln" 
            public String[] FileTypes;

            // Description of this build system policy.
            public String PolicyDescription;
        }

        // All build system policies are defined here.
        static BuildSystemPolicy[] Policies = new BuildSystemPolicy[]
        {
            new BuildSystemPolicy
            {
                IncorrectPatterns = new String[] { "Mixed Platforms" },

                FileExceptions = null, 

                FileTypes = new String[] { "*.sln" },

                PolicyDescription = "Solution files should NOT have any Mixed Platforms configurations."
            },

            new BuildSystemPolicy
            {
                IncorrectPatterns = null,

                FileExceptions = new String[] { "P4API_x64.pdb" },

                FileTypes = new String[] { "*.ilk", "*.force", "*.pdb", "*.suo", "*.ncb", "*.generated.cs", "*.cache", "*.cs.dll" },

                PolicyDescription = "Only binaries that are added to exceptions list can be submitted to source control system."
            },

            new BuildSystemPolicy
            {
                IncorrectPatterns = new String[] { @"\<TargetFrameworkVersion\>v4\.0\<\/TargetFrameworkVersion\>",
                                                   @"\<TargetFrameworkVersion\>v3\.5\<\/TargetFrameworkVersion\>" },

                FileExceptions = new String[] { "HtmlAgilityPack.fx.4.0-CP.csproj" },

                FileTypes = new String[] { "*.csproj" },

                PolicyDescription = "Only .NET v4.5 is allowed for managed projects."
            },

            new BuildSystemPolicy
            {
                IncorrectPatterns = new String[] { @"\<Prefer32Bit\>true\<\/Prefer32Bit\>" },

                FileExceptions = null,

                FileTypes = new String[] { "*.csproj" },

                PolicyDescription = "No true Prefer32Bit flag is allowed in managed projects."
            },

            new BuildSystemPolicy
            {
                IncorrectPatterns = new String[] { @"\<PlatformToolset\>v100\<\/PlatformToolset\>",
                                                   @"\<PlatformToolset\>v90\<\/PlatformToolset\>" },

                FileExceptions = null,

                FileTypes = new String[] { "*.vcxproj" },

                PolicyDescription = "Visual Studio 2012 Build Toolset(v110) should be used for all native projects."
            },
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
            for (Int32 i = 0; i < Policies.Length; i++)
            {
                errorStr = FindIncorrectFiles(searchDirectory, Policies[i].FileTypes, Policies[i].FileExceptions, Policies[i].IncorrectPatterns);
                if (errorStr != null)
                {
                    errorFound = true;
                    errorOut.Write(errorStr);
                    errorOut.WriteLine("Error description: " + Policies[i].PolicyDescription);
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
