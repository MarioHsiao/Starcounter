

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
                        resultingMimeType = MimeType.Application_Json;

                        Container r = (Container)before;
                        while (r.Parent != null)
                            r = r.Parent;
                        Json root = (Json)r;
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
                    resultingMimeType = mimeType;
                    if (obj.HtmlContent != null) {
                        return Encoding.UTF8.GetBytes(obj.HtmlContent);
                    }
                    string s = obj.Html;
                    if (s == null) {
                        MimeType discard;
                        return this.Convert(before, MimeType.Application_Json, out discard);
                    }
                    if (s[0] != '/') // TODO! Needs optimization
                        s = "/" + obj.Html;
                    return Encoding.UTF8.GetBytes(X.GET(s));
                }

                case MimeType.Application_JsonPatch__Json: {
                        Session s = Session.Current;
                        if (s == null) {
                            throw new UnsupportedMimeTypeException(
                                String.Format("Cannot supply mime-type {0} for the JSON resource. There is no session, so no JSON-Patch message can be generated.", mimeType.ToString()));
                        }
                        resultingMimeType = mimeType;
                        //return HttpPatchBuilder.CreateHttpPatchResponse(ChangeLog.CurrentOnThread);
                        var ret = s.CreateJsonPatchBytes(true);
                        return ret;
                }
            }
           
            throw new UnsupportedMimeTypeException(
                String.Format("Unsupported mime type {0}.",mimeType.ToString()));
        }
    }
}
