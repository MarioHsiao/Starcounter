using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HttpStructs;
using Starcounter.Internal.Web;

namespace Starcounter.Internal.REST {
   /// <summary>
   /// To serve Web pages and App state, you can implement a REST handler. A REST handler can process a Method on a 
   /// resource located using a URI.
   /// </summary>
   /// <remarks>
   /// The most commonly known use of Representational state transfer (REST) is the world wide web (WWW)
   /// that uses the HTTP protocol (the most commonly used REST protocol). The most commonly used REST Methods
   /// are GET and POST, often used by web browser to get REST resources in the HTML format.
   ///  The term REST was introduced by Roy Fielding, one of the authors of the original HTTP protocol.
   /// </remarks>
   public interface IHttpRestServer {

      /// <summary>
      /// As an example, GetResource("images/hello.jpg") should return a byte array containing a jpeg image.
      /// </summary>
      /// <param name="relativeUri">The relative URI (i.e. the right part of a URL) to the web resource to materialize.</param>
      /// <returns>The bytes containg the resource.</returns>
      HttpResponse Handle( HttpRequest request );

      /// <summary>
      /// The starcounter .EXE modules will provide a path where static file resources such as .html files or images
      /// are kept. This allows the web server to serve conent from all modules without having to copy or deploy files to
      /// a single location.
      /// </summary>
      /// <param name="path">The path to add to the list of paths used by the web server to find content.</param>
      void UserAddedLocalFileDirectoryWithStaticContent(string path);

      int Housekeep();
   }
}
