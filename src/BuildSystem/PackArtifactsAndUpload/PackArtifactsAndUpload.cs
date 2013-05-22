using BuildSystemHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackArtifactsAndUpload
{
    class PackArtifactsAndUpload
    {
        static Int32 Main(String[] args)
        {
            try
            {
                // Printing tool welcome message.
                BuildSystem.PrintToolWelcome("Pack artifacts and upload them to FTP");

                // Checking if its a nightly or a special build.
                if ((!BuildSystem.IsNightlyBuild()) || (Environment.GetEnvironmentVariable(BuildSystem.UploadToUsFtp) != "True"))
                {
                    Console.WriteLine("Skipping generation of packed artifacts since its a standard or personal build...");
                    return 0;
                }

                // Checking if given directory exists.
                String dirPathToZip = args[0];
                if (!Directory.Exists(dirPathToZip))
                    throw new Exception("Given directory '" + dirPathToZip + "' does not exist.");

                String buildType = args[1];
                if ((buildType != BuildSystem.TestBetaName) &&
                    (buildType != BuildSystem.StableBuildsName) &&
                    (buildType != BuildSystem.NightlyBuildsName) &&
                    (buildType != BuildSystem.CustomBuildsName))
                {
                    throw new Exception("Wrong argument: " + buildType + ".");
                }

                String buildNumber = Environment.GetEnvironmentVariable(BuildSystem.BuildNumberEnvVar);
                String relativeZipPath = Path.Combine(Path.Combine(buildType, buildNumber), buildNumber + ".zip");
                String zipLocalPath = Path.Combine(BuildSystem.LocalBuildsFolder, relativeZipPath);

                // Creating all directories if needed.
                String zipLocalDir = Path.GetDirectoryName(zipLocalPath);
                Directory.CreateDirectory(zipLocalDir);

                // Checking that Zip file does not exist.
                if (File.Exists(zipLocalPath))
                    throw new Exception("Zip file " + zipLocalPath + " already exists!");

                // Compressing given directory.
                ZipFile.CreateFromDirectory(dirPathToZip, zipLocalPath);

                // Start uploading to FTP.
                String relativeZipWithSlashes = relativeZipPath.Replace("\\", "/");
                BuildSystem.UploadFileToFTP(
                    BuildSystem.StarcounterFtpConfigName,
                    zipLocalPath,
                    relativeZipWithSlashes);

                return 0;
            }
            catch (Exception generalException)
            {
                return BuildSystem.LogException(generalException);
            }
        }
    }
}
