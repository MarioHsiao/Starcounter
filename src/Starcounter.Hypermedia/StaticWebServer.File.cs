// ***********************************************************************
// <copyright file="StaticWebServer.File.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BizArk.Core.Util;
using HttpStructs;
using Starcounter.Internal.REST;
using Starcounter.Advanced;

namespace Starcounter.Internal.Web {

    /// <summary>
    /// Class StaticWebServer
    /// </summary>
    public partial class StaticWebServer {

        /// <summary>
        /// The watched paths
        /// </summary>
        public Dictionary<string, FileSystemWatcher> WatchedPaths;

        /// <summary>
        /// Contains the directories that may contain web resources such as .html files and other assets. The built in web server
        /// </summary>
        public List<string> WorkingDirectories = new List<string>();


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
        public HttpResponse GetFileResource(HttpResponse cached, string relativeUri, HttpRequest req) {


            req.Debug(" (FILE ACCESS)");

            bool shouldBeCached = true;
            bool shouldCompress = req.IsGzipAccepted;
            HttpResponse fres = cached;
            if (fres == null)
                fres = new HttpResponse();

            string dir = null;
            string fileName = null;
            string fileExtension = null;
            int len = 0;
            string code = "200 OK";
            bool isText = true;
            bool is404 = true;
            string mimeType;
            byte[] payload = null;

            if (WorkingDirectories.Count == 0) {
                mimeType = "plain/text";
                is404 = true;
                payload = Encoding.UTF8.GetBytes(
                    "Uri could not be resolved. No directories added for serving static files.");
            }
            else {
                for (int t = 0; is404 && t < WorkingDirectories.Count; t++) {
                    ParseFileSpecifier(this.WorkingDirectories[t], relativeUri, out dir, out fileName, out fileExtension); // TODO! Try all working directories

                    try {
                        FileStream f = FileOpenAlternative(ref dir, ref fileName, ref fileExtension);
                        len = (int)f.Length;

                        // Check for UTF-8 byte order mark (BOM) offset
                        if (len >= 3) {
                            int utf8Size = 3;                  // UTF 8 byte check
                            byte[] bom = new byte[utf8Size];   // allocate place for UTF-8 check
                            f.Read(bom, 0, utf8Size);
                            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) // UTF-8
                            {
                                len -= utf8Size;               // Adjust the payload size without the 'bom'
                            }
                            else {
                                f.Position -= utf8Size;        // Reset the filestream position
                            }
                        }

                        payload = new byte[len];

                        f.Read(payload, 0, (int)len);

                        f.Close();
                        is404 = false;
                        break;
                    }
                    catch (FileNotFoundException) {
                        is404 = true;
                    }
                    catch (DirectoryNotFoundException) {
                        is404 = true;
                    }
                    catch (DriveNotFoundException) {
                        is404 = true;
                    }
                }
                if (is404) {
                    payload = Encoding.UTF8.GetBytes(String.Format("Error 404: File {0} not found", relativeUri + "."));
                    mimeType = "plain/text";
                }
                else {
                    var ext = "." + fileExtension;
                    //var mimeType2 = HtmlAgilityPack.HtmlWeb.GetContentTypeForExtension( ext, "text/x-" + fileExtension);
                    mimeType = MimeMap.GetMimeType(ext);
                    //if (mimeType2 != null && mimeType2 != mimeType) {
                    //    throw new Exception(String.Format(
                    //        "Uncertain mime type for file {0}. Should file extention {1} be mime type {2} or mime type {3}?",
                    //        fileName, ext, mimeType, mimeType2));
                    //                                                        
                    //}
                }

            }
            if (is404) {
                mimeType = "plain/text";
                code = "404 NOT FOUND";
                Console.WriteLine("Could not find " + relativeUri);
            }

            if (payload == null)
                payload = new byte[0];

            len = payload.Length;

            string str = "HTTP/1.1 " + code + "\r\nServer:SC\r\nConnection:keep-alive\r\n";
            
            // string[] parts = fileName.Split('.');
            // string ext = parts[parts.Length-1];
            //            Console.WriteLine("Type for " + fileName + "." + fileExtension + " is " + contentType);

            //               string etag = relativeUri;
            //                            "Etag: \"" + etag + "\"\r\n" +
            isText = mimeType.StartsWith("text/");
            if (isText)
                mimeType += ";charset=utf-8";

            byte[] compressed = null;
            bool didCompress = false;


            if (req.NeedsScriptInjection && mimeType.StartsWith("text/html")) {
                req.Debug(" (analysing html)");
                fres.ScriptInjectionPoint = ScriptInjector.FindScriptInjectionPoint(payload, 0);
                shouldCompress = false;
            }

            if (shouldCompress) {
                compressed = Compress(payload);
                didCompress = compressed.Length + 100 < payload.Length; // Don't use compress version if the difference is too small
                //didCompress = false;
                //                Console.WriteLine(String.Format("Compressed({0})+100 < Uncompressed({1})", compressed.Length, payload.Length));
                if (didCompress) {
                    req.Debug(" (compressing)"); // String.Format("Compressed({0})+100 < Uncompressed({1})", compressed.Length, payload.Length));
                    len = compressed.Length;
                }
                else {
                    req.Debug(" (not-worth-compressing)"); // String.Format("Compressed({0})+100 < Uncompressed({1})", compressed.Length, payload.Length));
                    fres.WorthWhileCompressing = false;
                }
            }


            str += "Content-Type:" + mimeType + "\r\n";


            //            if (!bigCookie)
            //                str += "Set-Cookie:sid=" + req.SessionID.ToString() + "\r\n";

            if (didCompress)
                str += "Content-Encoding:gzip\r\n";

