using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Errors;
using System.Diagnostics;
using BuildSystemHelper;

namespace ErrorHelpPages {

    class Program {
        const string RemoteRepositoryPath = @"/Starcounter/Starcounter.wiki.git";
        const string RemoteRepositoryGitURL = @"git://github.com" + RemoteRepositoryPath;
        const string RemoteRepositoryHTTPSURL = @"https://github.com" + RemoteRepositoryPath;

        /// <summary>
        /// Provides the name of the template file to use, relative to
        /// the local wiki repository.
        /// </summary>
        const string TemplateFileName = "Help-Page-Template.md";
        /// <summary>
        /// Provides the path where help pages should reside, relative to
        /// the local wiki repository.
        /// </summary>
        const string HelpPageRootPath = "HelpPages";

        static string ErrorCodesFile;
        static string LocalRepoDirectory;
        static string GitExecutablePath = @"C:\Program Files (x86)\Git\bin\git.exe";
        static Git git;
        static bool CanClone = false;
        static bool Verbose = false;
        static bool Quiet = false;
        static bool JustUpdateLocalRepository = false;
        static bool SkipUpdateLocalRepository = false;
        static bool Push = false;
        static int MaxPages = int.MaxValue;
        static bool ForceRun = false;
        static HelpPageTemplate template;

        static void Main(string[] args) {
            try {
                Run(args);
            } catch (Exception generalException) {
                Exit(BuildSystem.LogException(generalException));
            }
        }

        static void Run(string[] args) {
            BuildSystem.PrintToolWelcome("Create/update error help pages");

            Setup(args);
            CheckAllowRun();

            if (!SkipUpdateLocalRepository) {
                UpdateLocalRepository();
            }

            if (!JustUpdateLocalRepository) {
                RunPageGeneration();
            }

            WriteStatus("Done.");
        }

        static void RunPageGeneration() {
            if (!Git.ContainsGitDirectory(LocalRepoDirectory)) {
                Exit(ExitCodes.NotAGitDirectory,
                    string.Format("The local repository \"{0}\" is not a direct parent of a git repository.", LocalRepoDirectory)
                    );
            }

            var helpPagePath = Path.Combine(LocalRepoDirectory, HelpPageRootPath);
            if (!Directory.Exists(helpPagePath)) {
                Directory.CreateDirectory(helpPagePath);
            }

            WriteStatus("Reading the template file...");
            try {
                template = HelpPageTemplate.Read(Path.Combine(LocalRepoDirectory, TemplateFileName));
            } catch (FileNotFoundException) {
                Exit(
                    ExitCodes.TemplateFileNotFound,
                        string.Format(
                        "The help page template file \"{0}\" was not found.",
                        TemplateFileName)
                        );
            } catch (FormatException) {
                Exit(
                    ExitCodes.TemplateBadFormat,
                        string.Format(
                        "The format of the help page template \"{0}\" was not recognized.",
                        TemplateFileName)
                        );
            }

            WriteStatus("Creating help pages that don't exist...");
            try {

                int count = 0;
                using (var file = File.OpenRead(ErrorCodesFile)) {
                    var errors = ErrorFileReader.ReadErrorCodes(file);
                    foreach (var error in errors.ErrorCodes) {
                        var created = CreatePageIfNotExist(error, helpPagePath);
                        if (created) {
                            count++;
                            if (count >= MaxPages) {
                                break;
                            }
                        }
                    }
                }

                if (count > 0) {
                    WriteStatus("Committing {0} new page(s) to repository...", count.ToString());
                    git.Commit(string.Format("-a -m \"Committing {0} new error help page(s)\"", count.ToString()));
                    if (Push) {
                        WriteStatus("Pushing {0} new pages to {1}...", count.ToString(), RemoteRepositoryHTTPSURL);
                        git.Push(RemoteRepositoryHTTPSURL, "master");
                    }

                } else {
                    WriteStatus("No new pages to commit.");
                }

            } catch (ProcessExitException e) {
                // Any problem reported from the git executable we
                // just report to the error stream and let it exit
                // the process instantly.
                Exit(ExitCodes.GitUnexpectedExit, e.Message);
            }
        }

