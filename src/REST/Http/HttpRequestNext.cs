/*

using Starcounter.Internal;
using System;
namespace Starcounter {

    /// <summary>
    /// References the memory containing the complete Http request sent from the
    /// Starcounter Gateway. 
    /// </summary>
    /// <remarks>
    /// This class does not require your code to have the unsafe flag as it only exposes the IntPtr type.
    /// </remarks>
    public class HttpRequestNext {

        /// <summary>
        /// Gets the byte* for the raw data of the http request
        /// </summary>
        /// <param name="ptr">The byte pointer to the first byte of the http verb/method</param>
        /// <param name="size">The total number of bytes of the http request</param>
        public void GetRawRequest(out IntPtr ptr, out int size) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the verb and uri of the http request.
        /// </summary>
        /// <param name="ptr">Usually the same value as in GetRawRequest</param>
        /// <param name="size"></param>
        public void GetRawVerbAndUri(out IntPtr ptr, out int size) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets an individual header
        /// </summary>
        /// <remarks>
        /// Avoid calling this method unless necessary as it adds extra overhead to the
        /// http request parsing
        /// </remarks>
        /// <param name="ptr"></param>
        /// <param name="size"></param>
        public void GetRawHeader(byte[] key, out IntPtr ptr, out int size) {
            throw new NotImplementedException();
        }

        public void GetRawContent(out IntPtr ptr, out int size);

        public long ContentLength {
            get {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// SessionID is a structure
        /// </summary>
        public SessionID SessionID {
            get {
                throw new NotImplementedException();
            }
            set {
                throw new NotImplementedException();
            }
        }

    }
}


*/