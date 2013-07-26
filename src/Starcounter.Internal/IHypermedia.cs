
namespace Starcounter {

    /// <summary>
    /// Implemented by resources to allow them to be converted into a
    /// binary representations. Used when responding to REST requests such as
    /// HTTP requests.
    /// </summary>
    /// <remarks>
    /// Text types are represented using an UTF-8 encoding.
    /// </remarks>
    public interface IHypermedia {

        /// <summary>
        /// Returns a binary representation of the resource using the specified MIME type.
        /// </summary>
        /// <remarks>
        /// Throws a System.NotSupportedException if the MIME type is not supported.
        /// </remarks>
        /// <param name="type">A common mime type. If the desired mime type is not available,
        ///  the method AsMimeType( string mimeType ) should be used.</param>
        /// <returns></returns>
        byte[] AsMimeType(MimeType mimeType);

        /// <summary>
        /// Returns a binary representation of the resource using the specified MIME type.
        /// </summary>
        /// <remarks>
        /// Throws a System.NotSupportedException if the MIME type is not supported.
        /// </remarks>
        /// <param name="type">The mime type. If the desired mime type is available in the
        /// MimeType enumeration, the method AsMimeType( MimeType mimeType ) should be used instead.</param>
        /// <returns></returns>
        byte[] AsMimeType(string mimeType);
    }

    /// <summary>
    /// Common mime types
    /// </summary>
    public enum MimeType {
        text_plain=1,
        text_html=2,
        application_json=3,
        application_jsonpatch_json=4
    }

    public class MimeTypeHelper {
        public static string MimeTypeAsString(MimeType mimeType) {
            return "application/json";
        }
    }
}
