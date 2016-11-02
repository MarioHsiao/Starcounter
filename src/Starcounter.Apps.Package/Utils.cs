using Starcounter.Apps.Package.Config;
using Starcounter.Internal;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace Starcounter.Apps.Package {
    public class Utils {

        /// <summary>
        /// Assure path to me a folder with ending slash
        /// </summary>
        /// <remarks>
        /// Author http://stackoverflow.com/questions/20405965/how-to-ensure-there-is-trailing-directory-separator-in-paths
        /// </remarks>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string PathAddBackslash(string path) {

            if (string.IsNullOrEmpty(path)) return path;

            // They're always one character but EndsWith is shorter than
            // array style access to last path character. Change this
            // if performance are a (measured) issue.
            string separator1 = Path.DirectorySeparatorChar.ToString();
            string separator2 = Path.AltDirectorySeparatorChar.ToString();

            // Trailing white spaces are always ignored but folders may have
            // leading spaces. It's unusual but it may happen. If it's an issue
            // then just replace TrimEnd() with Trim(). Tnx Paul Groke to point this out.
            path = path.TrimEnd();

            // Argument is always a directory name then if there is one
            // of allowed separators then I have nothing to do.
            if (path.EndsWith(separator1) || path.EndsWith(separator2))
                return path;

            // If there is the "alt" separator then I add a trailing one.
            // Note that URI format (file://drive:\path\filename.ext) is
            // not supported in most .NET I/O functions then we don't support it
            // here too. If you have to then simply revert this check:
            // if (path.Contains(separator1))
            //     return path + separator1;
            //
            // return path + separator2;
            if (path.Contains(separator2))
                return path + separator2;

            // If there is not an "alt" separator I add a "normal" one.
            // It means path may be with normal one or it has not any separator
            // (for example if it's just a directory name). In this case I
            // default to normal as users expect.
            return path + separator1;
        }

        /// <summary>
        /// Check if a path string is a folder
        /// </summary>
        /// <param name="path"></param>
        /// <returns>True if it's a folder path, otherwise false</returns>
        public static bool IsFolder(string path) {

            if (string.IsNullOrEmpty(path)) return false;

            string separator1 = Path.DirectorySeparatorChar.ToString();
            string separator2 = Path.AltDirectorySeparatorChar.ToString();

            path = path.TrimEnd();

            return (path.EndsWith(separator1) || path.EndsWith(separator2));
        }

        /// <summary>
        /// Create Directory
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Created folders</returns>
        public static string CreateDirectory(string path) {
            string createdBaseFolder = null;

            DirectoryInfo di = new DirectoryInfo(path);

            while (di.Exists == false) {
                createdBaseFolder = di.FullName;
                di = di.Parent;
            }

            Directory.CreateDirectory(path);

            return createdBaseFolder;
        }

        public static ushort GetSystemHttpPort() {

            string personalConfig = Path.Combine(Path.Combine(StarcounterEnvironment.InstallationDirectory, StarcounterEnvironment.Directories.InstallationConfiguration), StarcounterEnvironment.FileNames.InstallationServerConfigReferenceFile);
            PersonalConfiguration config = null;
            string serverDir;

            using (FileStream stream = new FileStream(personalConfig, FileMode.Open)) {
                config = PersonalConfiguration.Deserialize(stream);
                serverDir = config.ServerDir;
            };

            string serverConfig = Path.Combine(serverDir, "Personal.server.config");
            ushort port;
            ReadServerConfiguration(serverConfig, out port);
            return port;
        }

        /// <summary>
        /// Get port number from server configuration file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="port"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static void ReadServerConfiguration(string file, out ushort port) {

            string result;
            port = 0;

            ReadConfigFile(file, "SystemHttpPort", out result);
            if (ushort.TryParse(result, out port)) {
                if (port > IPEndPoint.MaxPort || port < IPEndPoint.MinPort) {
                    throw new InvalidOperationException(string.Format("Invalid port number {0}.", port));
                }
                return;
            }
        }

        /// <summary>
        /// Get a tag value from a xml configuration file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="tag"></param>
        /// <param name="result"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static void ReadConfigFile(string file, string tag, out string result) {

            result = null;

            if (!File.Exists(file)) {
                throw new InvalidOperationException(string.Format("Missing {0} configuration file.", file));
            }

            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines) {

                if (line.StartsWith("//")) continue;

                int startIndex = line.IndexOf("<" + tag + ">", StringComparison.CurrentCultureIgnoreCase);
                if (startIndex != -1) {

                    int len = tag.Length + 2;
                    int endIndex = line.IndexOf("</" + tag + ">", startIndex + len, StringComparison.CurrentCultureIgnoreCase);

                    result = line.Substring(startIndex + len, endIndex - (startIndex + len));
                    return;
                }
            }
            throw new InvalidOperationException(string.Format("Failed to find the <{0}> tag in {1}", tag, file));
        }
    }
}