            if (req.IsAppView)
                str += "Cache-control:private,max-age=0\r\n"; // Dont cache
            else
                str += "Cache-control:public,max-age=31536000\r\n"; // 1 year cache

            // Cache-Control:public,max-age=31536000
            // Age:0

            string lenStr = len.ToString();
            str += "Content-Length:" + lenStr + "\r\n\r\n";
            fres.ContentLength = len;
            fres.ContentLengthLength = lenStr.Length; // TODO! Should really measure bytes. In UTF-8 bytes and characters for numeric characters is one and the same.

            byte[] header = Encoding.UTF8.GetBytes(str);

            fres.ContentLengthInjectionPoint = header.Length - 4 - lenStr.Length;  // TODO! Should really measure bytes. In UTF-8 bytes and characters for numeric characters is one and the same.
            fres.ScriptInjectionPoint += header.Length;
            fres.HeaderInjectionPoint = 9 + code.Length + 2; // "HTTP/1.1 " + code + "/r/n"

            byte[] response;

            if (didCompress) {
                response = new byte[header.Length + compressed.Length];
                compressed.CopyTo(response, header.Length);
                fres.Compressed = response;
                fres.CompressedContentOffset_ = header.Length;
                fres.CompressedContentLength_ = compressed.Length;
            }
            else {
                response = new byte[header.Length + len];
                payload.CopyTo(response, header.Length);
                fres.Uncompressed = response;
                fres.UncompressedContentOffset_ = header.Length;
               // fres.
            }
            fres.HeadersLength = header.Length;
            header.CopyTo(response, 0);

            if (shouldBeCached && cached == null) {
                CacheOnUri[relativeUri] = fres;
                fres.Uris.Add(relativeUri);
                string path = (dir + "\\" + fileName + "." + fileExtension);
                string fileSignature = path.ToUpper();
                fres.FilePath = fileSignature;
                fres.FileDirectory = dir;
                fres.FileName = fileName + "." + fileExtension;
                fres.FileExists = !is404;
                if (!is404) {
                    fres.FileModified = File.GetLastWriteTime(path);
                }
                CacheOnFilePath[fileSignature] = fres;
                WatchChange(dir, fileName + "." + fileExtension); //.DirectoryName,fi.Name);
            }

            //  if (found && isText)
            //  {
            //     Console.WriteLine(Encoding.UTF8.GetString(response).Substring(0,1000));
            //  }
            return fres;
        }

        /// <summary>
        /// Called to monitor live changes to static resources. If a file is changed,
        /// the cache is invalidated. This allows the web server to always server fresh
        /// versions of any resource.
        /// </summary>
        /// <param name="dir">The dir.</param>
        /// <param name="fileName">Name of the file.</param>
        private void WatchChange(string dir, string fileName) {
            FileSystemWatcher fsw;
            string fileSpecifier = dir + "\\" + fileName;
            if (!WatchedPaths.TryGetValue(fileSpecifier, out fsw)) {
                if (Directory.Exists(dir)) {
                    fsw = new FileSystemWatcher(dir);
                    fsw.Filter = fileName;
                    fsw.IncludeSubdirectories = false;
                    fsw.Changed += new FileSystemEventHandler(FileHasChanged);
                    fsw.EnableRaisingEvents = true;
                }
                else {
                    fsw = null;
                }
                WatchedPaths[fileSpecifier] = fsw;
            }
        }


        /// <summary>
        /// Files the has changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="FileSystemEventArgs" /> instance containing the event data.</param>
        internal void FileHasChanged(object sender, FileSystemEventArgs e) {
            string fileSignature = e.FullPath.ToUpper();
            HttpResponse cached;
            if (CacheOnFilePath.TryGetValue(fileSignature, out cached)) {

                foreach (var uri in cached.Uris) {
                    Console.WriteLine("(decache) " + uri);
                    CacheOnUri.Remove(uri);
                }
                CacheOnFilePath.Remove(fileSignature);
            }
        }


        /// <summary>
        /// Files the open alternative.
        /// </summary>
        /// <param name="dir">The dir.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="fileExtension">The file extension.</param>
        /// <returns>FileStream.</returns>
        public FileStream FileOpenAlternative(ref string dir, ref string fileName, ref string fileExtension) {
            try {
                return File.OpenRead(dir + "/" + fileName + "." + fileExtension);
            }
            catch (Exception) {
                if (fileExtension == "") {
                    fileExtension = "html";
                    return File.OpenRead(dir + "/" + fileName + "." + fileExtension);
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
        public void ParseFileSpecifier(string serverPath, string relativeUri, out string directory, out string fileName, out string fileExtension) {
            if (!relativeUri.StartsWith("/")) {
                Console.WriteLine(String.Format("Illegal URI for static resoruce {0}", relativeUri));
                directory = null;
                fileExtension = null;
                fileName = null;
                return; // Only local uris are supported
            }

            string[] segments = relativeUri.Split('?');

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
                fileExtension = components[components.Length - 1];
                fileName = components[0];
                for (int i = 1; i < components.Length - 1; i++) {
                    fileName += "." + components[i];
                }
            }
            else {
                fileName = fileNameWithExtension;
                fileExtension = "";
            }
            //    fileSpecifier = directory + @"\" + fileNameWithExtension;
        }

        /// <summary>
        /// Clears the watched parts.
        /// </summary>
        public void ClearWatchedParts() {
            WatchedPaths = new Dictionary<string, FileSystemWatcher>();
        }

        /// <summary>
        /// Encodes to base64.
        /// </summary>
        /// <param name="toEncode">To encode.</param>
        /// <returns>System.String.</returns>
        static public string EncodeToBase64(string toEncode) {
            byte[] toEncodeAsBytes
                  = System.Text.ASCIIEncoding.ASCII.GetBytes(toEncode);
            string returnValue
                  = System.Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }

    }

}

