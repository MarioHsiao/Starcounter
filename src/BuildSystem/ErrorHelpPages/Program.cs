using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErrorHelpPages {

    class Program {
        // Use during testing: per-samuelsson/GoodTimes.wiki.git
        // const string RemoteRepositoryPath = @"/Starcounter/Starcounter.wiki.git";
        const string RemoteRepositoryPath = @"/per-samuelsson/GoodTimes.wiki.git";
        const string RemoteRepositoryGitURL = @"git://github.com" + RemoteRepositoryPath;

        static string ErrorCodesFile;
        static string LocalRepoDirectory;
        static string GitExecutablePath = @"C:\Program Files (x86)\Git\bin\git.exe";
        static Git git;
        static bool CanClone = false;
        static bool Verbose = false;
        static bool Quiet = false;

        static void Main(string[] args) {
            Setup(args);
            UpdateLocalRepository();

            // LocalRepo/ErrorHelpPageTemplate.md
            // LocalRepo/HelpPages

            // Update/create every wiki page.
            // TODO:
            
            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();
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
            if (Verbose) {
                WriteToConsole(status, args);
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
            Console.WriteLine("Usage: ErrorHelpPages [options] <path/to/errorcodes.xml> <path/to/repo>");
        }
    }
}
