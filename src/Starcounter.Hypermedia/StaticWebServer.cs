﻿// ***********************************************************************
// <copyright file="StaticWebServer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Starcounter.Advanced;
using System.Collections.Concurrent;

namespace Starcounter.Internal.Web {

    /// <summary>
    /// Manages the loading and caching of web resources such as .html files and images. Also keeps track 
    /// of non static web resources such as apps (having dynamic content). Each resource is represented 
    /// by a WebResource instance.
    /// <para><img src="http://www.rebelslounge.com/res/scweb/WebResource.png" /></para>
    /// </summary>
    public partial class StaticWebServer : IRestServer {

        /// <summary>
        /// Http response cache keyed on URI.
        /// </summary>
        /// <remarks>
        /// Http responses are cached in memory. The cache can store both compressed and
        /// uncompressed versions. Compressed and uncompressed items are cached
        /// the first time they are retrieved.
        /// </remarks>
        private ConcurrentDictionary<string, Response> cacheOnUri_;

        /// <summary>
        /// Http response cache keyed on file path on disk.
        /// </summary>
        /// <remarks>
        /// Http responses are cached in memory. The cache can store both compressed and
        /// uncompressed versions. Compressed and uncompressed items are cached
        /// the first time they are retrieved.
        /// </remarks>
        private ConcurrentDictionary<string, Response> cacheOnFilePath_;

        /// <summary>
        /// The web server can accept multiple root catalogs/directories to resolve static
        /// file resources. If the same file can be found in the same relative path in multiple
        /// root directories, the first match is used. For this reason, directories should
        /// be added in priority order with the most prioritized path first.
        /// </summary>
        public void UserAddedLocalFileDirectoryWithStaticContent(String appName, UInt16 port, String path) {

            // Getting full path for the directory.
            path = Path.GetFullPath(path);

            // Making sure that we don't add an existing directory.
            path = path.ToLowerInvariant();

            // Checking if directory exists.
            if (!Directory.Exists(path)) {
                throw new Exception(
                    String.Format("The directory {0} given for file server directory does not exist.",
                    path));
            }

            // Always clearing cache when adding new directory on this port.
            ClearCache();

            // Adding only if does not contain this path already.
            if (!fileDirectories_.Contains(path)) {

                fileDirectories_.Add(path);
                appNames_.Add(appName);
            }
        }

        /// <summary>
        /// Get a list with all folders where static file resources such as .html files or images are kept.
        /// </summary>
        /// <param name="port"></param>
        /// <returns>List with folders</returns>
        public List<string> GetWorkingDirectories(UInt16 port) {
            return fileDirectories_;
        }

        /// <summary>
        /// Creates a new web server with an empty cache.
        /// </summary>
        public StaticWebServer() {
            ClearCache();
        }

        /// <summary>
        /// Empties the cache.
        /// </summary>
        public void ClearCache() {

            cacheOnUri_ = new ConcurrentDictionary<string, Response>();
            cacheOnFilePath_ = new ConcurrentDictionary<string, Response>();
            ClearWatchedParts();
        }

        /// <summary>
        /// Handling the static file response clone.
        /// </summary>
        /// <param name="relativeUri">The URI of the resource</param>
        /// <param name="request">The http request as defined by Starcounter</param>
        /// <returns>The UTF8 encoded response</returns>
        public Response GetStaticResponseClone(string relativeUri, Request request) {

            Response resourceResp;

            relativeUri = relativeUri.ToLower();

            // Trying to get the resource from the cache.
            cacheOnUri_.TryGetValue(relativeUri, out resourceResp);

            // Checking if response wasn't cached.
            if (resourceResp == null) {

                // We need to lock here because of possible multiple file accesses.
                lock (cacheOnUri_) {

                    // Re-trying once again because of the lock.
                    if (!cacheOnUri_.TryGetValue(relativeUri, out resourceResp)) {
                        resourceResp = GetFileResource(resourceResp, relativeUri, request);
                    }
                }
            }

            return resourceResp.CloneStaticResourceResponse();
        }

        /// <summary>
        /// Converts an uncompressed resource to a compressed resource.
        /// </summary>
        /// <param name="input">The uncompressed resource</param>
        /// <returns>The compressed resource</returns>
        private static byte[] Compress(byte[] input) {

            byte[] output;

            using (MemoryStream ms = new MemoryStream()) {

                using (GZipStream gs = new GZipStream(ms, CompressionMode.Compress)) {

                    gs.Write(input, 0, input.Length);
                    gs.Close();
                    output = ms.ToArray();
                }
                ms.Close();
            }
            return output;
        }

        /// <summary>
        /// House-keeps this instance.
        /// </summary>
        public void Housekeep() {

            List<Response> invalidatedCachedResponses = new List<Response>(cacheOnFilePath_.Count);

            foreach (KeyValuePair<String, Response> cached in cacheOnFilePath_) {

                var path = cached.Value.FilePath;
                bool was = cached.Value.FileExists;
                bool exists = File.Exists(path);

                if (was != exists || exists && File.GetLastWriteTime(path) != cached.Value.FileModified) {
                    invalidatedCachedResponses.Add(cached.Value);
                }
            }

            foreach (Response cachedResp in invalidatedCachedResponses) {

                var path = cachedResp.FilePath;
                FileSystemEventArgs e = new FileSystemEventArgs(WatcherChangeTypes.Changed, cachedResp.FileDirectory, cachedResp.FileName);
                FileHasChanged(null, e);
            }
        }
    }
}
