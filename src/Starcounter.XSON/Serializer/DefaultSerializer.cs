
using System;
using System.Runtime.InteropServices;
using System.Text;
using Starcounter.Internal;
using Starcounter.Templates;

namespace Starcounter.XSON.Serializers {
    public class DefaultSerializer : TypedJsonSerializer {
        public static readonly TypedJsonSerializer Instance = new DefaultSerializer();

        public override string ToJson(Obj obj) {
            byte[] buffer;
            int count = ToJsonUtf8(obj, out buffer);
            return Encoding.UTF8.GetString(buffer, 0, count);
        }

        public override byte[] ToJsonUtf8(Obj obj) {
            byte[] buffer;
            byte[] sizedBuffer;
            int count = ToJsonUtf8(obj, out buffer);
            sizedBuffer = new byte[count];
            Buffer.BlockCopy(buffer, 0, sizedBuffer, 0, count);
            return sizedBuffer;
        }

        public override int ToJsonUtf8(Obj obj, out byte[] buffer) {
            bool nameWritten;
            bool recreateBuffer;
            byte[] buf;
            byte[] childObjArr;
            int templateNo;
            int posInArray;
            int valueSize;
            int offset;
            long bufferSize;
            Obj childObj;
            Template tProperty;
            TObj tObj;

            // The following variables are offset for remembering last position when buf needs to be increased:
            // bufferSize: The size of the buf. This value is increased when needed.
            // templateNo: The position in the PropertyList that was about to be written.
            // offset: The last verified position in the buf that was succesfully written.
            // nameWritten: If set to true, the name of the template is succesfully written (but not the value).
            // childObjArr: If last value was an object or an object in an array, the array contains the serialized object.
            // posInArray: Set to the last succesful copied value for an objectarray.

            // Initial values:
            bufferSize = 512;

            tObj = obj.Template;
            buf = new byte[bufferSize];
            templateNo = 0;
            nameWritten = false;
            childObjArr = null;
            posInArray = -1;
            recreateBuffer = false;
            valueSize = -1;

            unsafe {
                buf[0] = (byte)'{';
                offset = 1;

restart:
                if (recreateBuffer)
                    buf = IncreaseCapacity(buf, offset, valueSize);
                recreateBuffer = true;

                // Starting from the last written position
                fixed (byte* p = &buf[offset]) {
                    byte* pfrag = p;
                    for (int i = templateNo; i < tObj.Properties.Count; i++) {
                        tProperty = tObj.Properties[i];

                        // Property name.
                        if (!nameWritten) {
                            valueSize = JsonHelper.WriteString((IntPtr)pfrag, buf.Length - offset, tProperty.TemplateName);
                            if (valueSize == -1 || (buf.Length < (offset + valueSize + 1))) {
                                nameWritten = false;
                                goto restart;
                            }
                            nameWritten = true;
                            offset += valueSize;
                            pfrag += valueSize;

                            *pfrag++ = (byte)':';
                            offset++;
                        }

                        // Property value.
                        if (tProperty is TObj) {
                            if (childObjArr == null) {
                                childObj = obj.Get((TObj)tProperty);
                                if (childObj != null) {
                                    valueSize = childObj.ToJsonUtf8(out childObjArr);
                                } else {
                                    valueSize = JsonHelper.WriteNull((IntPtr)pfrag, buf.Length - offset);
                                    if (valueSize == -1)
                                        goto restart;
                                }
                            }

                            if (valueSize != -1 && childObjArr != null) {
                                if (buf.Length < (offset + valueSize))
                                    goto restart;
                                Buffer.BlockCopy(childObjArr, 0, buf, offset, valueSize);
                                pfrag += valueSize;
                                offset += valueSize;
                                childObjArr = null;
                            } else
                                goto restart;
                        } else if (tProperty is TObjArr) {
                            Arr arr = obj.Get((TObjArr)tProperty);
                            if (buf.Length < (offset + arr.Count * 2 + 2))
                                goto restart;

                            // We know that we at least have room for all start-end characters in the buf.
                            if (posInArray == -1) {
                                *pfrag++ = (byte)'[';
                                offset++;
                                posInArray = 0;
                            }
                            for (int arrPos = posInArray; arrPos < arr.Count; arrPos++) {
                                if (childObjArr == null) {
                                    valueSize = arr[arrPos].ToJsonUtf8(out childObjArr);
                                    if (valueSize == -1)
                                        goto restart;
                                    if (buf.Length < (offset + valueSize + 1))
                                        goto restart;
                                }

                                Buffer.BlockCopy(childObjArr, 0, buf, offset, valueSize);
                                childObjArr = null;
                                pfrag += valueSize;
                                offset += valueSize;
                                posInArray++;

                                if ((arrPos + 1) < arr.Count) {
                                    *pfrag++ = (byte)',';
                                    offset++;
                                }
                            }
                            *pfrag++ = (byte)']';
                            offset++;
                            posInArray = -1;
                        } else {
                            if (tProperty is TBool) {
                                valueSize = JsonHelper.WriteBool((IntPtr)pfrag, buf.Length - offset, obj.Get((TBool)tProperty));
                            } else if (tProperty is TDecimal) {
                                valueSize = JsonHelper.WriteDecimal((IntPtr)pfrag, buf.Length - offset, obj.Get((TDecimal)tProperty));
                            } else if (tProperty is TDouble) {
                                valueSize = JsonHelper.WriteDouble((IntPtr)pfrag, buf.Length - offset, obj.Get((TDouble)tProperty));
                            } else if (tProperty is TLong) {
                                valueSize = JsonHelper.WriteInt((IntPtr)pfrag, buf.Length - offset, obj.Get((TLong)tProperty));
                            } else if (tProperty is TString) {
                                valueSize = JsonHelper.WriteString((IntPtr)pfrag, buf.Length - offset, obj.Get((TString)tProperty));
                            } else if (tProperty is TTrigger) {
                                valueSize = JsonHelper.WriteNull((IntPtr)pfrag, buf.Length - offset);
                            }

                            if ((valueSize == -1) || (buf.Length < (offset + valueSize + 1)))
                                goto restart;
                            pfrag += valueSize;
                            offset += valueSize;
                        }

                        if ((i + 1) < tObj.Properties.Count) {
                            *pfrag++ = (byte)',';
                            offset++;
                        }
                        templateNo++;
                        nameWritten = false;
                    }

                    if (buf.Length < (offset + 1))
                        goto restart; // Bummer! we dont have any place left for the last char :(
                    *pfrag = (byte)'}';
                    offset++;
                }
            }
            buffer = buf;
            return offset;

        }

