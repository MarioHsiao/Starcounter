
using Starcounter.Internal.Web;
namespace Starcounter.Advanced {

    public partial class Response {

        private static Response Fixed404 = null;
        private static Response Fixed406 = null;

        /// <summary>
        /// Returns a default 404 Not Found response.
        /// </summary>
        /// <returns>A cached 404 response (one size fits all)</returns>
        public static Response NotFound404 {
            get {
                if (Fixed404 == null) {
                    Fixed404 = Response.FromStatusCode(404);
                }
                return Fixed404;
            }
        }

        /// <summary>
        /// Returns a default 406 Not Acceptable response.
        /// </summary>
        /// <returns>A cached 406 response (one size fits all)</returns>
        public static Response NotAcceptable406 {
            get {
                if (Fixed406 == null) {
                    Fixed406 = Response.FromStatusCode(406);
                }
                return Fixed406;
            }
        }
    }
}
