using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Errors;
using System.Diagnostics;

namespace ErrorHelpPages {

    class Program {
        // Use during testing: per-samuelsson/GoodTimes.wiki.git
        // const string RemoteRepositoryPath = @"/Starcounter/Starcounter.wiki.git";
        const string RemoteRepositoryPath = @"/per-samuelsson/GoodTimes.wiki.git";
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
        static HelpPageTemplate template;

        static void Main(string[] args) {
            Setup(args);

            if (!SkipUpdateLocalRepository) {
                UpdateLocalRepository();
            }

            if (JustUpdateLocalRepository)
                return;

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
            int count = 0;
            using (var file = File.OpenRead(ErrorCodesFile)) {
                var errors = ErrorFileReader.ReadErrorCodes(file);
                foreach (var error in errors.ErrorCodes) {
                    var created = CreatePageIfNotExist(error, helpPagePath);
                    if (created) count++;
                }
            }

            if (count > 0) {
                WriteStatus("Committing {0} new page(s) to repository...", count.ToString());
                git.Commit("-a -m \"Committing a set of test pages\"");
                if (Push) {
                    WriteStatus("Pushing {0} new pages to {1}...", count.ToString(), RemoteRepositoryHTTPSURL);
                    git.Push(RemoteRepositoryHTTPSURL, "master");
                }

            } else {
                WriteStatus("No new pages to commit.");
            }

            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();
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

                switch (arg.Substring(2).ToLowerInvariant()) {
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
                    case "debug":
                        Debugger.Launch();
                        break;
                    default:
                        Usage();
                        Exit(ExitCodes.WrongArguments);
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
                lock (git) {
                    WriteGitOutput(e.Data ?? string.Empty);
                }
            };
            git.ErrorDataReceived += (sender, e) => {
                lock (git) {
                    WriteGitErrorOutput(e.Data ?? string.Empty);
                }
            };
        }

        static void UpdateLocalRepository() {
            var cloned = false;

            WriteStatus("Updating local repository...");
            try {
                git.Status("-s");
            } catch (DirectoryNotFoundException) {
                if (!CanClone) {
                    Exit(ExitCodes.LocalRepoDirNotFound,
                        string.Format(
                        "Local repository directory \"{0}\" not found. Pass \"--clone\" to allow it to be created.",
                        LocalRepoDirectory)
                        );
                }
                Directory.CreateDirectory(LocalRepoDirectory);
                Clone();
                cloned = true;

            } catch (ProcessExitException e) {
                // Maybe the directory exist, but it's not a
                // git repository? Check that.
                var gitDir = Path.Combine(LocalRepoDirectory, ".git");
                if (Directory.Exists(gitDir)) {
                    Exit(ExitCodes.GitUnexpectedExit, e.Message);
                }

                // Clone if allowed.
                if (!CanClone) {
                    Exit(ExitCodes.LocalRepoDirNotFound,
                        string.Format(
                        "Local repository directory \"{0}\" is not a git repository. Pass \"--clone\" to allow it to be created.",
                        LocalRepoDirectory)
                        );
                }
                Clone();
                cloned = true;
            }

            if (!cloned) {
                Pull();
            }
        }

        static void Clone() {
            WriteStatus("Cloning \"{0}\" into \"{1}\"", RemoteRepositoryGitURL, LocalRepoDirectory);

            Environment.CurrentDirectory = LocalRepoDirectory;
            try {
                git.Clone(RemoteRepositoryGitURL, ".", "--progress");
            } catch (ProcessExitException e) {
                Exit(ExitCodes.GitUnexpectedExit, e.Message);
            }
        }

        static void Pull() {
            WriteStatus("Pulling \"{0}\" into \"{1}\"", RemoteRepositoryGitURL, LocalRepoDirectory);

            try {
                git.Pull(RemoteRepositoryGitURL, "", "--progress");
            } catch (ProcessExitException e) {
                Exit(ExitCodes.GitUnexpectedExit, e.Message);
            }
        }

        static void WriteGitOutput(string status, params string[] args) {
            if (Verbose) {
                WriteToConsole(status, args);
            }
        }

        static void WriteGitErrorOutput(string status, params string[] args) {
            try {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                WriteToConsole(status, args);
            } finally {
                Console.ResetColor();
            }
        }

        static void WriteStatus(string status, params string[] args) {
            WriteToConsole(status, args);
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
            Console.WriteLine("Usage: ErrorHelpPages [options] <path/to/errorcodes.xml> <path/to/repo>");
        }
    }
}
