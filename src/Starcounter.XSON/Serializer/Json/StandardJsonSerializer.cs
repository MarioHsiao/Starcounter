
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Starcounter.Internal;
using Starcounter.Templates;
using System.Diagnostics;
using System.Collections;

namespace Starcounter.Advanced.XSON {

	public abstract class StandardJsonSerializerBase : TypedJsonSerializer {

        public override int EstimateSizeBytes(Json obj) {
            var transaction = obj.Transaction;
            if (transaction != null) {
                return transaction.AddAndReturn<Json, int>(_EstimateSizeBytes, obj);
            }
            return _EstimateSizeBytes(obj);
        }

        public override int Serialize(Json obj, byte[] buf, int origOffset) {
            var transaction = obj.Transaction;
            if (transaction != null) {
                return transaction.AddAndReturn<Json, byte[], int, int>(_Serialize, obj, buf, origOffset);
            }
            return _Serialize(obj, buf, origOffset);
        }

        /// <summary>
        /// Estimates the size of serialization in bytes.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private int _EstimateSizeBytes(Json obj) {
            int sizeBytes = 0;
            bool addAppName = false;

            if (obj.IsArray) {

                sizeBytes = 2; // 2 for "[]".

                for (int arrPos = 0; arrPos < ((IList)obj).Count; arrPos++) {
                    sizeBytes += EstimateSizeBytes(obj._GetAt(arrPos) as Json) + 1; // 1 for ",".
                }

                return sizeBytes;
            }

            sizeBytes += 1; // 1 for "{".

            if (obj.Template == null) {
                sizeBytes += 1; // 1 for "}".
                return sizeBytes;
            }

            List<Template> exposedProperties;
            Json childObj;
            Template tProperty;

            addAppName = (obj._stepParent == null && obj._appName != null);

            if (addAppName) {
//                Debugger.Launch();

                sizeBytes += obj._appName.Length + 4; // 2 for ":{" and 2 for quotation marks around string.
                sizeBytes += "/polyjuice-merger?".Length + obj._appName.Length + 1 + obj.GetHtmlPartialUrl().Length; // 1 for "=".
            }

            exposedProperties = ((TObject)obj.Template).Properties.ExposedProperties;
            for (int i = 0; i < exposedProperties.Count; i++) {

                tProperty = exposedProperties[i];

                sizeBytes += tProperty.TemplateName.Length + 3; // 1 for ":" and to for quotation marks around string.

                // Property value.
                if (tProperty is TObject) {

                    childObj = ((TObject)tProperty).Getter(obj);
                    if (childObj != null) {
                        sizeBytes += EstimateSizeBytes(childObj);
                    } else {
                        sizeBytes += 2; // 2 for "{}".
                    }
                } else if (tProperty is TObjArr) {

                    Json arr = ((TObjArr)tProperty).Getter(obj);

                    sizeBytes += 1; // 1 for "[".

                    for (int arrPos = 0; arrPos < ((IList)arr).Count; arrPos++) {
                        sizeBytes += EstimateSizeBytes(arr._GetAt(arrPos) as Json) + 1; // 1 for ",".
                    }

                    sizeBytes += 1; // 1 for "]".
                } else {
                    if (tProperty is TBool) {
                        sizeBytes += 5;
                    } else if (tProperty is TDecimal) {
                        sizeBytes += 32;
                    } else if (tProperty is TDouble) {
                        sizeBytes += 32;
                    } else if (tProperty is TLong) {
                        sizeBytes += 32;
                    } else if (tProperty is TString) {
                        String s = ((TString)tProperty).Getter(obj);

                        if (s == null)
                            sizeBytes += 2; // 2 for quotation marks around string.
                        else
                            sizeBytes += s.Length * 2 + 2; // 2 for quotation marks around string.
                        
                    } else if (tProperty is TTrigger) {
                        sizeBytes += 4;
                    }
                }

                sizeBytes += 1; // 1 for comma.
            }

            if (addAppName) {
                sizeBytes += 9; // 1 for comma, 6 for "Html", 1 for ':' and 1 for '}'
            }

            if (obj._stepSiblings != null && obj._stepSiblings.Count != 0) {
                foreach (Json pp in obj._stepSiblings) {
                    sizeBytes += 4 + pp._appName.Length + pp.GetHtmlPartialUrl().Length; // 2 for "&" and "=" and 2 for quotation marks around string.
                    sizeBytes += pp._appName.Length + 1; // 1 for ":".
                    sizeBytes += EstimateSizeBytes(pp) + 1; // 1 for ",".
                }
            }

