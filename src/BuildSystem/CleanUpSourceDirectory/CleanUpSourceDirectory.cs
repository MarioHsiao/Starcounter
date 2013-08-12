using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using BuildSystemHelper;

namespace CleanUpSourceDirectory
{
    class Program
    {
        static Boolean DeleteTrashFiles(String searchDirectory,
                                        String[] fileTypes,
                                        String[] fileExceptions)
        {
            if (fileTypes == null)
                throw new ArgumentException("'fileTypes' argument is null");

            // Working with each file type.
            foreach (String fileType in fileTypes)
            {
                // Retrieving all matching files recursively.
                String[] allMatchingFiles = Directory.GetFiles(searchDirectory, fileType, SearchOption.AllDirectories);

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

                    // Delete file if its not skipped.
                    if (!ignoreFile)
                    {
                        FileSystemInfo fsi = new FileInfo(file);
                        try
                        {
                            fsi.Attributes = FileAttributes.Normal;
                            fsi.Delete();

                            // Printing the diagnostic message.
                            Console.WriteLine("Removed file '" + fsi.Name + "' from sources directory.");
                        }
                        catch
                        {
                            Console.Error.WriteLine("Can't delete prohibited file '" + fsi.Name + "' from sources directory.");
                            return false;
                        }
                    }
                }
            }

            return true; // No errors found.
        }

        static int Main(string[] args)
        {
            // Printing tool welcome message.
            BuildSystem.PrintToolWelcome("Sources Directory Cleanup");

            // Definitions of files that are skipped from evaluation.
            String[] fileExceptions = { "Starcounter.VisualStudio.2010.dll",
                                        "Starcounter.VisualStudio.2010.pdb",
                                        "Microsoft.mshtml.dll",

                                        // The following exceptions are for Flash player.
                                        "flashplayer10_2_p3_64bit_activex_111710.exe",
                                        "flashplayer10_2_p3_uninstall_win64_111710.exe",
                                        "flashplayer10_2_r2_32bit_activex_012611.exe",
                                        "flashplayer_square_p2_uninstall_win32_092710.exe",
                                        "AxShockwaveFlashObjects.dll",
                                        "ShockwaveFlashObjects.dll"
                                      };

            // Definitions of file types that are searched for.
            String[] fileTypes = { "*.ilk", "*.exe", "*.dll", "*.force",
                                   "*.pdb", "*.suo", "*.ncb", "*.generated.cs",
                                   "*.cache", "*.cs.dll" };

            String searchDirectory = Directory.GetCurrentDirectory();
            if (args.Length > 0)
            {
                searchDirectory = args[0];
            }

            // Writing to error output.
            TextWriter errorOut = Console.Error;
            if (!DeleteTrashFiles(searchDirectory, fileTypes, fileExceptions))
            {
                errorOut.WriteLine("Errors during clean-up. Aborting...");
                return 1;
            }

            errorOut.WriteLine("Build system cleanup utility finished successfully.");
            errorOut.WriteLine("---------------------------------------------------------------");

            return 0;
        }
    }
}