        private static byte[] IncreaseCapacity(byte[] current, int offset, int needed) {
            byte[] tmpBuffer;
            long bufferSize = current.Length;

            bufferSize *= 2;
            if (needed != -1) {
                while (bufferSize < (offset + needed))
                    bufferSize *= 2;
            }

//            System.Diagnostics.Debug.WriteLine("Increasing buffer, new size: " + bufferSize);

            tmpBuffer = new byte[bufferSize];
            Buffer.BlockCopy(current, 0, tmpBuffer, 0, offset);
            return tmpBuffer;
        }

        public override int PopulateFromJson(Obj obj, string json) {
            //using (JsonTextReader reader = new JsonTextReader(new StringReader(json))) {
            //    if (reader.Read()) {

            //        if (!(reader.TokenType == JsonToken.StartObject)) {
            //            throw new Exception("Invalid json data. Cannot populate object");
            //        }
            //        PopulateObject(obj, reader);
            //    }
            //    return -1;
            //}
            return -1;
        }

        public override int PopulateFromJson(Obj obj, byte[] buffer, int bufferSize) {
            return PopulateFromJson(obj, Encoding.UTF8.GetString(buffer, 0, bufferSize));
        }

        public override int PopulateFromJson(Obj obj, IntPtr buffer, int jsonSize) {
            byte[] jsonArr = new byte[jsonSize];
            Marshal.Copy(buffer, jsonArr, 0, jsonSize);
            return PopulateFromJson(obj, jsonArr, jsonSize);
        }

        ///// <summary>
        ///// Poplulates the object with values from read from the jsonreader. This method is recursively
        ///// called for each new object that is parsed from the json.
        ///// </summary>
        ///// <param name="obj">The object to set the parsed values in</param>
        ///// <param name="reader">The JsonReader containing the json to be parsed.</param>
        //private void PopulateObject(Obj obj, JsonReader reader) {
        //    bool insideArray = false;
        //    Template tChild = null;
        //    TObj tobj = obj.Template;
            
        //    try {
        //        while (reader.Read()) {
        //            switch (reader.TokenType) {
        //                case JsonToken.StartObject:
        //                    Obj newObj;
        //                    if (insideArray) {
        //                        newObj = obj.Get((TObjArr)tChild).Add();
        //                    } else {
        //                        newObj = obj.Get((TObj)tChild);
        //                    }
        //                    PopulateObject(newObj, reader);
        //                    break;
        //                case JsonToken.EndObject:
        //                    return;
        //                case JsonToken.PropertyName:
        //                    var tname = (string)reader.Value;
        //                    tChild = tobj.Properties.GetTemplateByName(tname);
        //                    if (tChild == null) {
        //                        throw ErrorCode.ToException(Error.SCERRJSONPROPERTYNOTFOUND, string.Format("Property=\"{0}\"", tname), (msg, e) => {
        //                            return new FormatException(msg, e);
        //                        });
        //                    }
        //                    break;
        //                case JsonToken.String:
        //                    obj.Set((TString)tChild, (string)reader.Value);
        //                    break;
        //                case JsonToken.Integer:
        //                    obj.Set((TLong)tChild, (long)reader.Value);
        //                    break;
        //                case JsonToken.Boolean:
        //                    obj.Set((TBool)tChild, (bool)reader.Value);
        //                    break;
        //                case JsonToken.Float:
        //                    if (tChild is TDecimal) {
        //                        obj.Set((TDecimal)tChild, Convert.ToDecimal(reader.Value));
        //                    } else {
        //                        obj.Set((TDouble)tChild, (double)reader.Value);
        //                    }
        //                    break;
        //                case JsonToken.StartArray:
        //                    insideArray = true;
        //                    break;
        //                case JsonToken.EndArray:
        //                    insideArray = false;
        //                    break;
        //                default:
        //                    throw new NotImplementedException();
        //            }
        //        }
        //    } catch (InvalidCastException castException) {
        //        switch (reader.TokenType) {
        //            case JsonToken.String:
        //            case JsonToken.Integer:
        //            case JsonToken.Boolean:
        //            case JsonToken.Float:
        //                throw ErrorCode.ToException(
        //                    Error.SCERRJSONVALUEWRONGTYPE,
        //                    castException,
        //                    string.Format("Property=\"{0} ({1})\", Value=\"{2}\"", tChild.PropertyName, tChild.JsonType, reader.Value.ToString()),
        //                    (msg, e) => {
        //                        return new FormatException(msg, e);
        //                    });
        //            default:
        //                throw;
        //        }
        //    }
        //}
    }
}