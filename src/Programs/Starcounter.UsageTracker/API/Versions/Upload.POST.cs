using System;
using Starcounter;
using Starcounter.Advanced;
using StarcounterApplicationWebSocket.VersionHandler.Model;
using Codeplex.Data;
using Starcounter.Applications.UsageTrackerApp.VersionHandler;
using Starcounter.Internal.Web;
using System.Net;
using StarcounterApplicationWebSocket.VersionHandler;
using System.IO;

namespace Starcounter.Applications.UsageTrackerApp.API.Versions {
    internal class Upload {

        public static void BootStrap(ushort port) {


            // Upload handler
            Handle.POST(port, "/upload", (Request request) => {

                try {

                    // Save Body content to disk
                    string file;
                    SaveBodyToDisk(request, out file);

                    // Add file entry to database
                    string errorMessage;
                    bool result = PackageHandler.AddFileEntryToDatabase(file, out errorMessage);
                    if (result == false) {
                        return new Response() { Uncompressed = HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.BadRequest, null, errorMessage) };
                    }

                    // Trigger unpacker worker
                    VersionHandlerApp.UnpackWorker.Trigger();

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NoContent };
                }
                catch (Exception e) {
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = "Could not handle package. " + e.ToString() };
                }

            });


        }

        /// <summary>
        /// Save file to filesystem
        /// </summary>
        /// <param name="request"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        private static void SaveBodyToDisk(Request request, out string file) {

            VersionHandlerSettings settings = VersionHandlerApp.Settings;

            // Assure upload folder
            if (!Directory.Exists(settings.UploadFolder)) {
                Directory.CreateDirectory(settings.UploadFolder);
            }

            // Create destination file
            file = Path.Combine(settings.UploadFolder, Path.GetRandomFileName());

            // Save request body to file.
            byte[] buffer = request.BodyBytes;
            File.WriteAllBytes(file, buffer);

        }



    }
}
