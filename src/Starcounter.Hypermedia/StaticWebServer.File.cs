// ***********************************************************************
// <copyright file="StaticWebServer.File.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using BizArk.Core.Util;

namespace Starcounter.Internal.Web {
    /// <summary>
    /// Class StaticWebServer
    /// </summary>
    public partial class StaticWebServer {
        private Dictionary<string, FileSystemWatcher> watchedPaths;

        /// <summary>
        /// Contains the directories that may contain web resources such as .html files and other assets. 
        /// </summary>
        internal List<string> workingDirectories = new List<string>();

        /// <summary>
        /// Object used for locking.
        /// </summary>
        private object lockObject = new object();

        /// <summary>
        /// Reads the file system to find the resource addressed by an uri without using any cached version.
        /// </summary>
        /// <param name="cached">If there is an existing cache entry, it is provided here. The cache entry may
        /// contain compressed or uncompressed versions whereas the request only targets one of these versions. This means
        /// that a cache item may be built up by multiple calls to this method, each with a different version requested. If the
        /// provided cached item already contains the version requested, the file is still read and the cached version is
        /// overwritten.</param>
        /// <param name="relativeUri">The uri without the server domain</param>
        /// <param name="req">The Starcounter session id</param>
        /// <returns>A cacheable resource item with at least one version (compressed or uncompressed).</returns>
        private Response GetFileResource(Response cached, string relativeUri, Request req) {
            // TODO:
            // The sent in cached response is not used today. We should change the cache so we 
            // have different responses for compressed and uncompressed

            bool didCompress;
            bool shouldBeCached;
            bool shouldCompress;
            byte[] compressed = null;
            byte[] payload;
            HttpStatusCode statusCode;
            int contentLength;
            Response response;
            string fileExtension = null;
            string mimeType;
            string dir = null;
            string fileName = null;
            
            req.Debug(" (FILE ACCESS) " + relativeUri);

            statusCode = HttpStatusCode.OK;
            shouldBeCached = !Configuration.Current.FileServer.DisableAllCaching;

            if (workingDirectories.Count == 0) {
                statusCode = HttpStatusCode.NotFound;
                mimeType = MimeTypeHelper.MimeTypeAsString(MimeType.Text_Plain);
                payload = Encoding.UTF8.GetBytes(
                    "Uri could not be resolved. No directories added for serving static files.");
            } else {
                if (!ReadFile(relativeUri, out dir, out fileName, out fileExtension, out payload)) {
                    statusCode = HttpStatusCode.NotFound;
                    mimeType = MimeTypeHelper.MimeTypeAsString(MimeType.Text_Plain);
                    payload = Encoding.UTF8.GetBytes(String.Format("Error 404: File {0} not found", relativeUri + "."));
                } else {
                    mimeType = MimeMap.GetMimeType(fileExtension);
                }
            }

            response = Response.FromStatusCode((int)statusCode);
            contentLength = (payload != null) ? payload.Length : -1;
            if (mimeType.StartsWith("text/"))
                mimeType += ";charset=utf-8";
            response.ContentType = mimeType;

            shouldCompress = req.IsGzipAccepted;
            didCompress = false;
            if (contentLength != -1 && shouldCompress) {
                compressed = Compress(payload);
                didCompress = compressed.Length + 100 < payload.Length; // Don't use compress version if the difference is too small
//                Console.WriteLine(String.Format("Compressed({0})+100 < Uncompressed({1})", compressed.Length, payload.Length));
                if (didCompress) {
                    req.Debug(" (compressing)"); // String.Format("Compressed({0})+100 < Uncompressed({1})", compressed.Length, payload.Length));
                    contentLength = compressed.Length;
                    payload = compressed;
                    response.ContentEncoding = "gzip";
                } else {
                    req.Debug(" (not-worth-compressing)"); // String.Format("Compressed({0})+100 < Uncompressed({1})", compressed.Length, payload.Length));
                    response.WorthWhileCompressing = false;
                }
            }

            if (statusCode != HttpStatusCode.OK)
                response.CacheControl = "no-cache";
            else if (Configuration.Current.FileServer.DisableAllCaching)
                response.CacheControl = "private,max-age=0";
            else 
                response.CacheControl = "public,max-age=31536000";

            response.ContentLength = contentLength;
            response.Content = payload;

            if (statusCode == HttpStatusCode.OK && shouldBeCached && cached == null) {
                response.Uris.Add(relativeUri);
                string path = Path.Combine(dir,  fileName) + fileExtension;
                string fileSignature = path.ToUpper();
                response.FilePath = fileSignature;
                response.FileDirectory = dir;
                response.FileName = fileName + fileExtension;
                response.FileExists = (statusCode != HttpStatusCode.NotFound);
                if (response.FileExists) {
                    response.FileModified = File.GetLastWriteTime(path);
                }
                cacheOnUri[relativeUri] = response;
                cacheOnFilePath[fileSignature] = response;
                WatchChange(dir, fileName + fileExtension);
            }

            return response;
        }

