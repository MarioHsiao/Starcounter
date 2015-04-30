using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Administrator.Server.Utilities {
    public class Utils {

        /// <summary>
        /// Create Directory structure 
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Created root base folder</returns>
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

        /// <summary>
        /// Check if directory is empty
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public static bool IsDirectoryEmpty(string folder) {
            return !Directory.EnumerateFileSystemEntries(folder).Any();
        }

    }
}
