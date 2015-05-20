﻿using System;
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

        /// <summary>
        /// Replaces certain parameter in XML file.
        /// </summary>
        /// <param name="pathToXml"></param>
        /// <param name="paramName"></param>
        /// <param name="paramNewValue"></param>
        /// <returns></returns>
        public static bool ReplaceXMLParameterInFile(
              String pathToXml,
              String paramName,
              String paramNewValue) {
            if (!File.Exists(pathToXml))
                return false;

            String fileContents = File.ReadAllText(pathToXml);

            // Searching for the first entry.
            Int32 startIndex = fileContents.IndexOf(paramName);
            if (startIndex <= 0)
                return false;

            // Searching the end of the parameter value.
            Int32 endIndex = fileContents.IndexOf('<', startIndex);
            String strToReplace = fileContents.Substring(startIndex, endIndex - startIndex);

            // Replacing with new parameter value.
            fileContents = fileContents.Replace(strToReplace, paramName + ">" + paramNewValue);

            // Saving modified XML file contents.
            File.WriteAllText(pathToXml, fileContents);

            return true;
        }

    }
}
