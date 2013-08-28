﻿
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using Starcounter.Internal;
using Starcounter.Templates;
using Starcounter.Advanced.XSON;
using TJson = Starcounter.Templates.Schema<Starcounter.Json<object>>;

namespace Starcounter.Internal.XSON {
    public class NewtonsoftSerializer : TypedJsonSerializer {
        public override string ToJson(Json<object> obj) {
            bool needsComma;
            int t;
            StringBuilder sb;
            Template tProp;
            TJson tObj;

            if (obj.IsPrimitive || obj.IsArray) {
                throw new NotImplementedException("Serialization of Json objects where the root element is an array or a primtive object is not supported");
            }

            tObj = (TJson)obj.Template;
            t = 0;
            needsComma = false;

            sb = new StringBuilder();
            sb.Append('{');
            foreach (object val in obj._Values) {
                tProp = tObj.Properties[t++];
                if (tProp.HasInstanceValueOnClient) {
                    if (needsComma) {
                        sb.Append(',');
                    }
                    sb.Append('"');
                    sb.Append(tProp.TemplateName);
                    sb.Append('"');
                    sb.Append(':');
                    if (tProp is TObjArr) {
                        sb.Append('[');
                        int i = 0;
                        foreach (var x in val as Arr) {
                            if (i++ > 0) {
                                sb.Append(',');
                            }
                            sb.Append(x.ToJson());
                        }
                        sb.Append(']');
                    } else if (tProp is TJson) {
                        sb.Append(((Json<object>)val).ToJson());
                    } else {
                        object papa = val;
                        TValue valueProperty = tProp as TValue;
                        if (valueProperty != null && ( valueProperty.Bound == Bound.Yes ) )
                            papa = valueProperty.GetBoundValueAsObject(obj);

                        sb.Append(JsonConvert.SerializeObject(papa));
                    }
                    needsComma = true;
                }
            }
            sb.Append('}');
            return sb.ToString();
        }

        public override byte[] ToJsonUtf8(Json<object> obj) {
            return Encoding.UTF8.GetBytes(ToJson(obj));
        }

        public override int ToJsonUtf8(Json<object> obj, out byte[] buffer) {
            buffer = ToJsonUtf8(obj);
            return buffer.Length;
        }

        public override int PopulateFromJson(Json<object> obj, string json) {
            using (JsonTextReader reader = new JsonTextReader(new StringReader(json))) {
                if (reader.Read()) {

                    if (!(reader.TokenType == JsonToken.StartObject)) {
                        throw new Exception("Invalid json data. Cannot populate object");
                    }
                    PopulateObject(obj, reader);
                }
                return -1;
            }
        }

        public override int PopulateFromJson(Json<object> obj, byte[] buffer, int bufferSize) {
            return PopulateFromJson(obj, Encoding.UTF8.GetString(buffer, 0, bufferSize));
        }

        public override int PopulateFromJson(Json<object> obj, IntPtr buffer, int jsonSize) {
            byte[] jsonArr = new byte[jsonSize];
            Marshal.Copy(buffer, jsonArr, 0, jsonSize);
            return PopulateFromJson(obj, jsonArr, jsonSize);
        }

        /// <summary>
        /// Poplulates the object with values from read from the jsonreader. This method is recursively
        /// called for each new object that is parsed from the json.
        /// </summary>
        /// <param name="obj">The object to set the parsed values in</param>
        /// <param name="reader">The JsonReader containing the json to be parsed.</param>
        private void PopulateObject(Json<object> obj, Newtonsoft.Json.JsonReader reader) {
            bool insideArray = false;
            Template tChild = null;

            if (obj.IsPrimitive || obj.IsArray) {
                throw new NotImplementedException("Deserialization of Json objects where the root element is an array or a primtive object is not supported");
            }


            TJson tobj = (TJson)obj.Template;
            
            try {
                while (reader.Read()) {
                    switch (reader.TokenType) {
                        case JsonToken.StartObject:
                            Json<object> newObj;
                            if (insideArray) {
                                newObj = obj.Get((TObjArr)tChild).Add();
                            } else {
                                newObj = obj.Get((TJson)tChild);
                            }
                            PopulateObject(newObj, reader);
                            break;
                        case JsonToken.EndObject:
                            return;
                        case JsonToken.PropertyName:
                            var tname = (string)reader.Value;
                            tChild = tobj.Properties.GetTemplateByName(tname);
                            if (tChild == null) {
                                throw ErrorCode.ToException(Error.SCERRJSONPROPERTYNOTFOUND, string.Format("Property=\"{0}\"", tname), (msg, e) => {
                                    return new FormatException(msg, e);
                                });
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
                            if (tChild is Property<decimal>) {
                                obj.Set((Property<decimal>)tChild, Convert.ToDecimal(reader.Value));
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
            } catch (InvalidCastException castException) {
                switch (reader.TokenType) {
                    case JsonToken.String:
                    case JsonToken.Integer:
                    case JsonToken.Boolean:
                    case JsonToken.Float:
                        throw ErrorCode.ToException(
                            Error.SCERRJSONVALUEWRONGTYPE,
                            castException,
                            string.Format("Property=\"{0} ({1})\", Value=\"{2}\"", tChild.PropertyName, tChild.JsonType, reader.Value.ToString()),
                            (msg, e) => {
                                return new FormatException(msg, e);
                            });
                    default:
                        throw;
                }
            }
        }
    }
}