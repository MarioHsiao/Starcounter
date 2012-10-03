using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace RapidMinds.BuildSystem.Common.Tools
{
    public class Utils
    {

        /// <summary>
        /// Copy directory structure recursively
        /// </summary>
        /// <param name="sourceFolder">The source folder.</param>
        /// <param name="destinationFolder">The destination folder.</param>
        /// <param name="filesCopied">The files copied.</param>
        public static void CopyDirectory(string sourceFolder, string destinationFolder, ref int filesCopied)
        {
            String[] files;

            if (destinationFolder[destinationFolder.Length - 1] != Path.DirectorySeparatorChar)
            {
                destinationFolder += Path.DirectorySeparatorChar;
            }
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            files = Directory.GetFileSystemEntries(sourceFolder);
            foreach (string Element in files)
            {
                // Sub directories

                if (Directory.Exists(Element))
                {
                    Utils.CopyDirectory(Element, destinationFolder + Path.GetFileName(Element), ref filesCopied);
                }
                else
                {
                    File.Copy(Element, destinationFolder + Path.GetFileName(Element), true);
                    filesCopied++;
                }
            }
        }


        /// <summary>
        /// Generates the user friendly serial information.
        /// </summary>
        /// <param name="serialInformation">The serial information.</param>
        /// <returns></returns>
        public static string GenerateUserFriendlySerialInformation(string serialInformation)
        {
            if (string.IsNullOrEmpty(serialInformation)) throw new ArgumentException("serialInformation");

            int chunks = 4;
            int maxwidth = 8;

            serialInformation = serialInformation.PadLeft(maxwidth, '0');

            char[] charArray = serialInformation.ToCharArray();
            Array.Reverse(charArray);
            string reversedStr = new string(charArray);

            string newString = string.Empty;

            for (int i = 0; i < reversedStr.Length; i++)
            {

                if (i != 0 && chunks > 0 && (i % chunks) == 0)
                {
                    newString = newString.Insert(0, "-");
                }

                newString = newString.Insert(0, reversedStr[i].ToString());
            }

            return newString;

        }
 

    }
}
