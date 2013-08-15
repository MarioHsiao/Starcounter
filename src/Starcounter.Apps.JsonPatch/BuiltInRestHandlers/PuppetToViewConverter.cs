

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
        public byte[] Convert(object before, MimeType mimeType, out MimeType resultingMimeType ) {

            switch (mimeType) {

                case MimeType.Application_Json:
                case MimeType.Unspecified: {

                        Container r = (Container)before;
                        while (r.Parent != null)
                            r = r.Parent;
                        Json root = (Json)r;
                        resultingMimeType = MimeType.Application_Json;
                        return root.ToJsonUtf8();
                    }

                case MimeType.Text_Html: {
                    var obj = (Json)before;
                    string s = obj.Html; ;
                    if (s[0] != '/') // TODO! Needs optimization
                        s = "/" + obj.Html;
                    resultingMimeType = mimeType;
                    return X.GET(s).BodyBytes;
                }

                case MimeType.Application_JsonPatch__Json: {
                        resultingMimeType = mimeType;
                        return HttpPatchBuilder.CreateHttpPatchResponse(ChangeLog.CurrentOnThread);
                }
            }
           
            throw new ArgumentException(
                String.Format("Unsupported mime type {0}.",mimeType.ToString()));
        }
    }
}
