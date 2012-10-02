//#define NO_DEAMON
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Threading;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Net;
using System.Collections;
using System.Management;
using RapidMinds.BuildSystem.Common;


namespace RapidMinds.BuildSystem.BuildAndDeploy
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Assembly asm = Assembly.GetAssembly(typeof(Program));
                Version asmVersion = asm.GetName().Version;

                object[] attributes = asm.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                string title = string.Empty;
                if (attributes.Length == 1)
                {
                    title = ((AssemblyTitleAttribute)attributes[0]).Title;
                }


                // Print header
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine();
                Console.WriteLine("{0} v{1}.{2} (build {3})", title, asmVersion.Major, asmVersion.Minor, asmVersion.Build);
                Console.WriteLine();
                Console.ResetColor();

                // Change output to english
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");



                // Argument handling
                if (args == null || args.Length == 0 || args.Length > 2 || (args.Length == 2 && !"/CREATE".Equals(args[1], StringComparison.CurrentCultureIgnoreCase)))
                {
                    Console.WriteLine("This tool builds and deploys the RapidMind projects.");
                    Console.WriteLine("Auto increment version number and auto generate a new serial number");
                    Console.WriteLine();
                    Console.WriteLine(" Usage: {0} [[<configuration>] [/CREATE]] [/DEAMON]", Path.GetFileName(asm.Location));
                    Console.ResetColor();
                    Environment.Exit(1);
                    return;
                }


#if NO_DEAMON
#else
                if (args.Length == 1 && "/DEAMON".Equals(args[0], StringComparison.CurrentCultureIgnoreCase))
                {
                    Program.RunAsDeamon(args);
                    return;

                }
#endif
                if (args.Length == 2 && "/CREATE".Equals(args[1], StringComparison.CurrentCultureIgnoreCase))
                {

                    if (File.Exists(args[0]))
                    {
                        throw new InvalidOperationException("Configuration file " + args[0] + " already exists");
                    }
                    else
                    {
                        Configuration defaultConfiguration = new Configuration();
                        defaultConfiguration.SetDefault();
                        Configuration.Save(defaultConfiguration, args[0]);

                        Console.ForegroundColor = ConsoleColor.Green;

                        Console.WriteLine("Configuration file {0} saved.", args[0]);

                    }
                    Console.ResetColor();


                    Environment.Exit(0);
                    return;
                }

#if NO_DEAMON
#else
                Program.AssureRunningDeamon();