        /// <summary>
        /// Searches for file specified in the relative uri and returns a bytearray containing
        /// the contents. 
        /// </summary>
        /// <param name="relativeUri"></param>
        /// <param name="fileExtension"></param>
        /// <param name="payload"></param>
        /// <returns>True if the file was found, false otherwise.</returns>
        private bool ReadFile(string relativeUri, out string dir, out string fileName, out string fileExtension, out byte[] payload) {
            int len;
            
            for (int t = 0; t < workingDirectories.Count; t++) {
                ParseFileSpecifier(workingDirectories[t], relativeUri, out dir, out fileName, out fileExtension);

                try {
                    // TODO: 
                    // Dont use exceptions. Check instead if directories and files exists.
                    FileStream f = FileOpenAlternative(dir, fileName, ref fileExtension);
                    len = (int)f.Length;

                    // Check for UTF-8 byte order mark (BOM) offset
                    if (len >= 3) {
                        int utf8Size = 3;                  // UTF 8 byte check
                        byte[] bom = new byte[utf8Size];   // allocate place for UTF-8 check
                        f.Read(bom, 0, utf8Size);
                        if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) // UTF-8 {
                            len -= utf8Size;               // Adjust the payload size without the 'bom'
                        else {
                            f.Position -= utf8Size;        // Reset the filestream position
                        }
                    }

                    payload = new byte[len];
                    f.Read(payload, 0, (int)len);
                    f.Close();
                    return true;
                } 
                // The default return value is false so we don't need to do anything special in each 
                // exceptionblock, just make sure that we catch it and return normally.
                catch (FileNotFoundException) { } 
                catch (DirectoryNotFoundException) { } 
                catch (DriveNotFoundException) { }
            }
            dir = null;
            fileName = null;
            fileExtension = null;
            payload = null;
            return false;
        }

        /// <summary>
        /// Called to monitor live changes to static resources. If a file is changed,
        /// the cache is invalidated. This allows the web server to always server fresh
        /// versions of any resource.
        /// </summary>
        /// <param name="dir">The directory the file resides in.</param>
        /// <param name="fileName">Name of the file.</param>
        private void WatchChange(string dir, string fileName) {
            FileSystemWatcher fsw;
            string fileSpecifier = dir + "\\" + fileName;
            if (!watchedPaths.TryGetValue(fileSpecifier, out fsw)) {
                if (Directory.Exists(dir)) {
                    fsw = new FileSystemWatcher(dir);
                    fsw.InternalBufferSize = 64 * 1024;
                    fsw.Filter = fileName;
                    fsw.IncludeSubdirectories = false;
                    fsw.Changed += new FileSystemEventHandler(FileHasChanged);
                    fsw.Deleted += new FileSystemEventHandler(FileHasChanged);
                    fsw.Renamed += new RenamedEventHandler(FileIsRenamed);
                    fsw.EnableRaisingEvents = true;
                } else {
                    fsw = null;
                }
                watchedPaths[fileSpecifier] = fsw;
            }
        }

        /// <summary>
        /// Removes all eventlisteners and disposes the watcher.
        /// </summary>
        /// <param name="fsw"></param>
        private void ClearWatchedChange(FileSystemWatcher fsw) {
            fsw.EnableRaisingEvents = false;
            fsw.Changed -= new FileSystemEventHandler(FileHasChanged);
            fsw.Deleted += new FileSystemEventHandler(FileHasChanged);
            fsw.Renamed += new RenamedEventHandler(FileIsRenamed);
            fsw.Dispose();
        }