        static bool CreatePageIfNotExist(ErrorCode error, string helpPagePath) {
            var fileName = Path.Combine(helpPagePath, string.Format("{0}-(SCERR{1}).md", error.Name, error.CodeWithFacility));
            if (!File.Exists(fileName)) {
                using (var file = File.CreateText(fileName)) {
                    HelpPage.Write(file, error, template);
                }

                git.Add(fileName);
                return true;
            }

            return false;
        }

        static void CheckAllowRun() {
            // A few conditions/codes that is inherited from
            // the older WikiErrorCodes program. Not sure why
            // they all have to be set just to run, but let's
            // leave it as this for now.

            if (BuildSystem.IsSameExecutableRunning())
                Exit(ExitCodes.AlreadyRunning, "ErrorHelpPages.exe is already running.");

            if (!ForceRun) {

                // This one, we set explicitly on the process, since we don't
                // want it to stop us ever. Maybe it should be set in the build
                // system even, but since I don't know the full effect of that,
                // lets keep it like this.
                Environment.SetEnvironmentVariable("SC_RELEASING_BUILD", "True");
                
                if (BuildSystem.IsPersonalBuild())
                    Exit(ExitCodes.PersonalBuild, "ErrorHelpPages.exe doesn't run during personal builds. Use \"--forcerun\" to force it.");

                if (!BuildSystem.IsReleasingBuild()) {
                    Exit(ExitCodes.NotAReleaseBuild, "ErrorHelpPages.exe doesn't run during non-release builds. Use \"--forcerun\" to force it.");
                }

                if (Environment.GetEnvironmentVariable("SC_UPDATE_ERROR_PAGES") == null) {
                    Exit(ExitCodes.UpdateFlagNotSet,
                        "ErrorHelpPages.exe doesn't run without the 'SC_UPDATE_ERROR_PAGES' environment variable set. Set it or use \"--forcerun\" to force it.");
                }
            }
        }

        static void Setup(string[] args) {
            if (args.Length < 2) {
                Usage();
                Exit(ExitCodes.WrongArguments);
            }

            for (int i = 0; i < args.Length; i++) {
                var arg = args[i];
                if (!arg.StartsWith("--")) {
                    if (++i == args.Length) {
                        Usage();
                        Exit(ExitCodes.WrongArguments);
                    }
                    ErrorCodesFile = arg;
                    LocalRepoDirectory = args[i];
                    break;
                }

                arg = arg.Substring(2).ToLowerInvariant();
                switch (arg) {
                    case "clone":
                        CanClone = true;
                        break;
                    case "verbose":
                        Verbose = true;
                        break;
                    case "quiet":
                        Quiet = true;
                        break;
                    case "gitpath":
                        if (++i == args.Length) {
                            Usage();
                            Exit(ExitCodes.WrongArguments);
                        }
                        GitExecutablePath = args[i];
                        break;
                    case "justpull":
                        JustUpdateLocalRepository = true;
                        break;
                    case "dontpull":
                        SkipUpdateLocalRepository = true;
                        break;
                    case "push":
                        Push = true;
                        break;
                    case "forcerun":
                        ForceRun = true;
                        break;
                    case "debug":
                        Debugger.Launch();
                        break;
                    default:
                        // We allow the syntax --[n] where [n] (without
                        // brackets) specifies the maximum number of pages
                        // to generate or update.
                        if (!int.TryParse(arg, out MaxPages)) {
                            Usage();
                            Exit(ExitCodes.WrongArguments);
                        }
                        break;
                }
            }

            ErrorCodesFile = Path.GetFullPath(ErrorCodesFile);
            if (!File.Exists(ErrorCodesFile)) {
                Exit(ExitCodes.ErrorCodeFileNotFound,
                    string.Format("Error codes file \"{0}\" not found.", ErrorCodesFile));
            }

            GitExecutablePath = Path.GetFullPath(GitExecutablePath);
            if (!File.Exists(GitExecutablePath)) {
                Exit(ExitCodes.GitExecutableNotFound,
                    string.Format("Git executable \"{0}\" not found.", GitExecutablePath));
            }

            LocalRepoDirectory = Path.GetFullPath(LocalRepoDirectory);

            git = new Git(GitExecutablePath);
            git.LocalRepo = LocalRepoDirectory;
            git.OutputDataReceived += (sender, e) => {
                if (e.Data != null) {
                    lock (git) {
                        WriteGitOutput(e.Data);
                    }
                }
            };
            git.ErrorDataReceived += (sender, e) => {
                if (e.Data != null) {
                    lock (git) {
                        WriteGitErrorOutput(e.Data);
                    }
                }
            };
        }

