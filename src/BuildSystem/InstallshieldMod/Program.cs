using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace InstallshieldMod
{

    class Program
    {
        static void Main(string[] args)
        {

            Assembly asm = Assembly.GetAssembly(typeof(Program));
            Version asmVersion = asm.GetName().Version;

            System.Console.WriteLine("InstallshieldMod Tool v{0}.{1} (build {2})", asmVersion.Major, asmVersion.Minor, asmVersion.Build);

            if (args == null || args.Length == 0)
            {
                Console.WriteLine();
                Console.WriteLine("InstallshieldMod modifies the InstallShield generated project file (*.isl).");
                Console.WriteLine("It tries to set a new version of the product and updates the absolut path's");
                Console.WriteLine();

                Console.WriteLine("Usage: InstallshieldMod <project file> <version> <serialinformation> <perforce root path> <projectname>");
                Environment.Exit(1);
                return;
            }

            if (args == null || args.Length < 5)
            {
                Console.WriteLine("Error: Invalid arguments");
                Console.WriteLine("Usage: InstallshieldMod <project file> <version> <serialinformation> <perforce root path> <projectname>");
                Environment.Exit(1);
                return;
            }


            // File
            string projectFile = args[0];

            if (!File.Exists(projectFile))
            {
                Console.WriteLine("Error: File not found (" + projectFile + ")");
                Environment.Exit(2);
                return;
            }


            // Version
            Version version;
            try
            {
                version = new Version(args[1]);
            }
            catch (Exception e)
            {
                Console.WriteLine("*Error* Invalid version format (" + e.Message + ")");
                Environment.Exit(3);
                return;
            }

            // Serial Information
            string serialInformation;
            try
            {
                serialInformation = args[2];
            }
            catch (Exception e)
            {
                Console.WriteLine("*Error* Invalid Serial Information (" + e.Message + ")");
                Environment.Exit(4);
                return;
            }


            string perforceRootPath = args[3];

            if (string.IsNullOrEmpty(perforceRootPath))
            {
                Console.WriteLine("*Error* Invalid perforceRootPath");
                Environment.Exit(5);
                return;
            }

            string projectName = args[4];

            if (string.IsNullOrEmpty(projectName))
            {
                Console.WriteLine("*Error* Invalid projectName");
                Environment.Exit(5);
                return;
            }
            try
            {

                bool bFixed = ModifyProjectFile(projectFile, version, serialInformation, perforceRootPath, projectName);
                if (bFixed)
                {
                    Console.WriteLine("Done");
                }
                else
                {
                    Console.WriteLine("Skipped. No changes needed");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Aborted.");
                Console.WriteLine("*Error*" + e.Message);
            }


        }

        /// <summary>
        /// Sets the assembly file version.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="searchString">The search string.</param>
        /// <param name="version">The version.</param>
        /// <param name="projectname">projectname exact how folders is names "SpeedGrid", "DragTabControl".</param>
        /// <returns></returns>
        static bool ModifyProjectFile(string file, Version version, string serialInformation, string perforcePath, string projectname)
        {
            if (file == null) throw new ArgumentNullException("Invalid filename");
            if (string.IsNullOrEmpty(perforcePath)) throw new ArgumentNullException("Invalid perforcePath");
            if (version == null) throw new ArgumentNullException("Invalid version");
            if (serialInformation == null) throw new ArgumentNullException("Invalid serialInformation");
            if (string.IsNullOrEmpty(projectname)) throw new ArgumentNullException("Invalid projectname");


            string content = string.Empty;
            string line;
            int modified_installerPaths = 0;
            int modified_ProductVersion = 0;
            int modified_ISYourDataBaseDir = 0;
            int modified_MSIPackageFileName = 0;
            int modified_SetupFileName = 0;

            double procent;
            double prevProcent = 0;

            string projectnameUpperCase = projectname.ToUpper();
            


            string currentFilePerforcePath = GetCurrentFilePerforcePath(file, @"RapidMinds\"+projectname+@"\Resources\Welcome.bmp");



            if (string.IsNullOrEmpty(currentFilePerforcePath))
            {
                throw new Exception("Can not determain the project file perforce path");
            }


            // Fix Rootpath
            if (!perforcePath.EndsWith(Path.DirectorySeparatorChar.ToString()) && !perforcePath.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                perforcePath += Path.DirectorySeparatorChar;
            }

            string ProductVersion = version.ToString();
            string ISYourDataBaseDir = version.ToString();
            string MSIPackageFileName = projectname+"_" + version.ToString();
            string SetupFileName = projectname +"_" + version.ToString();
            string ProductCode = Guid.NewGuid().ToString();
            string SerialInformation = serialInformation;

            Console.WriteLine("Reading project file " + file);

            StreamReader reader = new StreamReader(file);

            while (true)
            {
                line = reader.ReadLine();
                if (line == null) break;

                procent = ((100 * reader.BaseStream.Position)) / (reader.BaseStream.Length);

                if ((procent % 10) == 0 && procent != prevProcent)
                {
                    if (procent == 100)
                    {
                        Console.WriteLine(procent + "%");
                    }
                    else
                    {
                        Console.Write(procent + "%...");
                    }
                    prevProcent = procent;
                }


                line = FixPath(line, currentFilePerforcePath, perforcePath, ref modified_installerPaths);

                if (line == null)
                {
                    continue;
                }



                if (line.IndexOf("<row><td>ProductVersion</td><td>") != -1)
                {
                    content += "\t\t<row><td>ProductVersion</td><td>" + ProductVersion + "</td><td/></row>";
                    modified_ProductVersion++;
                }
                else if (line.IndexOf("<row><td>ISYourDataBaseDir</td><td>" + projectnameUpperCase + "1</td><td>") != -1)
                {
                    content += "\t\t<row><td>ISYourDataBaseDir</td><td>" + projectnameUpperCase + "1</td><td>" + ISYourDataBaseDir + "</td><td/><td>0</td><td/></row>";
                    modified_ISYourDataBaseDir++;
                }
                else if (line.IndexOf("<row><td>Express</td><td>MSIPackageFileName</td><td>") != -1)
                {
                    content += "\t\t<row><td>Express</td><td>MSIPackageFileName</td><td>" + MSIPackageFileName + "</td></row>";
                    modified_MSIPackageFileName++;
                }
                else if (line.IndexOf("<row><td>Express</td><td>SetupFileName</td><td>") != -1)
                {
                    content += "\t\t<row><td>Express</td><td>SetupFileName</td><td>" + SetupFileName + "</td></row>";
                    modified_SetupFileName++;
                }
                else if (line.IndexOf("<row><td>ProductCode</td><td>{") != -1)
                {
                    content += "\t\t<row><td>ProductCode</td><td>{" + ProductCode + "}</td><td/></row>";
                    modified_ProductVersion++;
                }
                else if (line.IndexOf("<row><td>OpenSite_Installation</td><td>226</td><td>") != -1)
                {
                    // Installation
                    // <row><td>OpenWiki_Installation</td><td>226</td><td>WindowsFolder</td><td>[WindowsFolder]explorer.exe http://www.rapidminds.com</td><td/><td>Open RapidMinds WikiPage</td></row>

                    string action = "[WindowsFolder]explorer.exe \"http://www.rapidminds.com/"+projectname+"/Installed.php?serial=" + SerialInformation + "\"";
                    content += "\t\t" + "<row><td>OpenSite_Installation</td><td>226</td><td>WindowsFolder</td><td>" + action + "</td><td/><td>OpenSite Installation</td></row>";
                    modified_ProductVersion++;

                }
                else if (line.IndexOf("<row><td>OpenSite_UnInstallation</td><td>1250</td><td>") != -1)
                {
                    // UnInstallation
                    // <row><td>OpenSite_UnInstallation</td><td>1250</td><td>WindowsFolder</td><td>[WindowsFolder]explorer.exe http://www.rapidminds.com</td><td/><td>OpenSite UnInstallation</td></row>

                    string action = "[WindowsFolder]explorer.exe \"http://www.rapidminds.com/" + projectname + "/Uninstalled.php?serial=" + SerialInformation + "\"";
                    content += "\t\t" + "<row><td>OpenSite_UnInstallation</td><td>1250</td><td>WindowsFolder</td><td>" + action + "</td><td/><td>OpenSite UnInstallation</td></row>";
                    modified_ProductVersion++;
                }

                else if (line.IndexOf("_SPEEDGRID VERSION_") != -1)
                {
                    content += line.Replace("_SPEEDGRID VERSION_", ProductVersion);
                    modified_ProductVersion++;
                }
                else
                {
                    content += line;
                }

                content += Environment.NewLine;

            }

            reader.Close();
            reader.Dispose();


            if (modified_installerPaths > 0)
            {
                Console.WriteLine("Root Paths modified: " + modified_installerPaths);
            }

            if (modified_ProductVersion > 0)
            {
                Console.WriteLine("Product version: " + ProductVersion);
                //if (modified_ProductVersion > 1)
                //{
                //    Console.WriteLine("*Warning* found multiple (" + modified_ProductVersion + ") ProductVersion entries");
                //}

            }


            if (modified_ISYourDataBaseDir > 0)
            {
                Console.WriteLine("Product installation foldername: " + ISYourDataBaseDir);
                if (modified_ISYourDataBaseDir > 1)
                {
                    Console.WriteLine("Product installation folder modified Ok, *Warning* found multiple (" + modified_ISYourDataBaseDir + ") product output folders entries");
                }

            }

            if (modified_MSIPackageFileName > 0)
            {
                Console.WriteLine("MSI Package filename: " + MSIPackageFileName);
                if (modified_MSIPackageFileName > 1)
                {
                    Console.WriteLine("*Warning* found multiple (" + modified_MSIPackageFileName + ") MSIPackageFileName entries");
                }

            }

            if (modified_SetupFileName > 0)
            {
                Console.WriteLine("Setup filename: " + SetupFileName);

                if (modified_SetupFileName > 1)
                {
                    Console.WriteLine("*Warning* found multiple (" + modified_SetupFileName + ") SetupFileName entries");
                }

            }


            if (modified_installerPaths == 0 &&
                 modified_ProductVersion == 0 &&
                  modified_ISYourDataBaseDir == 0 &&
                   modified_MSIPackageFileName == 0 &&
                modified_SetupFileName == 0)
            {
                // No modifications
                return false;
            }



            //// Remove readonly attribute
            FileInfo info = new FileInfo(file);
            info.Attributes &= ~FileAttributes.ReadOnly;

            Console.Write("Saving file...");

            StreamWriter writer = new StreamWriter(file);
            writer.Write(content);
            writer.Close();
            writer.Dispose();

            Console.WriteLine("Done.");

            return true;
        }

        // FIX PATH's
        // Match : \perforce\RapidMinds\ 
        // New Root Path : d:\myperforce\speedgrid
        //
        // 		<row><td>documentation.chm</td><td>Documentation.chm</td><td>DOCUME~1.CHM|Documentation.chm</td><td>0</td><td/><td/><td/><td>1</td><td>c:\perforce\RapidMinds\SpeedGrid\Resources\Documentation.chm</td><td>1</td><td/></row>
        // 		<row><td>readme.txt</td><td>ISX_DEFAULTCOMPONENT11</td><td>readme.txt</td><td>0</td><td/><td/><td/><td>1</td><td>c:\perforce\RapidMinds\SpeedGrid\Resources\readme.txt</td><td>1</td><td/></row>
        //		<row><td>ARPPRODUCTICON.exe</td><td/><td>c:\perforce\RapidMinds\SpeedGrid\Resources\Grid (32x32).ico</td><td>0</td></row>

        static string FixPath(string line, string match, string newRootPath, ref int changes)
        {
            try
            {
                if (string.Compare(match, newRootPath, true) == 0)
                {
                    return line;
                }

                // Extract path
                if (string.IsNullOrEmpty(line)) return line;

                int startSearchPoint = line.IndexOf(match, StringComparison.CurrentCultureIgnoreCase);
                if (startSearchPoint == -1)
                {
                    return line;
                }

                int endPoint = line.IndexOf("<", startSearchPoint, StringComparison.CurrentCultureIgnoreCase);
                if (endPoint == -1)
                {
                    Console.WriteLine("*Error* Could not not find endpoint in matching string (" + line + ")");
                    return null;
                }

                int startPoint = line.LastIndexOf(">", startSearchPoint, StringComparison.CurrentCultureIgnoreCase);
                if (startPoint == -1)
                {
                    Console.WriteLine("*Error* Could not not find startpoint in matching string (" + line + ")");
                    return null;
                }
                startPoint++; // Skipp start chat

                string oldPath = line.Substring(startPoint, endPoint - startPoint);

                // c:\perforce\RapidMinds\SpeedGrid\Resources\Grid (32x32).ico

                // Get path. xxxxxxxx\perforce\RapidMinds\ 

                int relativePathStartPoint = oldPath.IndexOf(match, StringComparison.CurrentCultureIgnoreCase);
                if (relativePathStartPoint == -1) return line;

                string relativePath = oldPath.Substring(relativePathStartPoint + match.Length);

                // Fix Rootpath
                if (relativePath.StartsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    relativePath = relativePath.TrimStart(Path.DirectorySeparatorChar);
                }

                if (relativePath.StartsWith(Path.AltDirectorySeparatorChar.ToString()))
                {
                    relativePath = relativePath.TrimStart(Path.AltDirectorySeparatorChar);
                }

                string newPath = Path.Combine(newRootPath, relativePath);


                line = line.Replace(oldPath, newPath);

                changes++;

            }
            catch (Exception e)
            {
            }


            return line;
        }

        static string GetCurrentFilePerforcePath(string file, string match)
        {
            // "\t\t<row><td>NewBinary1</td><td/><td>c:\\perforce\\RapidMinds\\SpeedGrid\\Resources\\Welcome.bmp</td></row>"
            StreamReader reader = new StreamReader(file);

            string line;
            string foundPath = null;

            while (true)
            {
                line = reader.ReadLine();
                if (line == null) break;

                int pos = line.IndexOf(match, StringComparison.CurrentCultureIgnoreCase);
                if (pos != -1)
                {
                    // c:\\perforce\\RapidMinds\\SpeedGrid\\Resources\\Welcome.bmp
                    string found = ExtractPath(line, pos);
                    if (!string.IsNullOrEmpty(found))
                    {
                        foundPath = found.Substring(0, found.Length - match.Length);
                    }
                }
            }

            reader.Close();
            reader.Dispose();

            return foundPath;
            // 
        }

        static string ExtractPath(string line, int point)
        {

            int endPoint = line.IndexOf("<", point, StringComparison.CurrentCultureIgnoreCase);
            if (endPoint == -1)
            {
                Console.WriteLine("*Error* Could not not find endpoint in matching string (" + line + ")");
                return null;
            }

            int startPoint = line.LastIndexOf(">", point, StringComparison.CurrentCultureIgnoreCase);
            if (startPoint == -1)
            {
                Console.WriteLine("*Error* Could not not find startpoint in matching string (" + line + ")");
                return null;
            }
            startPoint++; // Skipp start chat

            string foundPath = line.Substring(startPoint, endPoint - startPoint);

            return foundPath;
        }

    }


}
