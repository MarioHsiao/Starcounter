using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using System.Net;
using System.Diagnostics;
using Starcounter.Internal;
using Starcounter.Internal.Web;
using Starcounter.Administrator.API.Utilities;
using Starcounter.Administrator.API.Handlers;
using Starcounter.Server.Rest.Representations.JSON;
using Starcounter.Server.Rest;
using Starcounter.CommandLine;
using System.IO;
using Starcounter.Rest.ExtensionMethods;
using System.Collections.Generic;
using Starcounter.Administrator.Server.Utilities;
using System.Globalization;
using System.Linq;

namespace Starcounter.Administrator.Server.Handlers {
    internal static partial class StarcounterAdminAPI {

        /// <summary>
        /// Register CollationFiles GET
        /// </summary>
        public static void CollationFiles_GET(ushort port, IServerRuntime server) {

            // Get a list of all running Executables
            // Example response
            //{
            // "Items": [
            //      {
            //          "File": "TurboText_sv-SE_3.dll",
            //          "Description": "Swedish",
            //      }
            //  ]
            //}
            Handle.GET("/api/admin/servers/{?}/collationfiles", (string name, Request req) => {

                try {

                    IList<CollationFile> collations = GetAvailableCollations();

                    CollationsFiles collationsFiles = new CollationsFiles();

                    foreach (CollationFile collation in collations) {
                        var collationFileJson = collationsFiles.Items.Add();
                        collationFileJson.File = collation.Name;
                        collationFileJson.Description = collation.CultureInfo.DisplayName;
                    }

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = collationsFiles.ToJsonUtf8() };

                }
                catch (Exception e) {
                    return RestUtils.CreateErrorResponse(e);
                }
            });
        }


        /// <summary>
        /// Get available collations on the server
        /// </summary>
        /// <returns>List of available collation files</returns>
        private static IList<CollationFile> GetAvailableCollations() {

            IList<CollationFile> collationItems = new List<CollationFile>();

            // First checking the user-wide installation directory.
            String scInstDir = Environment.GetEnvironmentVariable(Starcounter.Internal.StarcounterEnvironment.VariableNames.InstallationDirectory, EnvironmentVariableTarget.User);

            if (scInstDir == null)
                scInstDir = Environment.GetEnvironmentVariable(Starcounter.Internal.StarcounterEnvironment.VariableNames.InstallationDirectory, EnvironmentVariableTarget.Machine);

            // Then checking the system-wide installation directory.
            if (scInstDir == null) {
                return null;
            }

            // TurboText_nb-NO_3.dll
            string searchPattern = StarcounterEnvironment.FileNames.CollationFileNamePrefix+"_*.dll";

            string[] files = Directory.GetFiles(scInstDir, searchPattern);

            foreach (string file in files) {

                string fileName = System.IO.Path.GetFileName(file);
                string name = fileName.Substring(10);

                int pos = name.IndexOf('_');
                if (pos != -1) {
                    int pos2 = name.IndexOf('.');
                    if (pos2 != -1) {
                        string versionStr = name.Substring(pos + 1, pos2 - pos - 1);

                        int version;

                        if (int.TryParse(versionStr, out version) == true) {
                            name = name.Substring(0, pos);
                            try {
                                CultureInfo info = CultureInfo.GetCultureInfo(name);

                                CollationFile existingItem = null;
                                try {
                                    existingItem = collationItems.First(item => string.Compare(item.CultureInfo.Name, info.Name, true) == 0);
                                }
                                catch (Exception) { }

                                if (existingItem == null || version > existingItem.Version) {
                                    if (existingItem != null) {
                                        collationItems.Remove(existingItem);
                                    }

                                    CollationFile cItem = new CollationFile();
                                    cItem.CultureInfo = info;
                                    cItem.Name = fileName;
                                    cItem.Version = version;

                                    collationItems.Add(cItem);
                                }
                            }
                            catch (Exception) {
                                // Culture is not supported
                            }

                        }
                    }
                }
                else {
                    // Invalid filename
                }
            }
            return collationItems;
        }


        /// <summary>
        /// CollationFile
        /// </summary>
        class CollationFile {
            public string Name;
            public int Version;
            public CultureInfo CultureInfo;
        }
    }
}
