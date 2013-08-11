
namespace Starcounter.Internal {

    /// <summary>
    /// 
    /// </summary>
    public interface IResponseConverter {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="before"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        byte[] Convert( object before, MimeType type, out MimeType resultingMimetype );
    }
}
