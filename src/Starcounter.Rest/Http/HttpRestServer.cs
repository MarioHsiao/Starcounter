// ***********************************************************************
// <copyright file="HttpRestServer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
using HttpStructs;
using Starcounter.Internal.Web;
using Starcounter.Advanced;
namespace Starcounter.Internal.REST {
    /// <summary>
    /// Class HttpRestServer
    /// </summary>
   public abstract class HttpRestServer : IHttpRestServer {

       /// <summary>
       /// As an example, GetResource("images/hello.jpg") should return a byte array containing a jpeg image.
       /// </summary>
       /// <param name="request">The request.</param>
       /// <returns>The bytes containg the resource.</returns>
       /// <exception cref="System.NotImplementedException"></exception>
      public virtual Response HandleRequest(Request request) {
         throw new NotImplementedException();
      }

      /// <summary>
      /// The starcounter .EXE modules will provide a path where static file resources such as .html files or images
      /// are kept. This allows the web server to serve conent from all modules without having to copy or deploy files to
      /// a single location.
      /// </summary>
      /// <param name="path">The path to add to the list of paths used by the web server to find content.</param>
      public virtual void UserAddedLocalFileDirectoryWithStaticContent(string path) {
      }

      /// <summary>
      /// Housekeeps this instance.
      /// </summary>
      /// <returns>System.Int32.</returns>
      public virtual int Housekeep() {
         return -1;
      }
   }
}
