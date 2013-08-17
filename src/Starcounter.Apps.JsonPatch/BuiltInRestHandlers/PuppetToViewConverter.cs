

using Starcounter.Advanced;
using Starcounter.Internal.JsonPatch;
using System;
using System.Text;
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
                        var ret = root.ToJsonUtf8();
                        var s = root.Session;
                        if (s != null) {
                            // Make sure that we regard all changes as being sent to the client.
                            // Calculate patches from here ons
                            s.CheckpointChangeLog();
                        }
                        return ret;
                    }

                case MimeType.Text_Html: {
                    var obj = (Json)before;
                    string s = obj.Html; ;
                    if (s[0] != '/') // TODO! Needs optimization
                        s = "/" + obj.Html;
                    resultingMimeType = mimeType;
                    return Encoding.UTF8.GetBytes(X.GET(s));
                }

                case MimeType.Application_JsonPatch__Json: {
                        resultingMimeType = mimeType;
                        //return HttpPatchBuilder.CreateHttpPatchResponse(ChangeLog.CurrentOnThread);
                        var ret = Session.Current.CreateJsonPatchBytes(true);
                        return ret;
                }
            }
           
            throw new UnsupportedMimeTypeException(
                String.Format("Unsupported mime type {0}.",mimeType.ToString()));
        }
    }
}
