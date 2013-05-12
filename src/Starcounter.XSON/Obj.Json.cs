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
using Starcounter.Internal;

namespace Starcounter {
    // TODO: 
    // Not sure where this class should be 
    public abstract class CodegeneratedJsonSerializer {
        public abstract int Serialize(IntPtr buffer, int bufferSize, dynamic obj);
        public abstract int Populate(IntPtr buffer, int bufferSize, dynamic obj);
    }

    /// <summary>
    /// Class App
    /// </summary>
    public partial class Obj {
        // byte[] ToJsonUtf8        --> Both slow and generated 
        // string ToJson            --> Both slow and generated
        // ToJson(byte[] buffer)    --> Not supported without generated.
        // ToJson(IntPtr buffer)    --> Not supported without generated.
        public byte[] ToJsonUtf8() {
            // TODO: 
            // Make this function virtual and override it when generating code.
            // Then this branch is not needed.
            if (Template.UseCodegeneratedSerializer) {
                return ToJsonUtf8_Codegen();
            } else {
                return System.Text.Encoding.UTF8.GetBytes(ToJson_Slow());
            }
        }

        public string ToJson() {
            // TODO: 
            // Make this function virtual and override it when generating code.
            // Then this branch is not needed.
            if (Template.UseCodegeneratedSerializer) {
                return System.Text.Encoding.UTF8.GetString(ToJsonUtf8_Codegen());
            } else {
                return ToJson_Slow();
            }
        }

        public int ToJson(IntPtr buffer, int bufferSize) {
            var codeGenSerializer = Template.GetSerializer();
            if (codeGenSerializer != null) {
                return codeGenSerializer.Serialize(buffer, bufferSize, this);
            } else {
                throw new NotSupportedException("This function is only valid when using codegenerated serializer.");
            }
        }

        public int ToJson(byte[] buffer) {
            int usedSize = 0;

            unsafe {
                fixed (byte* p = buffer) {
                    usedSize = ToJson((IntPtr)p, buffer.Length);
                }
            }
            return usedSize;
        }

        public int Populate(IntPtr buffer, int bufferSize) {
            var codeGenSerializer = Template.GetSerializer();
            if (codeGenSerializer != null) {
                return codeGenSerializer.Populate(buffer, bufferSize, this);
            } else {
                throw new NotSupportedException("This function is only valid when using codegenerated serializer.");
            }
        }

        /// <summary>
        /// Serializes the current instance to a bytearray containing UTF8 encoded bytes.
        /// </summary>
        /// <returns>A bytearray containing the serialized json.</returns>
        private byte[] ToJsonUtf8_Codegen() {
            int startBufferSize = 4096;
            int incAmount = 1;
            byte[] buffer;
            int usedSize = -1;

            // TODO:
            // How do we handle creating buffer and increasing buffer if size is not enough?
            
            while (true) {
                buffer = new byte[startBufferSize * incAmount];

                // TODO:
                // Change generated code to not throw any exceptions but return errorcodes.
                try {
                    usedSize = ToJson(buffer);
                    break;
                } catch (Exception ex) {
                    if (ErrorCode.IsFromErrorCode(ex)) {
                        incAmount = incAmount * 4;
                        if (incAmount > 4096)
                            throw;
                    } else
                        throw;
                }
            }

            if (usedSize != -1) {
                byte[] retArr = new byte[usedSize];
                Buffer.BlockCopy(buffer, 0, retArr, 0, usedSize);
                return retArr;
            }
            return null;
        }

        /// <summary>
        /// To the json.
        /// </summary>
        /// <returns>System.String.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public string ToJson_Slow() { 
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
                                            sb.Append(prop.TemplateName);
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
                    sb.Append(prop.TemplateName);
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


//            t += InsertAdditionalJsonProperties(sb, t > 0);

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

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="sb"></param>
        ///// <param name="addComma"></param>
        ///// <returns></returns>
        //protected virtual int InsertAdditionalJsonProperties(StringBuilder sb, bool addComma) {
        //    return 0;
        //}

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

            try {
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
