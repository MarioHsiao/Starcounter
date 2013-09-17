

using Starcounter.Advanced;
using Starcounter.Internal.JsonPatch;
using System;
using System.Diagnostics;
using System.Text;
namespace Starcounter.Internal {

    /// <summary>
    /// 
    /// </summary>
    public class JsonMimeConverter : IResponseConverter {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="before"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public byte[] Convert(Request request, object before, MimeType mimeType, out MimeType resultingMimeType ) {

//            Debugger.Launch();
            switch (mimeType) {

                case MimeType.Application_Json:
/*                case MimeType.Text_Plain:
                case MimeType.Unspecified:*/ {
                        resultingMimeType = MimeType.Application_Json;

                        Json r = (Json)before;
                        while (r.Parent != null)
                            r = r.Parent;
                        var root = (Json)r;
                        var ret = root.ToJsonUtf8();
                        var s = root.Session;
                        if (s != null) {
                            // Make sure that we regard all changes as being sent to the client.
                            // Calculate patches from here on.
                            s.CheckpointChangeLog();
                        }
                        return ret;
                    }

//                case MimeType.Text_Html: {
//                    var obj = (Json)before;
//                    resultingMimeType = mimeType;
//                    if (obj._htmlContent != null) {
//                        return obj._htmlContent;
//                    }
//                    string s = obj.OldHtml;
//                    if (s == null) {
//                        MimeType discard;
//                        return this.Convert(before, MimeType.Application_Json, out discard);
//                    }
//                    if (s[0] != '/') // TODO! Needs optimization
//                        s = "/" + obj.OldHtml;
//                    return Encoding.UTF8.GetBytes(X.GET(s));
//                }

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

            if (before is Json) {
                Json obj = (Json)before;
                var str = obj.AsMimeType(mimeType);
                if (str == null && request != null) {
                    str = obj.AsMimeType( request["Accept"] );
                }
                if (str != null) {
                    resultingMimeType = mimeType;
                    return Encoding.UTF8.GetBytes(str);
                }
                switch (mimeType) {
                    case MimeType.Text_Plain:
                    case MimeType.Unspecified:
                    case MimeType.Text_Html: {
                            return this.Convert(null,before, MimeType.Application_Json, out resultingMimeType);
                    }
                }
            }
           
            throw new UnsupportedMimeTypeException(
                String.Format("Unsupported mime type {0}.",mimeType.ToString()));
        }
    }
}
