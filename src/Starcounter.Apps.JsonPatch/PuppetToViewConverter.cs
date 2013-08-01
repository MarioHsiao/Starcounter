

namespace Starcounter.Internal {

    /// <summary>
    /// 
    /// </summary>
    public class PuppetToViewConverter : IResponseConverter {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="before"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public byte[] Convert(object before, MimeType type) {
            var obj = (Json)before;
            string s = obj.View;
            if (s[0] != '/') // TODO! Needs optimization
                s = "/" + obj.View;
            return NodeX.GET(s).BodyBytes;
        }
    }
}
