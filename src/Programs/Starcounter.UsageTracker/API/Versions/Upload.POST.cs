using System;
using Starcounter;
using Starcounter.Advanced;
using StarcounterApplicationWebSocket.VersionHandler.Model;
using Codeplex.Data;
using Starcounter.Applications.UsageTrackerApp.VersionHandler;
using System.Net;
using StarcounterApplicationWebSocket.VersionHandler;
using System.IO;
using System.Collections.Generic;

namespace Starcounter.Applications.UsageTrackerApp.API.Versions {
    internal class Upload {

        public static void BootStrap(ushort port) {

            Dictionary<String, FileStream> uploadedFiles = new Dictionary<String, FileStream>();

            Handle.POST(port, "/upload", (Request req) =>
            {
                Random rand = new Random((int)DateTime.Now.Ticks);
                String fileName = "upload-";
                for (Int32 i = 0; i < 5; i++)
                    fileName += rand.Next();

                VersionHandlerSettings settings = VersionHandlerApp.Settings;

                lock (settings)
                {
                    // Assure upload folder.
                    if (!Directory.Exists(settings.UploadFolder))
                        Directory.CreateDirectory(settings.UploadFolder);

                    // Create destination file.
                    FileStream fs = new FileStream(Path.Combine(settings.UploadFolder, fileName), FileMode.Create);
                    uploadedFiles.Add(fileName, fs);
                }

                Response resp = new Response() { Body = fileName };

                return resp;
            });

            Handle.PUT(port, "/upload/{?}", (Request req, String uploadId) =>
            {
                VersionHandlerSettings settings = VersionHandlerApp.Settings;

                lock (settings)
                {
                    // Checking that dictionary contains the upload.
                    if (!uploadedFiles.ContainsKey(uploadId))
                        return 404;

                    Byte[] bodyBytes = req.BodyBytes;
                    UInt64 checkSum = 0;
                    for (Int32 i = 0; i < bodyBytes.Length; i++)
                        checkSum += bodyBytes[i];

                    FileStream fs = uploadedFiles[uploadId];
                    fs.Write(bodyBytes, 0, bodyBytes.Length);
                    if (req["UploadSettings"] == "Final")
                    {
                        fs.Close();
                        uploadedFiles.Remove(uploadId);

                        try
                        {
                            String filePath = Path.Combine(settings.UploadFolder, uploadId);

                            // Add file entry to database.
                            String errorMessage;
                            bool result = PackageHandler.AddFileEntryToDatabase(filePath, out errorMessage);
                            if (result == false)
                                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.BadRequest, Body = errorMessage };

                            // Trigger unpacker worker.
                            VersionHandlerApp.UnpackWorker.Trigger();
                        }
                        catch (Exception e)
                        {
                            return new Response()
                            {
                                StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError,
                                Body = "Failed to handle the package. " + e.ToString()
                            };
                        }
                    }

                    return new Response()
                    {
                        StatusCode = (ushort)System.Net.HttpStatusCode.NoContent,
                        Body = checkSum.ToString()
                    };
                }
            });
        }
    }
}
