using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AdoptProjectToBuildSystem
{
    class AdoptProjectToBuildSystem
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("You should provide a path to the project that should be adopted.");
                return;
            }

            String projectFileName = args[0];
            if (!projectFileName.EndsWith(".csproj"))
            {
                Console.WriteLine("Project file should be a Visual C# project and have '.csproj' extension.");
                return;
            }

            String[] findStrings = { "|$(Platform)",
                                     "|AnyCPU",
                                     "<OutputPath>bin\\Release\\</OutputPath>",
                                     "<OutputPath>bin\\Debug\\</OutputPath>",
                                     "<Platform Condition=\" '$(Platform)' == '' \">AnyCPU</Platform>",
                                     "<Import Project=\"$(MSBuildBinPath)\\Microsoft.CSharp.targets\" />",
                                     "<Import Project=\"$(MSBuildToolsPath)\\Microsoft.CSharp.targets\" />"};

            String[] replaceStrings = { "",
                                        "",
                                        "",
                                        "",
                                        "",
                                        "<Import Project=\"$(SC_BUILD_SOURCES_PATH)\\SCBuildCommon.targets\" />\n  <Import Project=\"$(MSBuildBinPath)\\Microsoft.CSharp.targets\" />",
                                        "<Import Project=\"$(SC_BUILD_SOURCES_PATH)\\SCBuildCommon.targets\" />\n  <Import Project=\"$(MSBuildToolsPath)\\Microsoft.CSharp.targets\" />"};

            // Reading all data from the project file.
            String projectText = File.ReadAllText(projectFileName);

            // Checking if project has already been converted.
            if (projectText.Contains("$(SC_BUILD_SOURCES_PATH)\\SCBuildCommon.targets"))
            {
                Console.WriteLine("Seems that specified project has already been converted previously.");
                return;
            }

            // Checking if all of the above strings exist in the project file.
            for (int i = 0; i < findStrings.Length; i++)
            {
                if (!projectText.Contains(findStrings[i]))
                {
                    Console.WriteLine("Warning: Project file does not contain required substring: " + findStrings[i]);
                }
                projectText = projectText.Replace(findStrings[i], replaceStrings[i]);
            }

            // Overwriting the project file.
            StreamWriter outputProject = File.CreateText(projectFileName);
            outputProject.Write(projectText);
            outputProject.Close();

            // Diagnostics..
            Console.WriteLine("Project file " + projectFileName + " has been successfully updated for the build system!");
            Console.WriteLine("Now manually add project aliases to 'SCBuildCommon.targets' file following the existing examples in there.");
        }
    }
}
