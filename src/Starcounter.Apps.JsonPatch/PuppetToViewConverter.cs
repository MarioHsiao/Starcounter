

using Starcounter.Internal.JsonPatch;
using System;
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

            switch (type) {

                case MimeType.Text_Html: {
                    var obj = (Json)before;
                    string s = obj.Html; ;
                    if (s[0] != '/') // TODO! Needs optimization
                        s = "/" + obj.Html;

                    return NodeX.GET(s).BodyBytes;
                }

                case MimeType.Application_JsonPatch__Json: {
                    return HttpPatchBuilder.CreateHttpPatchResponse(ChangeLog.CurrentOnThread);
                }
            }
           
            throw new ArgumentException("Unsupported mime type.");
        }
    }
}