            sizeBytes += 1; // 1 for "}".

            return sizeBytes;
        }

        /// <summary>
        /// Serializes given JSON object.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="buf"></param>
        /// <param name="origOffset"></param>
        /// <returns></returns>
        private int _Serialize(Json obj, byte[] buf, int origOffset) {
            int valueSize;
            List<Template> exposedProperties;
            TObject tObj;
            int offset = origOffset;
            String htmlUriMerged = null;

            bool addAppName = false;

            unsafe {
                // Starting from the last written position
                fixed (byte* p = &buf[offset]) {
                    byte* pfrag = p;

                    // Processing array first.
                    if (obj.IsArray) {

                        *pfrag++ = (byte)'[';
                        offset++;

                        for (int arrPos = 0; arrPos < ((IList)obj).Count; arrPos++) {
                            valueSize = (obj._GetAt(arrPos) as Json).ToJsonUtf8(buf, offset);

                            pfrag += valueSize;
                            offset += valueSize;

                            if ((arrPos + 1) < ((IList)obj).Count) {
                                *pfrag++ = (byte)',';
                                offset++;
                            }
                        }
                        *pfrag++ = (byte)']';
                        offset++;

                        return offset - origOffset; ;
                    }

                    // If its not an array, its an object.
                    *pfrag++ = (byte)'{';
                    offset++;

                    addAppName = (obj._stepParent == null && obj._appName != null);

                    if (addAppName) {
                        htmlUriMerged = "/polyjuice-merger?" + obj._appName + "=" + obj.GetHtmlPartialUrl();

                        valueSize = JsonHelper.WriteStringAsIs((IntPtr)pfrag, buf.Length - offset, obj._appName);
                        offset += valueSize;
                        pfrag += valueSize;

                        *pfrag++ = (byte)':';
                        offset++;

                        *pfrag++ = (byte)'{';
                        offset++;
                    }

                    tObj = (TObject)obj.Template;

                    exposedProperties = tObj.Properties.ExposedProperties;
                    for (int i = 0; i < exposedProperties.Count; i++) {
                        Template tProperty = exposedProperties[i];

                        // Property name.
                        //                            valueSize = JsonHelper.WriteString((IntPtr)pfrag, buf.Length - offset, tProperty.TemplateName);
                        valueSize = JsonHelper.WriteStringAsIs((IntPtr)pfrag, buf.Length - offset, tProperty.TemplateName);

                        offset += valueSize;
                        pfrag += valueSize;

                        *pfrag++ = (byte)':';
                        offset++;

                        // Property value.
                        if (tProperty is TObject) {
                            Json childObj = ((TObject)tProperty).Getter(obj);
                            if (childObj != null) {
                                valueSize = childObj.ToJsonUtf8(buf, offset);

                                pfrag += valueSize;
                                offset += valueSize;
                            } else {
                                valueSize = 2;

                                pfrag[0] = (byte)'{';
                                pfrag[1] = (byte)'}';

                                pfrag += valueSize;
                                offset += valueSize;
                            }
                        } else if (tProperty is TObjArr) {
                            Json arr = ((TObjArr)tProperty).Getter(obj);

                            *pfrag++ = (byte)'[';
                            offset++;

                            for (int arrPos = 0; arrPos < ((IList)arr).Count; arrPos++) {
                                valueSize = (arr._GetAt(arrPos) as Json).ToJsonUtf8(buf, offset);

                                pfrag += valueSize;
                                offset += valueSize;

                                if ((arrPos + 1) < ((IList)arr).Count) {
                                    *pfrag++ = (byte)',';
                                    offset++;
                                }
                            }
                            *pfrag++ = (byte)']';
                            offset++;
                        } else {
                            if (tProperty is TBool) {
                                valueSize = JsonHelper.WriteBool((IntPtr)pfrag, buf.Length - offset, ((TBool)tProperty).Getter(obj));
                            } else if (tProperty is TDecimal) {
                                valueSize = JsonHelper.WriteDecimal((IntPtr)pfrag, buf.Length - offset, ((TDecimal)tProperty).Getter(obj));
                            } else if (tProperty is TDouble) {
                                valueSize = JsonHelper.WriteDouble((IntPtr)pfrag, buf.Length - offset, ((TDouble)tProperty).Getter(obj));
                            } else if (tProperty is TLong) {
                                valueSize = JsonHelper.WriteInt((IntPtr)pfrag, buf.Length - offset, ((TLong)tProperty).Getter(obj));
                            } else if (tProperty is TString) {
                                valueSize = JsonHelper.WriteString((IntPtr)pfrag, buf.Length - offset, ((TString)tProperty).Getter(obj));
                            } else if (tProperty is TTrigger) {
                                valueSize = JsonHelper.WriteNull((IntPtr)pfrag, buf.Length - offset);
                            }

                            pfrag += valueSize;
                            offset += valueSize;
                        }

                        if ((i + 1) < exposedProperties.Count) {
                            *pfrag++ = (byte)',';
                            offset++;
                        }
                    }

                    if (addAppName) {
                        *pfrag++ = (byte)'}';
                        offset++;

                        *pfrag++ = (byte)',';
                        offset++;
                    }

                    // Checking if we have Json siblings on this level.
                    if (obj._stepSiblings != null && obj._stepSiblings.Count != 0) {
                        // Serializing every sibling first.
                        foreach (Json pp in obj._stepSiblings) {
                            htmlUriMerged += "&" + pp._appName + "=" + pp.GetHtmlPartialUrl();

                            valueSize = JsonHelper.WriteStringAsIs((IntPtr)pfrag, buf.Length - offset, pp._appName);
                            offset += valueSize;
                            pfrag += valueSize;

                            *pfrag++ = (byte)':';
                            offset++;

                            valueSize = pp.ToJsonUtf8(buf, offset);
                            pfrag += valueSize;
                            offset += valueSize;

                            *pfrag++ = (byte)',';
                            offset++;
                        }
                    }

                    if (addAppName) {
                        // Adding Html property to outer level.
                        valueSize = JsonHelper.WriteStringAsIs((IntPtr)pfrag, buf.Length - offset, "Html");
                        offset += valueSize;
                        pfrag += valueSize;

                        *pfrag++ = (byte)':';
                        offset++;

                        valueSize = JsonHelper.WriteString((IntPtr)pfrag, buf.Length - offset, htmlUriMerged);
                        offset += valueSize;
                        pfrag += valueSize;
                    }

                    *pfrag++ = (byte)'}';
                    offset++;
                }
            }
            return offset - origOffset;
        }
	}

    public class StandardJsonSerializer : StandardJsonSerializerBase {

        public override int Populate(Json obj, IntPtr source, int sourceSize) {
            string propertyName;

            if (obj.IsArray) {
                throw new NotImplementedException("Cannot serialize JSON where the root object is an array");
            }

            var reader = new JsonReader(source, sourceSize);
            Json arr;
            Json childObj;
            TObject tObj = (TObject)obj.Template;
            Template tProperty;
            JsonReader arrayReader;

            while (reader.GotoProperty()) {
                propertyName = reader.ReadString();
                tProperty = tObj.Properties.GetExposedTemplateByName(propertyName);
                if (tProperty == null) {
                    JsonHelper.ThrowPropertyNotFoundException(propertyName);
                }

                reader.GotoValue();
                try {
                    if (tProperty is TBool) {
                        ((TBool)tProperty).Setter(obj, reader.ReadBool());
                    } else if (tProperty is TDecimal) {
                        ((TDecimal)tProperty).Setter(obj, reader.ReadDecimal());
                    } else if (tProperty is TDouble) {
                        ((TDouble)tProperty).Setter(obj, reader.ReadDouble());
                    } else if (tProperty is TLong) {
                        ((TLong)tProperty).Setter(obj, reader.ReadLong());
                    } else if (tProperty is TString) {
                        ((TString)tProperty).Setter(obj, reader.ReadString());
                    }
                    else if (tProperty is TObject) {
                        childObj = ((TObject)tProperty).Getter(obj);
                        reader.PopulateObject(childObj);
                    } else if (tProperty is TObjArr) {
                        arr = ((TObjArr)tProperty).Getter(obj);
                        arrayReader = reader.CreateSubReader();
                        while (arrayReader.GotoNextObject()) {
                            childObj = arr.NewItem();
                            arrayReader.PopulateObject(childObj);
                        }
                        reader.Skip(arrayReader.Used);
                    }
                } catch (InvalidCastException ex) {
                    JsonHelper.ThrowWrongValueTypeException(ex, tProperty.TemplateName, tProperty.JsonType, reader.ReadString());
                }
            }
            return reader.Used;
        }
    }
}