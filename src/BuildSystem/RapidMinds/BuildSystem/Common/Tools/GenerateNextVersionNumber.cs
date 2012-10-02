using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace RapidMinds.BuildSystem.Common.Tools
{
    public class GenerateNextVersionNumber
    {
        #region Properties

        public string VersionFile { get; protected set; }
        public Version NextVersion { get; protected set; }
        #endregion


        public GenerateNextVersionNumber(string versionFile)
        {
            if (string.IsNullOrEmpty(versionFile)) throw new ArgumentException("Invalid version file", "versionFile");
            this.VersionFile = versionFile;
        }


        public void Execute()
        {

            ConsoleColor prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.WriteLine("== Generate next version number ==");
            Console.ForegroundColor = prevColor;

            Version nextVersion;
            if (File.Exists(this.VersionFile))
            {
                StreamReader re = File.OpenText(this.VersionFile);
                string versionStr = re.ReadLine();
                Version previousVersion = new Version(versionStr);
                re.Close();
                // Incement build number
                nextVersion = new Version(previousVersion.Major, previousVersion.Minor, previousVersion.Build + 1, previousVersion.Revision);
            }
            else
            {
                nextVersion = new Version(0, 0, 0, 0);
            }

            // Save new version
            FileInfo fInfo = new FileInfo(this.VersionFile);
            StreamWriter streamWriter = fInfo.CreateText();
            streamWriter.WriteLine(nextVersion.ToString());
            streamWriter.Close();


            this.NextVersion = nextVersion;

            prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("- Version number will be : {0} -", this.NextVersion.ToString());
            Console.ForegroundColor = prevColor;



        }


    }
}
