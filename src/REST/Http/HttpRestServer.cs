
using System;
using System.Text;
using HttpStructs;
using Starcounter.Internal.Web;
namespace Starcounter.Internal.REST {
   public abstract class HttpRestServer : IHttpRestServer {

      public virtual HttpResponse Handle( HttpRequest request ) {
         throw new NotImplementedException();
      }

      public virtual void UserAddedLocalFileDirectoryWithStaticContent(string path) {
      }

      public virtual int Housekeep() {
         return -1;
      }
   }
}
