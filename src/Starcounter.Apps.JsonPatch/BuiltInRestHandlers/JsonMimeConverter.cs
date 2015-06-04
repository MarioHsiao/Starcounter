using System;
using System.Text;
using Starcounter.Advanced;
using Starcounter.XSON;

namespace Starcounter.Internal {
    /// <summary>
    /// 
    /// </summary>
    public class JsonMimeConverter : IResponseConverter {
        private JsonPatch jsonPatch = new JsonPatch();
 
        /// <summary>
        /// 
        /// </summary>
        /// <param name="before"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public byte[] Convert(Request request, object before, MimeType mimeType, out MimeType resultingMimeType) {
            Session s;
            byte[] ret = null;

            Profiler.Current.Start(ProfilerNames.JsonMimeConverter);

            switch (mimeType) {
                case MimeType.Application_Json:
                    resultingMimeType = MimeType.Application_Json;

                    Json r = (Json)before;
                    while (r.Parent != null)
                        r = r.Parent;
                    var root = (Json)r;
                    ret = root.ToJsonUtf8();

                    if (root.ChangeLog != null) {
                        // Make sure that we regard all changes as being sent to the client.
                        // Calculate patches from here on.
                        root.ChangeLog.Checkpoint();
                    }
                    break;
                case MimeType.Application_JsonPatch__Json:
                    resultingMimeType = MimeType.Application_JsonPatch__Json;

                    s = Session.Current;
                    if (s == null || s.PublicViewModel == null) {
                        throw new UnsupportedMimeTypeException(
                            String.Format("Cannot supply mime-type {0} for the JSON resource. There is no session, so no JSON-Patch message can be generated.", mimeType.ToString()));
                    }

                    int size = jsonPatch.CreateJsonPatchBytes(s.PublicViewModel, true, s.CheckOption(SessionOptions.IncludeNamespaces), out ret);
                    if (ret.Length != size) {
                        byte[] tmp = new byte[size];
                        Buffer.BlockCopy(ret, 0, tmp, 0, size);
                        ret = tmp;
                    }
                    break;
                default:
                    resultingMimeType = MimeType.Unspecified;
                    if (before is Json) {
                        Json obj = (Json)before;
                        var str = obj.AsMimeType(mimeType);
                        if (str == null && request != null) {
                            str = obj.AsMimeType(request["Accept"]);
                        }
                        if (str != null) {
                            resultingMimeType = mimeType;
                            ret = Encoding.UTF8.GetBytes(str);
                        } else {
                            switch (mimeType) {
                                case MimeType.Text_Plain:
                                case MimeType.Unspecified:
                                case MimeType.Text_Html:
                                    ret = this.Convert(null, before, MimeType.Application_Json, out resultingMimeType);
                                    break;
                            }
                        }
                    }

                    if (ret == null) {
                        throw new UnsupportedMimeTypeException(
                            String.Format("Unsupported mime type {0}.", mimeType.ToString()));
                    }
                    break;
            }

            Profiler.Current.Stop(ProfilerNames.JsonMimeConverter);

            return ret;
        }
    }
}