#endif
                // Load Configuration
                Configuration configuration = Configuration.Load(args[0]);

                if (string.IsNullOrEmpty(configuration.Plugin))
                {
                    throw new FileNotFoundException("Missing plugin in configuration");
                }
                // Load plugin
                Assembly assembly = Assembly.LoadFile(Path.Combine(Directory.GetCurrentDirectory(), configuration.Plugin));
                Type[] types = assembly.GetTypes();

                foreach (Type type in types)
                {
                    if (typeof(IBuilder).IsAssignableFrom(type))
                    {
                        configuration.Builder = Activator.CreateInstance(type) as IBuilder;
                    }
                }

                if (configuration.Builder == null)
                {
                    throw new EntryPointNotFoundException("Can not find IBuilder class in plugin " + configuration.Plugin);
                }

                //                Type type = assembly.GetType("Plugin.DragTabControl.DragTabControlBuilder");

                // Execute
                //configuration.Builder = new SpeedGridBuilder();    // TODO;

                // Prepare build
                Program.Prepare(configuration.Version, configuration);

                // Build solution
                Program.Build(configuration.Version, configuration);

                // Retrive built version
                VersionInfo versionInfo = Program.GetInstance(configuration.Version, Environment.ExpandEnvironmentVariables(configuration.BinaryArchive));
                if (versionInfo != null)
                {
                    Program.PublishArtifactsToTeamCity(configuration);
                    Thread.Sleep(5000); // TODO: Make sure TeamCity gets time to upload artifacts
                    Program.RemoveInstanceFromBinaryArchive(versionInfo);
                }


                // Trigger Build deamon to start checking
                DeamonProgram.Trigger();

                // Print Result
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine();
                Console.Error.WriteLine("========== Execution: Successfully Executed Version {0} ==========", configuration.Version.ToString());
                Console.ResetColor();


                Environment.Exit(0);
            }
            catch (FileNotFoundException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("{0} : {1}", e.Message, e.FileName);
                Console.ResetColor();
                Environment.Exit(1);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.ResetColor();
                Environment.Exit(1);
            }

        }


        /// <summary>
        /// Publishes artifacts to team city.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        static private void PublishArtifactsToTeamCity(Configuration configuration)
        {
            // http://confluence.jetbrains.net/display/TCD4/Build+Script+Interaction+with+TeamCity#BuildScriptInteractionwithTeamCity-artPublishing

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.WriteLine("== Publish artifacts to TeamCity started ==");
            Console.ResetColor();

            string artifactsPath = Path.Combine(configuration.BinaryArchive, configuration.Version.ToString());

            artifactsPath = Path.Combine(artifactsPath, configuration.SerialInformation);
            artifactsPath = Path.Combine(artifactsPath, "*.*");

            Console.WriteLine("##teamcity[publishArtifacts '{0}']", artifactsPath);

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("- Teamcity informed -");
            Console.ResetColor();

        }

        static public void AssureRunningDeamon()
        {

            Assembly asm = Assembly.GetAssembly(typeof(Program));
            //string name = asm.GetName().Name;

            string fullPath = asm.Location;


            ProcessStartInfo startInfo = new ProcessStartInfo(fullPath, "/Deamon");

            startInfo.WorkingDirectory = Directory.GetCurrentDirectory();

            Process.Start(startInfo);


            // Start deamon
//            Process.Start( name, "/Deamon"); // TODO



            return;
        }

        static public void Prepare(Version version, Configuration configuration)
        {




            configuration.Builder.Prepare(configuration);

            string doneFile = Path.Combine(Environment.ExpandEnvironmentVariables(configuration.SourceArchive), configuration.Version.ToString());
            doneFile = doneFile + ".ok";
            TextWriter tw = new StreamWriter(doneFile);

            string dateTime = String.Format("{0:yyyy-MM-dd HH:mm:dd}", DateTime.Now);

            tw.WriteLine(dateTime);
            tw.Close();

        }

        /// <summary>
        /// Builds the specified version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns></returns>
        static public VersionInfo Build(Version version, Configuration configuration)
        {

            try
            {
                // Important to claim source before building
                if (ClaimSource(Environment.ExpandEnvironmentVariables(configuration.SourceArchive), version))
                {
                    VersionInfo versionInfo = configuration.Builder.Build(version, configuration);

                    // Important to release source
                    ReleaseSource(Environment.ExpandEnvironmentVariables(configuration.SourceArchive), version);

                    return versionInfo;
                }
                else
                {
                }
                return null;
            }
            catch (Exception e)
            {
                // Important to release source
                //ReleaseSource(Environment.ExpandEnvironmentVariables(configuration.SourceArchive), version);

                // If the build system as an bug we will not remove the source. just mark it as "error".
                MarkSourceAsInvalid(Environment.ExpandEnvironmentVariables(configuration.SourceArchive), version);

                // Delete source from source archive
                //Program.DeleteSourceFromSourceArchive(Environment.ExpandEnvironmentVariables(configuration.SourceArchive), version);

                throw e;
            }


        }

        static private bool ClaimSource(string sourceArchive, Version version)
        {

            string statusFile = Path.Combine(sourceArchive, version.ToString());
            statusFile = statusFile + ".ok";

            if (File.Exists(statusFile))
            {
                string result = Path.ChangeExtension(statusFile, ".busy");
                File.Move(statusFile, result);
                return true;
            }

            return false;
        }

        static private void ReleaseSource(string sourceArchive, Version version)
        {

            string statusFile = Path.Combine(sourceArchive, version.ToString());
            statusFile = statusFile + ".busy";

            if (File.Exists(statusFile))
            {
                string result = Path.ChangeExtension(statusFile, ".ok");
                File.Move(statusFile, result);
            }
        }

        static private void MarkSourceAsInvalid(string sourceArchive, Version version)
        {

            string statusFile = Path.Combine(sourceArchive, version.ToString());
            statusFile = statusFile + ".ok";

            if (File.Exists(statusFile))
            {
                string result = Path.ChangeExtension(statusFile, ".error");
                File.Move(statusFile, result);
            }
            else
            {
                statusFile = Path.Combine(sourceArchive, version.ToString());
                statusFile = statusFile + ".busy";

                if (File.Exists(statusFile))
                {
                    string result = Path.ChangeExtension(statusFile, ".error");
                    File.Move(statusFile, result);
                }

            }
        }


        /// <summary>
        /// Delete source from source archive
        /// </summary>
        /// <param name="sourceArchive">The source archive.</param>
        /// <param name="version">The version.</param>
        static public void DeleteSourceFromSourceArchive(string sourceArchive, Version version)
        {
            string sourcePath = Path.Combine(sourceArchive, version.ToString());


            string statusFile = Path.Combine(sourceArchive, version.ToString() + ".ok");
            if (File.Exists(statusFile))
            {
                File.Delete(statusFile);
            }

            statusFile = Path.Combine(sourceArchive, version.ToString() + ".busy");
            if (File.Exists(statusFile))
            {
                File.Delete(statusFile);
            }

            Program.DeleteRecursiveFolder(new DirectoryInfo(sourcePath));
        }

        static private void DeleteRecursiveFolder(DirectoryInfo dirInfo)
        {
            foreach (var subDir in dirInfo.GetDirectories())
            {
                Program.DeleteRecursiveFolder(subDir);
            }

            foreach (var file in dirInfo.GetFiles())
            {
                file.Attributes = FileAttributes.Normal;
                file.Delete();
            }

            dirInfo.Attributes &= ~FileAttributes.ReadOnly;
            dirInfo.Delete();
        }


        /// <summary>
        /// Gets version instance from Binary Archive
        /// </summary>
        /// <remarks>
        /// The VersionFile will be locked (*.lock)
        /// </remarks>
        /// <param name="version">The version.</param>
        /// <returns></returns>
        static public VersionInfo GetInstance(Version version, string binaryArchive)
        {

            string binPath = Path.Combine(binaryArchive, version.ToString());

            if (!Directory.Exists(binPath))
            {
                return null;
            }


            string[] files = Directory.GetFiles(binPath, "VersionInfo_*.xml");

            foreach (string file in files)
            {

                VersionInfo versionInfo = VersionInfo.Load(file);

                // Verify VersionInfo
                if (!VersionInfo.Verify(versionInfo))
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    DeamonProgram.WriteToLog(string.Format("*Warning* Corrupt VersionInfo - {0}", versionInfo.FileName));
                    Console.WriteLine(string.Format("*Warning* Corrupt VersionInfo - {0}", versionInfo.FileName));
                    Console.ResetColor();
                    Program.RepairVersionInfo(versionInfo);
                    continue;
                }

                VersionInfo.LockInstance(versionInfo);
                return versionInfo;



            }


            return null;
        }

        /// <summary>
        /// Removes the instance from binary archive.
        /// </summary>
        /// <param name="versionInfo">The version info.</param>
        static public void RemoveInstanceFromBinaryArchive(VersionInfo versionInfo)
        {

            if (!VersionInfo.Verify(versionInfo))
            {
                Program.RepairVersionInfo(versionInfo);
                throw new InvalidProgramException(string.Format("Corrupt VersionInfo, {0}", versionInfo.FileName));
            }


            string rootPath = Path.GetDirectoryName(versionInfo.FileName);

            string instancePath = Path.Combine(rootPath, versionInfo.IDTailBase64);

            Directory.Delete(instancePath, true);

            File.Delete(versionInfo.FileName);

            string statusFile = rootPath + ".ok";
            if (File.Exists(statusFile))
            {
                File.Delete(statusFile);
            }


            // TODO: Remove version folder if it's empty

            bool IsEmptyDirectory = (Directory.GetFiles(rootPath).Length == 0) && (Directory.GetDirectories(rootPath).Length == 0);

            if (IsEmptyDirectory)
            {
                Directory.Delete(rootPath);
            }




        }

        /// <summary>
        /// Repairs the version info.
        /// </summary>
        static private void RepairVersionInfo(VersionInfo versionInfo)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(string.Format("<<TODO>> RepairVersionInfo <<TODO>>"));
            Console.ResetColor();

            File.Delete(versionInfo.FileName);

        }

        /// <summary>
        /// Runs as deamon.
        /// </summary>
        /// <param name="args">The args.</param>
        static void RunAsDeamon(string[] args)
        {
            DeamonProgram deamon = new DeamonProgram();
            deamon.Start();
        }




    }
}
