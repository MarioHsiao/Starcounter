// ***********************************************************************
// <copyright file="StaticWebServer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Advanced;
using Starcounter.Internal.REST;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Starcounter.Internal.Web {

    /// <summary>
    /// Manages the loading and caching of web resources such as .html files and images. Also keeps track of non static web resources such as apps (havig dynamic
    /// content). Each resource is represented by a WebResource instance.
    /// <para><img src="http://www.rebelslounge.com/res/scweb/WebResource.png" /></para>
    /// </summary>
   public partial class StaticWebServer : HttpRestServer {


       /// <summary>
       /// The web server can accept multiple root catalogues/directories to resolve static
       /// file resources. If the same file can be found in the same relative path in multiple
       /// root directories, the first match is used. For this reason, directories should
       /// be added in priority order with the most prioritised path first.
       /// </summary>
       /// <param name="path">The file path for the directory to add</param>
       public override void UserAddedLocalFileDirectoryWithStaticContent(UInt16 port, String path)
       {
            Console.WriteLine("Adding path to static web server \"" + path + "\"");

            // Always clearing cache when adding new directory on this port.
            ClearCache();

            // Adding only if does not contain this path already.
            if (!WorkingDirectories.Contains(path))
                WorkingDirectories.Add(path);
       }

      /// <summary>
      /// Http response cache keyed on URI
      /// </summary>
      /// <remarks>
      /// Http responses are cached in memory. The cache can store both compressed and
      /// uncompressed versions. Compressed and uncompressed items are cached
      /// the first time they are retrieved.
      /// </remarks>
      private Dictionary<string, Response> CacheOnUri;

      /// <summary>
      /// Http response cache keyed on file path on disk
      /// </summary>
      /// <remarks>
      /// Http responses are cached in memory. The cache can store both compressed and
      /// uncompressed versions. Compressed and uncompressed items are cached
      /// the first time they are retrieved.
      /// </remarks>
      private Dictionary<string, Response> CacheOnFilePath;

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
         CacheOnUri = new Dictionary<string, Response>();
         CacheOnFilePath = new Dictionary<string, Response>();
         ClearWatchedParts();
      }

/*      public byte[] GET(string relativeUri) {
         relativeUri = "/" + relativeUri;
         var request = "GET " + relativeUri + " HTTP/1.1\r\nHost: localhost\r\n\r\n";
         var b = Encoding.UTF8.GetBytes(request);
         var req = RequestParser.Parse(b, b.Length);
         req.WantsCompressed = false; // TODO! REMOVE!
         var response = GET(relativeUri, req);
         var x = CacheOnUri[relativeUri.ToLower()];
         var uncompressed = x.Uncompressed;
         var ret = new byte[x.ContentLength];
         System.Buffer.BlockCopy(uncompressed, x.HeaderLength, ret, 0, x.ContentLength);
         return ret;
      }
 */

      /// <summary>
      /// As a HttpRestServer, the static file server needs to implement the
      /// Handle method to provide a response to an http request.
      /// </summary>
      /// <param name="request">The http request</param>
      /// <returns>The http response</returns>
      public override Response HandleRequest(Request request) {
         return GetStatic( request.Uri, request );
      }

      /// <summary>
      /// Handling the http GET method (verb).
      /// </summary>
      /// <param name="relativeUri">The URI of the resource</param>
      /// <param name="request">The http request as defined by Starcounter</param>
      /// <returns>The UTF8 encoded response</returns>
      public Response GetStatic(string relativeUri, Request request ) {

         //            if (relativeUri.Equals("/")) {
         //                relativeUri = "/index.html";
         //            }
         //            else {
          Response resource;
         relativeUri = relativeUri.ToLower();
         //            }

         // Read one byte at offset index and return it.
         if (CacheOnUri.TryGetValue(relativeUri, out resource)) {
/*            if (request.WantsCompressed) {
               if (resource.Compressed != null) {
                  if (resource.WorthWhileCompressing)
                     request.Debug(" (cached compressed response)");
                  else
                     request.Debug(" (cached not-worth-compressing response)");
                  return resource;
               }
            }
            else {
               if (resource.Uncompressed != null) {
                  request.Debug(" (cached uncompressed response)");
                  return resource.Uncompressed;
               }
            }
 */
             return resource;
         }

         resource = GetFileResource(resource, relativeUri, request);

         //            Cache[relativeUri] = body;
//         if (request.WantsCompressed) {
//            return resource.Compressed;
//         }
//         return resource.Uncompressed;
//         // TODO. Only return compressed when allowed
         return resource;
      }

      /// <summary>
      /// Converts an uncompressed resource to a compressed resource.
      /// </summary>
      /// <param name="input">The uncompressed resource</param>
      /// <returns>The compressed resource</returns>
      public static byte[] Compress(byte[] input) {
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
      /// Housekeeps this instance.
      /// </summary>
      /// <returns>System.Int32.</returns>
      public override int Housekeep() {
         //ClearCache(); // TODO! Only invalidate individual items
         var invalidated = new List<Response>(CacheOnFilePath.Count);
         foreach (var cached in this.CacheOnFilePath) {
            var path = cached.Value.FilePath;
            bool was = cached.Value.FileExists;
            bool @is = File.Exists(path);
            if (was != @is || @is && File.GetLastWriteTime(path) != cached.Value.FileModified ) {
               invalidated.Add(cached.Value);
            }
         }
         foreach (var cached in invalidated) {
            var path = cached.FilePath;
            FileSystemEventArgs e = new FileSystemEventArgs(WatcherChangeTypes.Changed, cached.FileDirectory, cached.FileName);
            FileHasChanged(null, e);
         }
         return 1000;
      }
   }
}