        /// <summary>
        /// Clears the watched parts.
        /// </summary>
        private void ClearWatchedParts() {
            if (watchedPaths != null) {
                foreach (var watcher in watchedPaths.Values) {
                    ClearWatchedChange(watcher);
                }
            }
            watchedPaths = new Dictionary<string, FileSystemWatcher>();
        }

        /// <summary>
        /// Triggered when a file in a watched directory is renamed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void FileIsRenamed(object sender, RenamedEventArgs e) {
            string fileSignature = e.OldFullPath.ToUpper();
            DecacheByFilePath(fileSignature);
        }

        /// <summary>
        /// Triggered when a file in a watched directory is changed or deleted.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileHasChanged(object sender, FileSystemEventArgs e) {
            string fileSignature = e.FullPath.ToUpper();
            DecacheByFilePath(fileSignature);
        }

        /// <summary>
        /// Removes the specified file from the cache
        /// </summary>
        /// <param name="fileSignature">The file to remove</param>
        private void DecacheByFilePath(string fileSignature) {
            Response cached;

            // Locking because execution is done in separate thread.
            lock (lockObject) {
                if (cacheOnFilePath.TryGetValue(fileSignature, out cached)) {
                    foreach (var uri in cached.Uris) {
                        Console.WriteLine("(decache uri) " + uri);
                        cacheOnUri.Remove(uri);
                    }
                    Console.WriteLine("(decache file) " + fileSignature);
                    cacheOnFilePath.Remove(fileSignature);
                }
            }
        }

        /// <summary>
        /// Tries to open the file with the specified name in the specified directory.
        /// If not succesful and the fileExtension parameter is not specified a default 
        /// extension is
        /// </summary>
        /// <param name="dir">The directory to look for the file</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="fileExtension">The file extension.</param>
        /// <returns></returns>
        private FileStream FileOpenAlternative(string dir, string fileName, ref string fileExtension) {
            bool hasExtension = !string.IsNullOrEmpty(fileExtension);
            string filePathWOExt = Path.Combine(dir, fileName);
            string filePath;

            try {
                filePath = filePathWOExt;
                if (hasExtension)
                    filePath += fileExtension; 
                return File.OpenRead(filePath);
            } catch (Exception) {
                if (!hasExtension) {
                    fileExtension = ".html";
                    filePath = filePathWOExt + fileExtension;
                    return File.OpenRead(filePath);
                }
                throw;
            }
        }

        /// <summary>
        /// Parses the file specifier.
        /// </summary>
        /// <param name="serverPath">The server path.</param>
        /// <param name="relativeUri">The relative URI.</param>
        /// <param name="directory">The directory.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="fileExtension">The file extension.</param>
        internal void ParseFileSpecifier(string serverPath, string relativeUri, out string directory, out string fileName, out string fileExtension) {
            if (!relativeUri.StartsWith("/")) {
                Console.WriteLine(String.Format("Illegal URI for static resource: {0}", relativeUri));
                directory = null;
                fileExtension = null;
                fileName = null;
                return; // Only local uris are supported
            }

            var decodedUri = HttpUtility.UrlDecode(relativeUri);
            string[] segments = decodedUri.Split('?');
            string[] components = segments[0].Split('/');

            directory = serverPath;
            for (int t = 0; t < components.Length - 1; t++) {
                string component = components[t];
                if (component != "")
                    directory += @"\" + component;
            }
            string fileNameWithExtension = components[components.Length - 1];
            components = fileNameWithExtension.Split('.');
            if (components.Length > 1) {
                fileExtension = "." + components[components.Length - 1];
                fileName = components[0];
                for (int i = 1; i < components.Length - 1; i++) {
                    fileName += "." + components[i];
                }
            } else {
                fileName = fileNameWithExtension;
                fileExtension = "";
            }
            //    fileSpecifier = directory + @"\" + fileNameWithExtension;
        }

        ///// <summary>
        ///// Encodes to base64.
        ///// </summary>
        ///// <param name="toEncode">To encode.</param>
        ///// <returns>System.String.</returns>
        //private static string EncodeToBase64(string toEncode) {
        //    byte[] toEncodeAsBytes
        //          = System.Text.ASCIIEncoding.ASCII.GetBytes(toEncode);
        //    string returnValue
        //          = System.Convert.ToBase64String(toEncodeAsBytes);
        //    return returnValue;
        //}
    }
}

