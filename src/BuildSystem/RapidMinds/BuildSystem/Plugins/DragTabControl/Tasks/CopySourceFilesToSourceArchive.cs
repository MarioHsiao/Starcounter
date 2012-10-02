using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using RapidMinds.BuildSystem.Common.Tools;

namespace Plugin.DragTabControl.Tasks
{
    public class CopySourceFilesToSourceArchive 
    {
        #region Properties

        public Version Version { get; protected set; }
        public string CheckoutPath { get; protected set; }
        public string SourceArchive { get; protected set; }

        #endregion

        public CopySourceFilesToSourceArchive(Version version, string checkoutPath, string sourceArchive)
        {
            if (version == null) throw new ArgumentNullException("version");
            if (string.IsNullOrEmpty(checkoutPath)) throw new ArgumentException("Invalid checkoutPath", "checkoutPath");
            if (string.IsNullOrEmpty(sourceArchive)) throw new ArgumentException("Invalid sourceArchive", "sourceArchive");

            this.Version = version;
            this.CheckoutPath = checkoutPath;
            this.SourceArchive = sourceArchive;

        }

        public void Execute()
        {

            // /F "%teamcity.build.checkoutDir%\DragTabControl\Properties\AssemblyInfo.cs" "%env.BUILD_NUMBER%" /APPEND

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.Error.WriteLine("------ Copy started: Copy source files to source archive ------");
            Console.ResetColor();

            // Copy source files from checkout dir to

            string destination = this.SourceArchive;
            destination = Path.Combine(destination, this.Version.ToString());
            destination = Path.Combine(destination, "RapidMinds");  // We need to keep the "perforce path" intact so the ModifyInstallShield tool will work
            destination = Path.Combine(destination, "DragTabControl");   // We need to keep the "perforce path" intact so the ModifyInstallShield tool will work

            Console.WriteLine("Copy files from {0} to {1}", this.CheckoutPath, destination);


            int filesCopied = 0;

            Utils.CopyDirectory(this.CheckoutPath, destination, ref filesCopied);
              
            // Time Elapsed 00:00:00.72
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Error.WriteLine("Copy succeeded, {0} files copied.", filesCopied);
            Console.ResetColor();


        }




    }
}
