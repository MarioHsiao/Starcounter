// ***********************************************************************
// <copyright file="HttpResponse.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using HttpStructs;
namespace Starcounter {

    /// <summary>
    /// The Starcounter Web Server caches resources as complete http responses.
    /// As the exact same response can often not be used, the cashed response also
    /// include useful offsets and injection points to facilitate fast transitions
    /// to individual http responses. The cached response is also used to cache resources
    /// (compressed or uncompressed content) even if the consumer wants to embedd the content
    /// in a new http response.
    /// </summary>
   public class HttpResponse {

       /// <summary>
       /// The _ uncompressed
       /// </summary>
      private byte[] _Uncompressed = null;
      /// <summary>
      /// The _ compressed
      /// </summary>
      private byte[] _Compressed = null;

      /// <summary>
      /// The uris
      /// </summary>
      public List<string> Uris = new List<string>();

      /// <summary>
      /// The file path
      /// </summary>
      public string FilePath;
      /// <summary>
      /// The file directory
      /// </summary>
      public string FileDirectory;
      /// <summary>
      /// The file name
      /// </summary>
      public string FileName;
      /// <summary>
      /// The file exists
      /// </summary>
      public bool FileExists;
      /// <summary>
      /// The file modified
      /// </summary>
      public DateTime FileModified;

      /// <summary>
      /// Initializes a new instance of the <see cref="HttpResponse" /> class.
      /// </summary>
      /// <param name="content">The content.</param>
      /// <exception cref="System.Exception"></exception>
      public HttpResponse(string content) {
          throw new Exception();
      }
      /// <summary>
      /// Initializes a new instance of the <see cref="HttpResponse" /> class.
      /// </summary>
      public HttpResponse() {
          HeaderInjectionPoint = -1;
      }

      /// <summary>
      /// As the session id is a fixed size field, the session id of a cached
      /// response can easily be replaced with a current session id.
      /// </summary>
      /// <value>The session id offset.</value>
      /// <remarks>The offset is only valid in the uncompressed response.</remarks>
      public int SessionIdOffset { get; set; }

      #region ContentInjection
      /// <summary>
      /// Used for content injection.
      /// Where to insert the View Model assignment into the html document.
      /// </summary>
      /// <remarks>
      /// The injection offset (injection point) is only valid in the uncompressed
      /// response.
      /// 
      /// Insertion is made at one of these points (in order of priority).
      /// ======================================
      /// 1. The point after the &lt;head&gt; tag.
      /// 2. The point after the &lt;!doctype&gt; tag.
      /// 3. The beginning of the html document.
      /// </remarks>
      /// <value>The script injection point.</value>
      public int ScriptInjectionPoint { get; set; }

      /// <summary>
      /// Used for content injection.
      /// When injecting content into the response, the content length header
      /// needs to be altered. Used together with the ContentLengthLength property.
      /// </summary>
      /// <value>The content length injection point.</value>
      public int ContentLengthInjectionPoint { get; set; } // Used for injection

      /// <summary>
      /// Used for content injection.
      /// When injecting content into the response, the content length header
      /// needs to be altered. The existing previous number of bytes used for the text
      /// integer length value starting at ContentLengthInjectionPoint is stored here.
      /// </summary>
      /// <value>The length of the content length.</value>
      public int ContentLengthLength { get; set; } // Used for injection

        /// <summary>
        /// Used for injecting headers. Specifies where to insert additional
        /// headers that might be needed.
        /// </summary>
        public int HeaderInjectionPoint { get; set; }

      #endregion

      /// <summary>
      /// The number of bytes containing the http header in the uncompressed response. This is also
      /// the offset of the first byte of the content.
      /// </summary>
      /// <value>The length of the header.</value>
      public int HeaderLength { get; set; }

      /// <summary>
      /// The number of bytes of the content (i.e. the resource) of the uncompressed http response.
      /// </summary>
      /// <value>The length of the content.</value>
      public int ContentLength { get; set; }

      /// <summary>
      /// The uncompressed cached response
      /// </summary>
      /// <value>The uncompressed.</value>
      public byte[] Uncompressed {
         get {
            return _Uncompressed;
         }
         set {
            _Uncompressed = value;
         }
      }

      /// <summary>
      /// Gets the bytes.
      /// </summary>
      /// <param name="request">The request.</param>
      /// <returns>System.Byte[][].</returns>
      public byte[] GetBytes(HttpRequest request) {
          if (request.IsGzipAccepted && Compressed != null)
              return Compressed;
          return Uncompressed;
      }

      /// <summary>
      /// The compressed (gzip) cached resource
      /// </summary>
      /// <value>The compressed.</value>
      public byte[] Compressed {
         get {
            if (!WorthWhileCompressing)
               return _Uncompressed;
            else
               return _Compressed;
         }
         set {
            _Compressed = value;
         }
      }

      /// <summary>
      /// The _ worth while compressing
      /// </summary>
      private bool _WorthWhileCompressing = true;

      /// <summary>
      /// If false, it was found that the compressed version of the response was
      /// insignificantly smaller, equally large or even larger than the original version.
      /// </summary>
      /// <value><c>true</c> if [worth while compressing]; otherwise, <c>false</c>.</value>
      public bool WorthWhileCompressing {
         get {
            return _WorthWhileCompressing;
         }
         set {
            _WorthWhileCompressing = value;
         }
      }
   }
}
