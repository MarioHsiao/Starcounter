
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
            return obj.Scope<Json, int>(_EstimateSizeBytes, obj);
        }

        public override int Serialize(Json obj, byte[] buf, int origOffset) {
            return obj.Scope<Json, byte[], int, int>(_Serialize, obj, buf, origOffset);
        }

        private bool WrapInAppName(Session session, Json obj) {
            if (!string.IsNullOrEmpty(obj._appName)
                && session != null
                && session.CheckOption(SessionOptions.IncludeNamespaces)
                && (session.PublicViewModel != obj)
                && !obj.calledFromStepSibling)
                return true;
            return false;
        }

        /// <summary>
        /// Estimates the size of serialization in bytes.
        /// </summary>
        private int _EstimateSizeBytes(Json obj) {

            int sizeBytes = 0;
            string htmlUriMerged = null;
            string partialConfigId = null;
            Session session = obj.Session;

            // Checking if application name should wrap the JSON.
            bool wrapInAppName = WrapInAppName(session, obj);
               
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

            if (wrapInAppName) {
                sizeBytes += obj._appName.Length + 4; // 2 for ":{" and 2 for quotation marks around string.

                // Checking if there is any partial Html provided.
                partialConfigId = obj.GetHtmlPartialUrl();
                if (!String.IsNullOrEmpty(partialConfigId)) {
                    htmlUriMerged = obj._appName + "=" + partialConfigId;
                    sizeBytes += 15 + partialConfigId.Length; // "PartialId":"configId",
                }
            }

            if (session != null && obj == session.PublicViewModel && session.CheckOption(SessionOptions.PatchVersioning)) {
                // add serverversion and clientversion to the serialized json if we have a root.
                sizeBytes += Starcounter.XSON.JsonPatch.ClientVersionPropertyName.Length + 35;
                sizeBytes += Starcounter.XSON.JsonPatch.ServerVersionPropertyName.Length + 35;
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

            // Wrapping in application name.
            if (wrapInAppName) {
                // Checking if we have any siblings. Since the array contains all stepsiblings (including this object)
                // we check if we have more than one stepsibling.
                if (!obj.calledFromStepSibling && obj.StepSiblings != null && obj.StepSiblings.Count != 1) {
                    // For comma.
                    sizeBytes++;

                    // Calculating the size for each step sibling.
                    foreach (Json pp in obj.StepSiblings) {
                        if (pp == obj)
                            continue;

                        pp.calledFromStepSibling = true;
                        try {
                            // Checking if there is any partial Html provided.
                            if (!String.IsNullOrEmpty(pp.GetHtmlPartialUrl())) {

                                if (htmlUriMerged != null)
                                    htmlUriMerged += "&";

                                htmlUriMerged += pp._appName + "=" + pp.GetHtmlPartialUrl();
                            }

                            sizeBytes += pp._appName.Length + 1; // 1 for ":".
                            sizeBytes += EstimateSizeBytes(pp) + 2; // 2 for ",".
                        } finally {
                            pp.calledFromStepSibling = false;
                        }
                    }
                }

                // ,"Html":"" is 10 characters
                sizeBytes += 10; 

                // Checking if merging Html URI was constructed.
                if (htmlUriMerged != null) {
                    htmlUriMerged = StarcounterConstants.PolyjuiceHtmlMergerPrefix + htmlUriMerged;
                    sizeBytes += htmlUriMerged.Length;

                    string setupStr = null;
                    try {
                        setupStr = StarcounterBase._DB.SQL<string>("SELECT p.Value FROM JuicyTilesSetup p WHERE p.Key = ?", htmlUriMerged).First;
                    } catch { }

                    if (setupStr != null) {
                        sizeBytes += setupStr.Length + 9; // "_setup":
                    }
                }
            }

            sizeBytes += 1; // 1 for "}".

            return sizeBytes;
        }

        /// <summary>
        /// Serializes given JSON object.
        /// </summary>
        private int _Serialize(Json obj, byte[] buf, int origOffset) {

            int valueSize;
            List<Template> exposedProperties;
            TObject tObj;
            int offset = origOffset;
            string htmlUriMerged = null;
            string partialConfigId = null;
            Session session = obj.Session;

            // Checking if application name should wrap the JSON.
            bool wrapInAppName = WrapInAppName(session, obj);

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

                        return offset - origOffset;
                    }

                    // If its not an array, its an object.
                    *pfrag++ = (byte)'{';
                    offset++;

                    if (wrapInAppName) {
                        // Checking if there is any partial Html provided.
                        partialConfigId = obj.GetHtmlPartialUrl();
                        if (!String.IsNullOrEmpty(partialConfigId)) {
                            htmlUriMerged = obj._appName + "=" + partialConfigId;
                        }

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

                    if (session != null && obj == session.PublicViewModel && session.CheckOption(SessionOptions.PatchVersioning)) {
                        // add serverversion and clientversion to the serialized json if we have a root.
                        valueSize = JsonHelper.WriteStringAsIs((IntPtr)pfrag, buf.Length - offset, Starcounter.XSON.JsonPatch.ClientVersionPropertyName);
                        offset += valueSize;
                        pfrag += valueSize;
                        *pfrag++ = (byte)':';
                        offset++;
                        valueSize = JsonHelper.WriteInt((IntPtr)pfrag, buf.Length - offset, session.ClientVersion);
                        offset += valueSize;
                        pfrag += valueSize;
                        *pfrag++ = (byte)',';
                        offset++;

                        valueSize = JsonHelper.WriteStringAsIs((IntPtr)pfrag, buf.Length - offset, Starcounter.XSON.JsonPatch.ServerVersionPropertyName);
                        offset += valueSize;
                        pfrag += valueSize;
                        *pfrag++ = (byte)':';
                        offset++;
                        valueSize = JsonHelper.WriteInt((IntPtr)pfrag, buf.Length - offset, session.ServerVersion);
                        offset += valueSize;
                        pfrag += valueSize;

                        if (exposedProperties.Count > 0) {
                            *pfrag++ = (byte)',';
                            offset++;
                        }
                    }

                    for (int i = 0; i < exposedProperties.Count; i++) {
                        Template tProperty = exposedProperties[i];
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

                    // Wrapping in application name.
                    if (wrapInAppName) {
                        *pfrag++ = (byte)'}';
                        offset++;

                        // Checking if we have any siblings. Since the array contains all stepsiblings (including this object)
                        // we check if we have more than one stepsibling.
                        if (!obj.calledFromStepSibling && obj.StepSiblings != null && obj.StepSiblings.Count != 1) {
                            bool addComma = true;

                            // Serializing every sibling first.
                            for (int s = 0; s < obj.StepSiblings.Count; s++) {
                                var pp = obj.StepSiblings[s];

                                if (pp == obj)
                                    continue;

                                if (addComma) {
                                    addComma = false;
                                    *pfrag++ = (byte)',';
                                    offset++;
                                }

                                pp.calledFromStepSibling = true;
                                try {
                                    // Checking if there is any partial Html provided.
                                    if (!String.IsNullOrEmpty(pp.GetHtmlPartialUrl())) {

                                        if (htmlUriMerged != null)
                                            htmlUriMerged += "&";

                                        htmlUriMerged += pp._appName + "=" + pp.GetHtmlPartialUrl();
                                    }

                                    valueSize = JsonHelper.WriteStringAsIs((IntPtr)pfrag, buf.Length - offset, pp._appName);
                                    offset += valueSize;
                                    pfrag += valueSize;

                                    *pfrag++ = (byte)':';
                                    offset++;

                                    valueSize = pp.ToJsonUtf8(buf, offset);
                                    pfrag += valueSize;
                                    offset += valueSize;

                                    if ((s + 1) < obj.StepSiblings.Count) {
                                        addComma = true;
                                    }
                                } finally {
                                    pp.calledFromStepSibling = false;
                                }
                            }
                        }

                        // Adding Html property.
                        *pfrag++ = (byte)',';
                        offset++;

                        // Adding Html property to outer level.
                        valueSize = JsonHelper.WriteStringAsIs((IntPtr)pfrag, buf.Length - offset, "Html");
                        offset += valueSize;
                        pfrag += valueSize;

                        *pfrag++ = (byte)':';
                        offset++;

                        // Checking if merging Html URI was constructed.
                        if (null != htmlUriMerged) {
                            htmlUriMerged = StarcounterConstants.PolyjuiceHtmlMergerPrefix + htmlUriMerged;

                            valueSize = JsonHelper.WriteString((IntPtr)pfrag, buf.Length - offset, htmlUriMerged);
                            offset += valueSize;
                            pfrag += valueSize;

                            string setupStr = null;
                            if (!string.IsNullOrEmpty(partialConfigId)) {
                                try {
                                    setupStr = StarcounterBase._DB.SQL<string>("SELECT p.Value FROM JuicyTilesSetup p WHERE p.Key = ?", partialConfigId).First;
                                } catch { }
                            }

                            if (setupStr != null) {
                                *pfrag++ = (byte)',';
                                offset++;

                                valueSize = JsonHelper.WriteString((IntPtr)pfrag, buf.Length - offset, "PartialId");
                                offset += valueSize;
                                pfrag += valueSize;

                                *pfrag++ = (byte)':';
                                offset++;

                                valueSize = JsonHelper.WriteString((IntPtr)pfrag, buf.Length - offset, partialConfigId);
                                offset += valueSize;
                                pfrag += valueSize;

                                *pfrag++ = (byte)',';
                                offset++;

                                valueSize = JsonHelper.WriteString((IntPtr)pfrag, buf.Length - offset, "_setup");
                                offset += valueSize;
                                pfrag += valueSize;

                                *pfrag++ = (byte)':';
                                offset++;

                                valueSize = JsonHelper.WriteStringNoQuotations(pfrag, buf.Length - offset, setupStr);
                                offset += valueSize;
                                pfrag += valueSize;
                            }

                        } else {

                            // Inserting an empty string.

                            *pfrag++ = (byte)'\"';
                            offset++;
                            *pfrag++ = (byte)'\"';
                            offset++;
                        }
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