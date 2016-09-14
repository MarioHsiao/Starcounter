// ***********************************************************************
// <copyright file="StaticWebServer.File.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        /// <summary>
        /// Watched file paths.
        /// </summary>
        private Dictionary<string, FileSystemWatcher> watchedPaths_;

        /// <summary>
        /// Contains the directories that may contain web resources such as .html files and other assets. 
        /// </summary>
        internal List<string> fileDirectories_ = new List<string>();

        /// <summary>
        /// Application names.
        /// </summary>
        internal List<string> appNames_ = new List<string>();

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
            string mimeType = MimeTypeHelper.MimeTypeAsString(MimeType.Text_Plain);
            string dir = null;
            string fileName = null;
            
            //Debug(" (FILE ACCESS) " + relativeUri);

            statusCode = HttpStatusCode.OK;
            shouldBeCached = !Configuration.Current.FileServer.DisableAllCaching;

            if (fileDirectories_.Count == 0) {

                statusCode = HttpStatusCode.NotFound;
                mimeType = MimeTypeHelper.MimeTypeAsString(MimeType.Text_Plain);
                payload = Encoding.UTF8.GetBytes(
                    "Uri could not be resolved. No directories added for serving static files.");
            } else {

                // Trying to read the file contents.
                if (!ReadFile(relativeUri, out dir, out fileName, out fileExtension, out payload)) {

                    //Debug("Could not find " + relativeUri);

                    return new Response() {
                        StatusCode = 404,
                        StatusDescription = "Not Found",
                        Body = String.Format("Error 404: Resource \"{0}\" not found.", relativeUri),
                        HandlingStatus = HandlerStatusInternal.ResourceNotFound
                    };

                } else {

                    mimeType = MimeMap.GetMimeType(fileExtension);
                }
            }

            // Checking if we couldn't find suitable mime-type.
            if (mimeType == null) {
                mimeType = "text/plain";
            }

            response = Response.FromStatusCode((int)statusCode);
            contentLength = (payload != null) ? payload.Length : -1;
            if (mimeType.StartsWith("text/")) {
                mimeType += ";charset=utf-8";
            }

            response.ContentType = mimeType;

            shouldCompress = req.IsGzipAccepted;

            didCompress = false;

            if (shouldCompress && statusCode == HttpStatusCode.OK && contentLength != -1) {

                compressed = Compress(payload);
                didCompress = compressed.Length + 100 < payload.Length; // Don't use compress version if the difference is too small
//                Debug(String.Format("Compressed({0})+100 < Uncompressed({1})", compressed.Length, payload.Length));

                if (didCompress) {
                    //Debug(" (compressing)"); // String.Format("Compressed({0})+100 < Uncompressed({1})", compressed.Length, payload.Length));
                    contentLength = compressed.Length;
                    payload = compressed;
                    response.ContentEncoding = "gzip";
                    response.Headers["Vary"] = "Accept-Encoding";
                } else {
                    //Debug(" (not-worth-compressing)"); // String.Format("Compressed({0})+100 < Uncompressed({1})", compressed.Length, payload.Length));
                }
            }

            if (statusCode != HttpStatusCode.OK) {
                response.CacheControl = "no-cache";
            } else {
                response.CacheControl = "public,max-age=0,must-revalidate";
            }

            response.ContentLength = contentLength;
            response.BodyBytes = payload;

            if (statusCode == HttpStatusCode.OK && shouldBeCached && cached == null) {

                if (response.Uris == null)
                    response.Uris = new List<string>();

                response.Uris.Add(relativeUri);
                string path = Path.Combine(dir,  fileName) + fileExtension;
                response.FilePath = path;
                response.FileDirectory = dir;
                response.FileName = fileName + fileExtension;
                response.FileExists = (statusCode != HttpStatusCode.NotFound);

                if (response.FileExists) {

                    // Saving modification date in RFC 1123 format.
                    response.FileModifiedDate = File.GetLastWriteTime(path).ToUniversalTime().ToString("r");

                    String mt = response.FileModifiedDate;
                    String ims = req.Headers["If-Modified-Since"];

                    response.Headers["Last-Modified"] = mt;

                    // Checking if X-File-Path should be added.
                    if (StarcounterEnvironment.XFilePathHeader) {
                        response.Headers["X-File-Path"] = response.FilePath;
                    }

                    // Check if resource is not modified.
                    if (mt.Equals(ims)) {

                        Response resp = new Response() {
                            StatusCode = 304,
                            StatusDescription = "Not Modified"
                        };

                        resp.Headers["Cache-Control"] = "public,max-age=0,must-revalidate";
                        resp.Headers["Last-Modified"] = mt;

                        // Checking if X-File-Path should be added.
                        if (StarcounterEnvironment.XFilePathHeader) {
                            if (null != response.FilePath) {
                                resp.Headers["X-File-Path"] = response.FilePath;
                            }
                        }

                        return resp;
                    }
                }

                // Creating byte representation of the response.
                response.ConstructFromFields(req, null);
                cacheOnUri_[relativeUri] = response;

                // TODO: 
                // Should cacheOnFilePath really be the actual responses? Isn't it better to just
                // contain a list of URI's in the cacheOnUri dictionary so we can refresh/remove 
                // all entries since we might have different responses for different URI's pointing to
                // the same physical file?
                Response existing;
                if (cacheOnFilePath_.TryGetValue(response.FilePath, out existing)) {

                    if (existing.Uris == null)
                        existing.Uris = new List<string>();

                    existing.Uris.Add(relativeUri);

                } else {

                    cacheOnFilePath_[response.FilePath] = response;
                    WatchChange(dir, fileName + fileExtension);
                }
            }

            return response;
        }

        /// <summary>
        /// Tries to find file in specified directory and returns file contents as byte array.
        /// </summary>
        Boolean ReadFileInDirectory(
            string relativeUri, 
            String dirPath, 
            out string dir, 
            out string fileName, 
            out string fileExtension, 
            out byte[] payload) {

            ParseFileSpecifier(dirPath, relativeUri, out dir, out fileName, out fileExtension);

            FileStream f = FileOpenAlternative(dir, fileName, ref fileExtension);

            if (f != null) {

                int len = (int)f.Length;

                // Check for UTF-8 byte order mark (BOM) offset
                if (len >= 3) {
                    int utf8Size = 3;                  // UTF 8 byte check
                    byte[] bom = new byte[utf8Size];   // allocate place for UTF-8 check
                    f.Read(bom, 0, utf8Size);
                    if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) {
                        len -= utf8Size;               // Adjust the payload size without the 'bom'
                    } else {
                        f.Position -= utf8Size;        // Reset the filestream position
                    }
                }

                payload = new byte[len];
                f.Read(payload, 0, (int)len);
                f.Close();

                return true;
            }

            dir = null;
            fileName = null;
            fileExtension = null;
            payload = null;

            return false;
        }

        /// <summary>
        /// Searches for file specified in the relative uri and returns a byte array containing
        /// the contents. 
        /// </summary>
        private bool ReadFile(
            string relativeUri,
            out string dir,
            out string fileName,
            out string fileExtension,
            out byte[] payload) {

            // Going through every directory.
            for (int t = (fileDirectories_.Count - 1); t >= 0; t--) {

                if (ReadFileInDirectory(relativeUri, fileDirectories_[t], out dir, out fileName, out fileExtension, out payload)) {
                    return true;
                }
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

            // Checking if path is already watched.
            if (!watchedPaths_.TryGetValue(fileSpecifier, out fsw)) {

                // Checking if watched directory exists.
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

                watchedPaths_[fileSpecifier] = fsw;
            }
        }

        /// <summary>
        /// Removes all event listeners and disposes the watcher.
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

            if (watchedPaths_ != null) {
                foreach (var watcher in watchedPaths_.Values) {
                    ClearWatchedChange(watcher);
                }
            }

            watchedPaths_ = new Dictionary<string, FileSystemWatcher>();
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

            // Locking because execution is done in separate thread.
            lock (fileDirectories_) {

                Response cachedResponse;

                if (cacheOnFilePath_.TryGetValue(fileSignature, out cachedResponse)) {

                    if (cachedResponse.Uris != null) {

                        Response resp;
                        foreach (var uri in cachedResponse.Uris) {
                            Debug("(decache uri) " + uri);
                            cacheOnUri_.TryRemove(uri, out resp);
                        }

                        Debug("(decache file) " + fileSignature);
                        cacheOnFilePath_.TryRemove(fileSignature, out resp);
                    }
                }
            }
        }

        /// <summary>
        /// Tries to open the file with the specified name in the specified directory.
        /// If not successful and the fileExtension parameter is not specified a default 
        /// extension is used.
        /// </summary>
        /// <param name="dir">The directory to look for the file</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="fileExtension">The file extension.</param>
        /// <returns></returns>
        private FileStream FileOpenAlternative(string dir, string fileName, ref string fileExtension) {

            bool hasExtension = !string.IsNullOrEmpty(fileExtension);
            string filePathWOExt = Path.Combine(dir, fileName);
            string filePath;

            filePath = filePathWOExt;
            if (hasExtension)
                filePath += fileExtension; 

            if (File.Exists(filePath))
                return File.OpenRead(filePath);
          
            if (!hasExtension) {
                fileExtension = ".html";
                filePath = filePathWOExt + fileExtension;
                if (File.Exists(filePath))
                    return File.OpenRead(filePath);
            }

            return null;
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
                Debug(String.Format("Illegal URI for static resource: {0}", relativeUri));
                directory = null;
                fileExtension = null;
                fileName = null;
                return; // Only local URIs are supported
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

        [Conditional("DEBUG")]
        private void Debug(string message) {
            Console.WriteLine(message);
        }
    }
}