        static void UpdateLocalRepository() {
            WriteStatus("Updating local repository...");
            
            if (!Directory.Exists(LocalRepoDirectory)) {
                if (!CanClone) {
                    Exit(ExitCodes.LocalRepoDirNotFound,
                        string.Format(
                        "Local repository directory \"{0}\" not found. Pass \"--clone\" to allow it to be created.",
                        LocalRepoDirectory)
                        );
                }
                Directory.CreateDirectory(LocalRepoDirectory);
                Clone();

            } else if (!Git.ContainsGitDirectory(LocalRepoDirectory)) {
                if (!CanClone) {
                    Exit(ExitCodes.NotAGitDirectory,
                        string.Format("The local repository \"{0}\" is not a direct parent of a git repository. Pass \"--clone\" to allow it to be created.",
                        LocalRepoDirectory)
                    );
                }
                Clone();
            } else {
                Pull();
            }
        }

        static void Clone() {
            WriteStatus("Cloning \"{0}\" into \"{1}\"", RemoteRepositoryGitURL, LocalRepoDirectory);

            Environment.CurrentDirectory = LocalRepoDirectory;
            try {
                git.Clone(RemoteRepositoryGitURL, ".");
            } catch (ProcessExitException e) {
                Exit(ExitCodes.GitUnexpectedExit, e.Message);
            }
        }

        static void Pull() {
            WriteStatus("Pulling \"{0}\" into \"{1}\"", RemoteRepositoryGitURL, LocalRepoDirectory);

            try {
                git.Pull(RemoteRepositoryGitURL, "");
            } catch (ProcessExitException e) {
                Exit(ExitCodes.GitUnexpectedExit, e.Message);
            }
        }

        static void WriteGitOutput(string status, params string[] args) {
            if (Verbose) {
                WriteToConsole(" git: " + status, args);
            }
        }

        static void WriteGitErrorOutput(string status, params string[] args) {
            try {
                Console.ForegroundColor = ConsoleColor.Red;
                WriteToConsole(" git: " + status, args);
            } finally {
                Console.ResetColor();
            }
        }

        static void WriteStatus(string status, params string[] args) {
            try {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                WriteToConsole(" - " + status, args);
            } finally {
                Console.ResetColor();
            }
        }

        static void WriteToConsole(string status, params string[] args) {
            if (!Quiet) {
                Console.WriteLine(status, args);
            }
        }

        static void Exit(int exitCode, string message = null) {
            if (message != null) {
                try {
                    if (exitCode != 0) {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    Console.WriteLine(message);
                } finally {
                    if (exitCode != 0) {
                        Console.ResetColor();
                    }
                }
            }
            Environment.Exit(exitCode);
        }

        static void Usage() {
            var options = "[--clone] [--verbose | --quiet] [--gitpath <path\\to\\git\\exe\\folder>] [--justpull | --dontpull] [--push] [--forcerun] [--<n> (maxpages)]";
            Console.WriteLine("Usage: ErrorHelpPages {0} <path\\to\\errorcodes.xml> <path\\to\\repo>", options);
        }
    }
}
