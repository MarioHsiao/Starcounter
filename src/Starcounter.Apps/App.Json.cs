using System;
using System.Text;
using Newtonsoft.Json;
using Starcounter.Templates;

namespace Starcounter {

    public partial class App {
//        private char[] Session;
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Needs optimization. Should build JSON directly from TurboText or static UTF8 bytes
        /// to UTF8. This suboptimal version first builds Windows UTF16 strings that are ultimatelly
        /// not used.
        /// </remarks>
        /// <returns></returns>
        public byte[] ToJsonUtf8(bool includeView) {
            return Encoding.UTF8.GetBytes(ToJson(includeView));
        }

        public byte[] ToJsonUtf8(bool includeView, bool includeSessionId)
        {
            return Encoding.UTF8.GetBytes(ToJson(includeView, false, includeSessionId ));
        }

        public string ToJson(bool includeView = false, bool includeSchema = false, bool includeSessionId = false) { //, IncludeView includeViewContent = IncludeView.Default) {
#if QUICKTUPLE
            var sb = new StringBuilder();
            var templ = this.Template;
            int t = 0;
            if (includeSchema)
                sb.Append("{$$:{},");
            else
                sb.Append('{');

            bool needsComma = false;

//            sb.Append('$');
            foreach (object val in _Values) {
                Template prop = templ.Properties[t++];
                /*                    if (includeSchema) {
                                if (prop.IsVisibleOnClient) {
                                        if (includeSchema && (!prop.HasInstanceValueOnClient || !prop.HasDefaultPropertiesOnClient)) {
                                            if (needsComma) {
                                                sb.Append(',');
                                            }
                                            int tt = 0;
                                            sb.Append('"');
                                            sb.Append('$');
                                            sb.Append(prop.Name);
                                            sb.Append('"');
                                            sb.Append(':');
                                            sb.Append('{');
                                            if (!prop.HasInstanceValueOnClient) {
                                                if (tt++ > 0)
                                                    sb.Append(',');
                                                sb.Append("Type:\"");
                                                sb.Append(prop.JsonType);
                                                sb.Append('"');
                                            }
                                            if (!prop.Editable) {
                                                if (tt++ > 0)
                                                    sb.Append(',');
                                                sb.Append("Editable:false");
                                            }
                                            sb.Append('}');
                                            needsComma = true;
                                        }
                                    }
                 */
                if (prop.HasInstanceValueOnClient) {
                    if (needsComma) {
                        sb.Append(',');
                    }
                    sb.Append('"');
                    sb.Append(prop.Name);
                    sb.Append('"');
                    sb.Append(':');
                    if (prop is ListTemplate) {
                        sb.Append('[');
                        int i = 0;
                        foreach (var x in val as Listing) {
                            if (i++ > 0) {
                                sb.Append(',');
                            }
                            sb.Append(x.ToJson(false));
                        }
                        sb.Append(']');
                    }
                    else if (prop is AppTemplate) {
//                       var x = includeViewContent;
//                       if (x == IncludeView.Default)
//                          x = IncludeView.Always;
                       sb.Append(((App)val).ToJson(includeSchema));
                    }
                    else {
                       sb.Append(JsonConvert.SerializeObject(val));
                    }
                    needsComma = true;
                }
            }
//            var view = Media.FileName ?? templ.PropertyName;

            if (Media.Content != null) {
                if (t > 0)
                    sb.Append(',');
                //                if (includeViewContent == IncludeView.Always ) {
                sb.Append("__vc:");
                //return StaticFileServer.GET(relativeUri, request);
                sb.Append(JsonConvert.SerializeObject(Encoding.UTF8.GetString(Media.Content.Uncompressed)));
                //                }
                //                else {
                //                   sb.Append("__vf:");
                //                   sb.Append(JsonConvert.SerializeObject(Media.Content.FilePath.ToString()));
                //                }
            }
            else {
//                var view = View ?? templ.PropertyName;

                if (includeView && View != null) {
                    if (t > 0)
                        sb.Append(',');
                        if (true) { // includeViewContent == IncludeView.Always ) {
                            sb.Append("__vc:");
                            var res = App.Get(View);
                            if (res == null) {
                                // TODO
                                //res = StaticResources.Handle( HttpRequest.GET( "/" + View ) ); 
                            }
                            if (res is HttpResponse) {
                                var response = res as HttpResponse;
                                sb.Append(JsonConvert.SerializeObject(Encoding.UTF8.GetString(response.Uncompressed)));
                            }
                            else {
                                throw new NotImplementedException();
                            }
                        }
                        //else {
                        //    sb.Append("__vf:");
                        //    sb.Append(JsonConvert.SerializeObject(Media.Content.FilePath.ToString()));
                        //}
                }

            }

            if (includeSessionId)
            {
                if (t > 0)
                    sb.Append(',');
                sb.Append("\"View-Model\":");
                sb.Append("\"1\"");
            }

//            if (t > 0)
//                sb.Append(',');
//            sb.Append("$Class:");
//            sb.Append(JsonConvert.SerializeObject(templ.ClassName));

            sb.Append('}');
            IsSerialized = true;
            return sb.ToString();
#else
            throw new NotImplementedException();
#endif
        }
    }
}
