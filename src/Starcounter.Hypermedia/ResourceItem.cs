
namespace Starcounter.Internal.Web {


    public class ResourceItem {

        private byte[] _UncompressedResponse = null;
        private byte[] _CompressedResponse = null;

//        private byte[] _UncompressedContent = null;
//        private byte[] _CompressedContent = null;

        /// <summary>
        /// As the session id is a fixed size field, the session id of a cached
        /// response can easily be replaced with a current session id.
        /// </summary>
        /// <remarks>
        /// The offset is only valid in the uncompressed response.
        /// </remarks>
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
        /// 1. The point after the <head> tag.
        /// 2. The point after the <!doctype> tag.
        /// 3. The beginning of the html document.
        /// </remarks>
        public int ScriptInjectionPoint { get; set; }

        /// <summary>
        /// Used for content injection.
        /// When injecting content into the response, the content length header
        /// needs to be altered. Used together with the ContentLengthLength property.
        /// </summary>
        public int ContentLengthInjectionPoint { get; set; } // Used for injection

        /// <summary>
        /// Used for content injection.
        /// When injecting content into the response, the content length header
        /// needs to be altered. The existing previous number of bytes used for the text
        /// integer length value starting at ContentLengthInjectionPoint is stored here.
        /// </summary>
        public int ContentLengthLength { get; set; } // Used for injection
        #endregion
        public int HeaderLength { get; set; }
        public int ContentLength { get; set; }

        public byte[] UncompressedResponse {
            get {
                return _UncompressedResponse;
            }
            set {
                _UncompressedResponse = value;
            }
        }

//        public byte[] UncompressedContent {
//            get {
//                return _UncompressedContent;
//            }
//            set {
//                _UncompressedContent = value;
//            }
//        }

        public byte[] CompressedResponse {
            get {
                if (!WorthWhileCompressing)
                    return _UncompressedResponse;
                else
                    return _CompressedResponse;
            }
            set {
                _CompressedResponse = value;
            }
        }

//        public byte[] CompressedContent {
//            get {
//                if (!WorthWhileCompressing)
//                    return _UncompressedContent;
//                else
//                    return _CompressedContent;
//            }
//            set {
//                _CompressedContent = value;
//            }
//        }

        private bool _WorthWhileCompressing = true;

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
