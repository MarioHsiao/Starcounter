

using Starcounter.Advanced.XSON;
using Starcounter.Internal;
using System.Collections.Generic;

namespace Modules {

    /// <summary>
    /// Represents this module
    /// </summary>
    public static class Starcounter_XSON {

        /// <summary>
        /// Contains all dependency injections into this module
        /// </summary>
        public static class Injections {


            /// <summary>
            /// In Starcounter, the user (i.e. programmer) can respond with an Obj on an Accept: text/html request.
            /// In this case, the HTML pertaining to the view of the view model described by the Obj should
            /// be retrieved. This cannot be done by the Obj itself as it does not know about the static web server
            /// or how to call any user handlers.
            /// </summary>
            public static IResponseConverter _JsonMimeConverter = null;
        }
    }
}