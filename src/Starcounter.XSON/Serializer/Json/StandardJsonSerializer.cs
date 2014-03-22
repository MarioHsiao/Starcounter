﻿
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Starcounter.Internal;
using Starcounter.Templates;
using System.Diagnostics;

namespace Starcounter.Advanced.XSON {

	public abstract class StandardJsonSerializerBase : TypedJsonSerializer {

        /// <summary>
        /// Estimates the size of serialization in bytes.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override int EstimateSizeBytes(Json obj) {
            int sizeBytes = 0;

            obj.ExecuteInScope(() => {

            if (obj.IsArray) {

                sizeBytes = 2; // 2 for "[]".

                for (int arrPos = 0; arrPos < obj.Count; arrPos++) {
                    sizeBytes += EstimateSizeBytes(obj._GetAt(arrPos) as Json) + 1; // 1 for ",".
                }

                return;
            }

            sizeBytes += 1; // 1 for "{".

            if (obj.Template == null) {
                sizeBytes += 1; // 1 for "}".
                return;
            }

            if (obj.JsonSiblings.Count != 0)
            {
                sizeBytes += "/polyjuice-merger?".Length + obj.AppName.Length + 1 + obj.GetHtmlPartialUrl().Length; // 1 for "=".

                // Serializing every sibling first.
                foreach (Json pp in obj.JsonSiblings)
                {
                    sizeBytes += 4 + pp.AppName.Length + pp.GetHtmlPartialUrl().Length; // 2 for "&" and "=" and 2 for quotation marks around string.
                    sizeBytes += pp.AppName.Length + 1; // 1 for ":".
                    sizeBytes += EstimateSizeBytes(pp) + 1; // 1 for ",".
                }

                sizeBytes += obj.AppName.Length + 4; // 2 for ":{" and 2 for quotation marks around string.
            }

            List<Template> exposedProperties;
            Json childObj;
            Template tProperty;

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

                    for (int arrPos = 0; arrPos < arr.Count; arrPos++) {
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

            sizeBytes += 1; // 1 for "}".

            // Checking if we have Json siblings on this level.
            if (obj.JsonSiblings.Count != 0) {

                sizeBytes += 1; // 1 for comma.

                sizeBytes += 4; // 4 for "Html".

                sizeBytes += 1; // 1 for ":".

                sizeBytes += 1; // 1 for "}".
            }
            });

            return sizeBytes;
        }

        /// <summary>
        /// Serializes given JSON object.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="buf"></param>
        /// <param name="origOffset"></param>
        /// <returns></returns>
        public override int Serialize(Json obj, byte[] buf, int origOffset) {
            int valueSize;
            List<Template> exposedProperties;
            TObject tObj;
            int offset = origOffset;
            String htmlUriMerged = null;

            obj.ExecuteInScope(() => {

                unsafe {
                    // Starting from the last written position
                    fixed (byte* p = &buf[offset]) {
                        byte* pfrag = p;

                        // Processing array first.
                        if (obj.IsArray) {

                            *pfrag++ = (byte)'[';
                            offset++;

                            for (int arrPos = 0; arrPos < obj.Count; arrPos++) {
                                valueSize = (obj._GetAt(arrPos) as Json).ToJsonUtf8(buf, offset);

                                pfrag += valueSize;
                                offset += valueSize;

                                if ((arrPos + 1) < obj.Count) {
                                    *pfrag++ = (byte)',';
                                    offset++;
                                }
                            }
                            *pfrag++ = (byte)']';
                            offset++;

                            return;
                        }

                        // If its not an array, its an object.
                        *pfrag++ = (byte)'{';
                        offset++;

                        tObj = (TObject)obj.Template;

                        // Checking if we have Json siblings on this level.
                        if (obj.JsonSiblings.Count != 0) {
                            htmlUriMerged = "/polyjuice-merger?" + obj.AppName + "=" + obj.GetHtmlPartialUrl();

                            // Serializing every sibling first.
                            foreach (Json pp in obj.JsonSiblings) {
                                htmlUriMerged += "&" + pp.AppName + "=" + pp.GetHtmlPartialUrl();

                                valueSize = JsonHelper.WriteString((IntPtr)pfrag, buf.Length - offset, pp.AppName);

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

                            // Adding current sibling app name.
                            valueSize = JsonHelper.WriteString((IntPtr)pfrag, buf.Length - offset, obj.AppName);

                            offset += valueSize;
                            pfrag += valueSize;

                            *pfrag++ = (byte)':';
                            offset++;

                            *pfrag++ = (byte)'{';
                            offset++;
                        }

                        exposedProperties = tObj.Properties.ExposedProperties;
                        for (int i = 0; i < exposedProperties.Count; i++) {
                            Template tProperty = exposedProperties[i];

                            // Property name.
                            valueSize = JsonHelper.WriteString((IntPtr)pfrag, buf.Length - offset, tProperty.TemplateName);

                            offset += valueSize;
                            pfrag += valueSize;

                            *pfrag++ = (byte)':';
                            offset++;

                            // Property value.
                            if (tProperty is TObject) {
                                Json childObj = ((TObject)tProperty).Getter(obj);
                                if (childObj != null) {
                                    valueSize = childObj.ToJsonUtf8(buf, offset);
                                } else {
                                    valueSize = 2;

                                    pfrag[0] = (byte)'{';
                                    pfrag[1] = (byte)'}';
                                }

                                pfrag += valueSize;
                                offset += valueSize;
                            } else if (tProperty is TObjArr) {
                                Json arr = ((TObjArr)tProperty).Getter(obj);

                                *pfrag++ = (byte)'[';
                                offset++;

                                for (int arrPos = 0; arrPos < arr.Count; arrPos++) {
                                    valueSize = (arr._GetAt(arrPos) as Json).ToJsonUtf8(buf, offset);

                                    pfrag += valueSize;
                                    offset += valueSize;

                                    if ((arrPos + 1) < arr.Count) {
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

                        *pfrag++ = (byte)'}';
                        offset++;

                        // Checking if we have Json siblings on this level.
                        if (obj.JsonSiblings.Count != 0) {

                            *pfrag++ = (byte)',';
                            offset++;

                            // Adding Html property to outer level.
                            valueSize = JsonHelper.WriteString((IntPtr)pfrag, buf.Length - offset, "Html");

                            offset += valueSize;
                            pfrag += valueSize;

                            *pfrag++ = (byte)':';
                            offset++;

                            valueSize = JsonHelper.WriteString((IntPtr)pfrag, buf.Length - offset, htmlUriMerged);

                            offset += valueSize;
                            pfrag += valueSize;

                            *pfrag++ = (byte)'}';
                            offset++;
                        }
                    }
                }
            });

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
                            childObj = arr.Add();
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