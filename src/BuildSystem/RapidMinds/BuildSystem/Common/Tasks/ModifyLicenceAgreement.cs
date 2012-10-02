using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using RapidMinds.BuildSystem.Common.Tools;
using System.Globalization;

namespace RapidMinds.BuildSystem.Common.Tasks
{
    public class ModifyLicenceAgreement
    {

        public string LicenceAgreementFile { get; protected set; }
        public string SerialInformation { get; protected set; }
        public Version Version { get; protected set; }
        public DateTime VersionCreation { get; protected set; }

        public ModifyLicenceAgreement(string licenceAgreementFile, string serialInformation, Version version, DateTime versionCreation)
        {
            if (string.IsNullOrEmpty(licenceAgreementFile)) throw new ArgumentException("Invalid projectFile", "projectFile");
            if (string.IsNullOrEmpty(serialInformation)) throw new ArgumentException("Invalid serialInformation", "serialInformation");
            if (version == null) throw new ArgumentNullException("version");

            this.LicenceAgreementFile = licenceAgreementFile;
            this.SerialInformation = serialInformation;
            this.Version = version;
            this.VersionCreation = versionCreation;
        }

        public void Execute()
        {

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.Error.WriteLine("------ Modify started: Modifying Licence Agreement ------");
            Console.ResetColor();

            TextReader textReader = new StreamReader(this.LicenceAgreementFile);

            string content = textReader.ReadToEnd();

            textReader.Dispose();


            string userFriendlySerialInformation = Utils.GenerateUserFriendlySerialInformation(this.SerialInformation);

            content = content.Replace("[insert:SERIAL_NUMBER]", userFriendlySerialInformation);
            content = content.Replace("[insert:VERSION]", this.Version.ToString());

            // TODO: Use the actual source checkout date
            content = content.Replace("[insert:DATE]", this.VersionCreation.ToString("d"));


            //// Remove readonly attribute
            FileInfo info = new FileInfo(this.LicenceAgreementFile);
            info.Attributes &= ~FileAttributes.ReadOnly;


            TextWriter textWriter = new StreamWriter(this.LicenceAgreementFile);
            textWriter.Write(content);
            textWriter.Dispose();


            // Time Elapsed 00:00:00.72
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Error.WriteLine("Modify succeeded.");
            Console.ResetColor();


        }




    }
}
