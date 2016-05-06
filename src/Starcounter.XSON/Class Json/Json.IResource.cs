using XSONInjection = Starcounter.Internal.XSON.Modules.Starcounter_XSON.Injections;

namespace Starcounter {
    public partial class Json : IResource {
        
        /// <summary>
        /// Override this method to provide a custom conversion when a request
        /// is made to some other mime type than "application/json".
        /// </summary>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        public virtual string AsMimeType(MimeType mimeType) {
            return this.ToJson();
        }

        /// <summary>
        /// The way to get a URL for HTML partial if any.
        /// </summary>
        /// <returns></returns>
        [System.Obsolete("This method will be removed in a later version.")]
        public virtual string GetHtmlPartialUrl() {
            return null;
        }

        /// <summary>
        /// Override this method to provide a custom conversion when a request
        /// is made to some other mime type than "application/json".
        /// </summary>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        public virtual string AsMimeType(string mimeType) {
            return AsMimeType(MimeTypeHelper.StringToMimeType(mimeType));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mimeType"></param>
        /// <param name="resultingMimeType"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public virtual byte[] AsMimeType(MimeType mimeType, out MimeType resultingMimeType, Request request = null ) {
            return XSONInjection.JsonMimeConverter.Convert(request, this, mimeType, out resultingMimeType);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="mimeType"></param>
        /// <param name="resultingMimeType"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public virtual byte[] AsMimeType(string mimeType, out MimeType resultingMimeType, Request request = null ) {
            return XSONInjection.JsonMimeConverter.Convert(request, this, MimeTypeHelper.StringToMimeType(mimeType), out resultingMimeType);
        }

        public static implicit operator Response(Json x) {
            var response = new Response() {
                Resource = x
            };
            return response;
        }

        public static implicit operator Json(Response r) {  
            if (r != null)
                return r.Resource as Json;
            return null;
        }
    }
}
