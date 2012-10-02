using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace RapidMinds.BuildSystem.Common.Tools
{
    public class GenerateSerialInformation
    {

        #region Properties

        //public string SerialFile { get; protected set; }
        public string NextSerialInformation { get; protected set; }
        #endregion


        public GenerateSerialInformation(/*string serialFile*/)
        {
//            if (string.IsNullOrEmpty(serialFile)) throw new ArgumentException("Invalid serial file", "serialFile");
//            this.SerialFile = serialFile;
        }

        public void Execute()
        {

            ConsoleColor prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.WriteLine("== Generate Serial information ==");
            Console.ForegroundColor = prevColor;

            //Version nextVersion;
            //if (File.Exists(this.SerialFile))
            //{
            //    StreamReader re = File.OpenText(this.SerialFile);
            //    string serialStr = re.ReadLine();
            //    Version previousVersion = new Version(serialStr);
            //    re.Close();
            //    // Incement build number
            //    nextVersion = new Version(previousVersion.Major, previousVersion.Minor, previousVersion.Build + 1, previousVersion.Revision);
            //}
            //else
            //{
            //    nextVersion = new Version(1, 0, 0, 0);
            //}

            //// Save new version
            //FileInfo fInfo = new FileInfo(this.SerialFile);
            //StreamWriter streamWriter = fInfo.CreateText();
            //streamWriter.WriteLine(nextVersion.ToString());
            //streamWriter.Close();


            // "634449480133226864"

            DateTime d1 = new DateTime(2011, 01, 01);
            DateTime d2 = DateTime.Now;
            TimeSpan t1 = d2 - d1;
            int seconds = (int)t1.TotalSeconds;

            this.NextSerialInformation = seconds.ToString();
            //this.NextSerialInformation = DateTime.Now.Ticks.ToString();

            prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("- Serial information will be : {0} -", this.NextSerialInformation.ToString());
            Console.ForegroundColor = prevColor;


        }

    }

}
