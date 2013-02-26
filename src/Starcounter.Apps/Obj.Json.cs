// ***********************************************************************
// <copyright file="App.Json.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
using Newtonsoft.Json;
using Starcounter.Templates;

namespace Starcounter {

    /// <summary>
    /// Class App
    /// </summary>
    public partial class Obj {
//        private char[] Session;
        /// <summary>
        /// To the json UTF8.
        /// </summary>
        /// <returns>System.Byte[][].</returns>
        /// <remarks>Needs optimization. Should build JSON directly from TurboText or static UTF8 bytes
        /// to UTF8. This suboptimal version first builds Windows UTF16 strings that are ultimatelly
        /// not used.</remarks>
        public byte[] ToJsonUtf8() {
            return Encoding.UTF8.GetBytes(ToJson());
        }

        /// <summary>
        /// To the json.
        /// </summary>
        /// <returns>System.String.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual string ToJson() { //, IncludeView includeViewContent = IncludeView.Default) {
#if QUICKTUPLE
            var sb = new StringBuilder();
            var templ = this.Template;
            int t = 0;
            //if (includeSchema)
            //    sb.Append("{$$:{},");
            //else
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
                    if (prop is TObjArr) {
                        sb.Append('[');
                        int i = 0;
                        foreach (var x in val as Arr) {
                            if (i++ > 0) {
                                sb.Append(',');
                            }
                            sb.Append(x.ToJson());
                        }
                        sb.Append(']');
                    }
                    else if (prop is TObj) {
//                       var x = includeViewContent;
//                       if (x == IncludeView.Default)
//                          x = IncludeView.Always;
                       sb.Append(((Obj)val).ToJson());
                    }
                    else {
                        object papa = val;
                        if (prop.Bound)
                            papa = prop.GetBoundValueAsObject(this);
                       
                       sb.Append(JsonConvert.SerializeObject(papa));
                    }
                    needsComma = true;
                }
            }
//            var view = Media.FileName ?? templ.PropertyName;


            t += InsertAdditionalJsonProperties(sb, t > 0);

//            if (t > 0)
//                sb.Append(',');
//            sb.Append("$Class:");
//            sb.Append(JsonConvert.SerializeObject(templ.ClassName));

            sb.Append('}');
            return sb.ToString();
#else
            throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="addComma"></param>
        /// <returns></returns>
        internal virtual int InsertAdditionalJsonProperties(StringBuilder sb, bool addComma) {
            return 0;
        }

    }
}
