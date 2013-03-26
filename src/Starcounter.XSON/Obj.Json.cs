// ***********************************************************************
// <copyright file="App.Json.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.IO;
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
                        TValue valueProperty = prop as TValue;
                        if (valueProperty != null && valueProperty.Bound)
                            papa = valueProperty.GetBoundValueAsObject(this);
                       
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
        protected virtual int InsertAdditionalJsonProperties(StringBuilder sb, bool addComma) {
            return 0;
        }

        /// <summary>
        /// Populates the current object with values parsed from the specified json string.
        /// </summary>
        /// <param name="json"></param>
        public void PopulateFromJson(string json) {
            using (JsonTextReader reader = new JsonTextReader(new StringReader(json))) {
                if (reader.Read()) {
                    if (!(reader.TokenType == JsonToken.StartObject)) {
                        throw new Exception("Invalid json data. Cannot populate object");
                    }
                    PopulateObject(this, reader);
                }
            }
        }

        /// <summary>
        /// Poplulates the object with values from read from the jsonreader. This method is recursively
        /// called for each new object that is parsed from the json.
        /// </summary>
        /// <param name="obj">The object to set the parsed values in</param>
        /// <param name="reader">The JsonReader containing the json to be parsed.</param>
        private void PopulateObject(Obj obj, JsonReader reader) {
            bool insideArray = false;
            Template tChild = null;
            TObj tobj = obj.Template;
            
            while (reader.Read()) {
                switch (reader.TokenType) {
                    case JsonToken.StartObject:
                        Obj newObj;
                        if (insideArray) {
                            newObj = obj.Get((TObjArr)tChild).Add();
                        } else {
                            newObj = obj.Get((TObj)tChild);
                        }
                        PopulateObject(newObj, reader);
                        break;
                    case JsonToken.EndObject:
                        return;
                    case JsonToken.PropertyName:
                        tChild = tobj.Properties.GetTemplateByName((string)reader.Value);
                        if (tChild == null) {
                            // TODO: 
                            // How should we handle properties in the json string that does not exist in the Obj?
                            throw new Exception("Unknown property '" + reader.Value + "' found in json. Cannot populate object.");
                        }
                        break;
                    case JsonToken.String:
                        obj.Set((TString)tChild, (string)reader.Value);
                        break;
                    case JsonToken.Integer:
                        obj.Set((TLong)tChild, (long)reader.Value);
                        break;
                    case JsonToken.Boolean:
                        obj.Set((TBool)tChild, (bool)reader.Value);
                        break;
                    case JsonToken.Float:
                        if (tChild is TDecimal) {
                            obj.Set((TDecimal)tChild, Convert.ToDecimal(reader.Value));
                        } else {
                            obj.Set((TDouble)tChild, (double)reader.Value);
                        }
                        break;
                    case JsonToken.StartArray:
                        insideArray = true;
                        break;
                    case JsonToken.EndArray:
                        insideArray = false;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}
