using Starcounter.Internal;
using Starcounter.Templates;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Starcounter.Advanced.XSON {
    public abstract class StandardJsonSerializerBase : TypedJsonSerializer {
        private delegate int EstimateSizeDelegate(TypedJsonSerializer serializer, Json json, Template template);
        private delegate int SerializeDelegate(TypedJsonSerializer serializer, Json json, Template template, IntPtr dest, int destSize);

        private static EstimateSizeDelegate[] estimatePerTemplate;
        private static SerializeDelegate[] serializePerTemplate;

        static StandardJsonSerializerBase() {
            estimatePerTemplate = new EstimateSizeDelegate[9];
            estimatePerTemplate[(int)TemplateTypeEnum.Unknown] = EstimateException;
            estimatePerTemplate[(int)TemplateTypeEnum.Bool] = EstimateBool;
            estimatePerTemplate[(int)TemplateTypeEnum.Decimal] = EstimateDecimal;
            estimatePerTemplate[(int)TemplateTypeEnum.Double] = EstimateDouble;
            estimatePerTemplate[(int)TemplateTypeEnum.Long] = EstimateLong;
            estimatePerTemplate[(int)TemplateTypeEnum.String] = EstimateString;
            estimatePerTemplate[(int)TemplateTypeEnum.Object] = ScopeAndEstimateObject;
            estimatePerTemplate[(int)TemplateTypeEnum.Array] = ScopeAndEstimateArray;
            estimatePerTemplate[(int)TemplateTypeEnum.Trigger] = EstimateTrigger;

            serializePerTemplate = new SerializeDelegate[9];
            serializePerTemplate[(int)TemplateTypeEnum.Unknown] = SerializeException;
            serializePerTemplate[(int)TemplateTypeEnum.Bool] = SerializeBool;
            serializePerTemplate[(int)TemplateTypeEnum.Decimal] = SerializeDecimal;
            serializePerTemplate[(int)TemplateTypeEnum.Double] = SerializeDouble;
            serializePerTemplate[(int)TemplateTypeEnum.Long] = SerializeLong;
            serializePerTemplate[(int)TemplateTypeEnum.String] = SerializeString;
            serializePerTemplate[(int)TemplateTypeEnum.Object] = ScopeAndSerializeObject;
            serializePerTemplate[(int)TemplateTypeEnum.Array] = ScopeAndSerializeArray;
            serializePerTemplate[(int)TemplateTypeEnum.Trigger] = SerializeTrigger;
        }

        public override int EstimateSizeBytes(Json json) {
            return json.Scope<TypedJsonSerializer, Json, int>((TypedJsonSerializer tjs, Json j) => {
                if (j.Template != null) {
                    return estimatePerTemplate[(int)j.Template.TemplateTypeId](tjs, j, j.Template);
                } else {
                    // No template defined. Assuming object.
                    return EstimateObject(this, j);
                }
            },
            this,
            json);
        }

        public override int EstimateSizeBytes(Json json, Template property) {
            return json.Scope<TypedJsonSerializer, Json, Template, int>((TypedJsonSerializer tjs, Json j, Template t) => {
                return estimatePerTemplate[(int)t.TemplateTypeId](tjs, j, t);
            },
            this,
            json,
            property);
        }

        public override int Serialize(Json json, IntPtr dest, int destSize) {
            bool oldValue = json.checkBoundProperties;
            try {
                json.checkBoundProperties = false;
                int realSize = json.Scope<TypedJsonSerializer, Json, IntPtr, int, int>((TypedJsonSerializer tjs, Json j, IntPtr d, int ds) => {
                    if (j.Template != null) {
                        return serializePerTemplate[(int)j.Template.TemplateTypeId](tjs, j, j.Template, d, ds);
                    } else {
                        // No template defined. Assuming object.
                        return SerializeObject(this, j, d, ds);
                    }
                },
                this,
                json,
                dest,
                destSize);

                AssertWrittenSize(json, realSize, destSize);
                return realSize;
            } finally {
                json.checkBoundProperties = oldValue;
            }
        }

        public override int Serialize(Json json, Template property, IntPtr dest, int destSize) {
            bool oldValue = json.checkBoundProperties;
            try {
                json.checkBoundProperties = false;
                int realSize = json.Scope<TypedJsonSerializer, Json, Template, IntPtr, int, int>((TypedJsonSerializer tjs, Json j, Template t, IntPtr d, int ds) => {
                    return serializePerTemplate[(int)t.TemplateTypeId](tjs, j, t, d, ds);
                },
                 this,
                 json,
                 property,
                 dest,
                 destSize);

                AssertWrittenSize(json, realSize, destSize);
                return realSize;
            } finally {
                json.checkBoundProperties = oldValue;
            }
        }
        
        private static void AssertWrittenSize(Json json, int realSize, int destSize) {
            if (realSize > destSize) {
                var errMsg = "TypedJson serializer: written size is larger than size of destination!";
                errMsg += " (written: " + realSize + ", destination: " + destSize + ")\r\n";
                errMsg += "Type: " + json.GetType() + ", DebugString: " + json.DebugString;
                throw new Exception(errMsg);
            }
        }

        private static int EstimateException(TypedJsonSerializer serializer, Json json, Template template) {
            throw new Exception("Cannot estimate size of Json. The type of template is unknown: " + template.GetType());
        }

        private static int EstimateBool(TypedJsonSerializer serializer, Json json, Template template) {
            ((TBool)template).SetCachedReads(json);
            return 5;
        }

        private static int EstimateDecimal(TypedJsonSerializer serializer, Json json, Template template) {
            ((TDecimal)template).SetCachedReads(json);
            return 32;
        }

        private static int EstimateDouble(TypedJsonSerializer serializer, Json json, Template template) {
            ((TDouble)template).SetCachedReads(json);
            return 32;
        }

        private static int EstimateLong(TypedJsonSerializer serializer, Json json, Template template) {
            ((TLong)template).SetCachedReads(json);
            return 32;
        }

        private static int EstimateString(TypedJsonSerializer serializer, Json json, Template template) {
            TString strTemplate = (TString)template;
            strTemplate.SetCachedReads(json);
            string value = strTemplate.Getter(json);

            if (value != null)
                return value.Length * 2 + 2;
            return 2;
        }

        private static int ScopeAndEstimateObject(TypedJsonSerializer serializer, Json json, Template template) {
            Json parent = json;
            if (template != json.Template) {
                // Template points to a property in the specified jsonobject. Get correct value from template getter.
                json = ((TObject)template).Getter(parent);
            }

            if (json != null) {
                return json.Scope<TypedJsonSerializer, Json, int>(EstimateObject, serializer, json);
            } else {
                return 2;
            }
        }

        private static int EstimateObject(TypedJsonSerializer serializer, Json json) {
            Session session = json.Session;
            int sizeBytes = 0;
            string htmlUriMerged = null;

            if (json.Template == null) {
                sizeBytes = 2; // 2 for "{}".
                return sizeBytes;
            }
            
            // Checking if application name should wrap the JSON.
            bool wrapInAppName = ShouldBeNamespaced(json, session);

            sizeBytes += 1; // 1 for "{".
            List<Template> exposedProperties;
            Template tProperty;

            if (session != null && json == session.PublicViewModel) {
                var patchVersion = (json.ChangeLog != null) ? json.ChangeLog.Version : null;
                if (patchVersion != null) {
                    // add serverversion and clientversion to the serialized json if we have a root.
                    sizeBytes += patchVersion.RemoteVersionPropertyName.Length + 35;
                    sizeBytes += patchVersion.LocalVersionPropertyName.Length + 35;
                }
            }

            exposedProperties = ((TObject)json.Template).Properties.ExposedProperties;
            for (int i = 0; i < exposedProperties.Count; i++) {
                tProperty = exposedProperties[i];

                sizeBytes += tProperty.TemplateName.Length + 3; // 1 for ":" and to for quotation marks around string.
                sizeBytes += estimatePerTemplate[(int)tProperty.TemplateTypeId](serializer, json, tProperty);
                sizeBytes += 1; // 1 for comma.
            }

            if (wrapInAppName) {
                sizeBytes += json.appName.Length + 4; // 2 for ":{" and 2 for quotation marks around string.
                
                // Checking if there is any partial Html provided.
                if (!String.IsNullOrEmpty(json.GetHtmlPartialUrl())) {
                    htmlUriMerged = json.appName + "=" + json.GetHtmlPartialUrl();
                }
                
                // Checking if we have any siblings. Since the array contains all stepsiblings (including this object)
                // we check if we have more than one stepsibling.
                if (!json.calledFromStepSibling && json.Siblings != null && json.Siblings.Count != 1) {
                    // For comma.
                    sizeBytes++;

                    // Calculating the size for each step sibling.
                    foreach (Json pp in json.Siblings) {
                        if (pp == json)
                            continue;

                        pp.calledFromStepSibling = true;
                        try {
                            // Checking if there is any partial Html provided.
                            if (!String.IsNullOrEmpty(pp.GetHtmlPartialUrl())) {
                                if (htmlUriMerged != null)
                                    htmlUriMerged += "&";

                                htmlUriMerged += pp.appName + "=" + pp.GetHtmlPartialUrl();
                            }

                            sizeBytes += pp.appName.Length + 1; // 1 for ":".
                            sizeBytes += serializer.EstimateSizeBytes(pp) + 2; // 2 for ",".
                        } finally {
                            pp.calledFromStepSibling = false;
                        }
                    }
                }

                // ,"Html":"" is 10 characters
                sizeBytes += 10;

                // Checking if merging Html URI was constructed.
                if (htmlUriMerged != null) {
                    htmlUriMerged = StarcounterConstants.HtmlMergerPrefix + htmlUriMerged;
                    sizeBytes += htmlUriMerged.Length;
                }
            }

            sizeBytes += 1; // 1 for "}".
            return sizeBytes;
        }

        private static int ScopeAndEstimateArray(TypedJsonSerializer serializer, Json json, Template template) {
            Json parent = json;

            if (template != parent.Template) {
                // Not a root. Get correct value from getter.
                json = ((TObjArr)template).Getter(parent);
            }

            if (json != null) {
                return json.Scope<TypedJsonSerializer, Json, int>(EstimateArray, serializer, json);
            } else {
                return 2;
            }
        }

        private static int EstimateArray(TypedJsonSerializer serializer, Json json) {
            int sizeBytes = 2; // 2 for "[]".
            IList arrList = (IList)json;

            for (int i = 0; i < arrList.Count; i++) {
                var rowJson = (Json)arrList[i];
                sizeBytes += serializer.EstimateSizeBytes(rowJson) + 1;
            }
            return sizeBytes;
        }

        private static int EstimateTrigger(TypedJsonSerializer serializer, Json json, Template template) {
            return 4;
        }

        private static int SerializeException(TypedJsonSerializer serializer, Json json, Template template, IntPtr dest, int destSize) {
            throw new Exception("Cannot serialize Json. The type of template is unknown: " + template.GetType());
        }

        private static int SerializeBool(TypedJsonSerializer serializer, Json json, Template template, IntPtr dest, int destSize) {
            if (template == null)
                template = json.Template;

            bool value = ((TBool)template).Getter(json);
            return JsonHelper.WriteBool(dest, destSize, value);
        }

        private static int SerializeDecimal(TypedJsonSerializer serializer, Json json, Template template, IntPtr dest, int destSize) {
            if (template == null)
                template = json.Template;

            decimal value = ((TDecimal)template).Getter(json);
            return JsonHelper.WriteDecimal(dest, destSize, value);            
        }

        private static int SerializeDouble(TypedJsonSerializer serializer, Json json, Template template, IntPtr dest, int destSize) {
            if (template == null)
                template = json.Template;

            double value = ((TDouble)template).Getter(json);
            return JsonHelper.WriteDouble(dest, destSize, value);
        }

        private static int SerializeLong(TypedJsonSerializer serializer, Json json, Template template, IntPtr dest, int destSize) {
            if (template == null)
                template = json.Template;

            long value = ((TLong)template).Getter(json);
            return JsonHelper.WriteInt(dest, destSize, value);
        }

        private static int SerializeString(TypedJsonSerializer serializer, Json json, Template template, IntPtr dest, int destSize) {
            if (template == null)    
                template = json.Template;
            
            string value = ((TString)template).Getter(json);
            return JsonHelper.WriteString(dest, destSize, value);
        }

        private static int ScopeAndSerializeObject(TypedJsonSerializer serializer, Json json, Template template, IntPtr dest, int destSize) {
            Json parent = json;

            if (template != parent.Template)
                json = ((TObject)template).Getter(json);

            if (json != null) {
                return json.Scope<TypedJsonSerializer, Json, IntPtr, int, int>(SerializeObject, serializer, json, dest, destSize);
            } else {
                unsafe {
                    byte* pdest = (byte*)dest;
                    *pdest++ = (byte)'{';
                    *pdest++ = (byte)'}';
                }
                return 2;
            }
        }

        private static int SerializeObject(TypedJsonSerializer serializer, Json json, IntPtr dest, int destSize) {
            int valueSize;
            int used = 0;
            List<Template> exposedProperties;
            string htmlUriMerged = null;
            Session session = json.Session;

            // Checking if application name should wrap the JSON.
            bool wrapInAppName = ShouldBeNamespaced(json, session);

            unsafe {
                byte* pfrag = (byte*)dest;

                *pfrag++ = (byte)'{';
                used++;

                if (json.Template == null) {
                    *pfrag++ = (byte)'}';
                    used++;
                    return used;
                }

                if (wrapInAppName) {   
                    // Checking if there is any partial Html provided.
                    if (!String.IsNullOrEmpty(json.GetHtmlPartialUrl())) {
                        htmlUriMerged = json.appName + "=" + json.GetHtmlPartialUrl();
                    }

                    valueSize = JsonHelper.WriteStringAsIs((IntPtr)pfrag, destSize - used, json.appName);
                    pfrag += valueSize;
                    used += valueSize;

                    *pfrag++ = (byte)':';
                    used++;

                    *pfrag++ = (byte)'{';
                    used++;
                }

                exposedProperties = ((TObject)json.Template).Properties.ExposedProperties;

                if (session != null && json == session.PublicViewModel) {
                    var patchVersion = (json.ChangeLog != null) ? json.ChangeLog.Version : null;
                    if (patchVersion != null) {
                        // add serverversion and clientversion to the serialized json if we have a root.
                        valueSize = JsonHelper.WriteStringAsIs((IntPtr)pfrag, destSize - used, patchVersion.RemoteVersionPropertyName);
                        used += valueSize;
                        pfrag += valueSize;
                        *pfrag++ = (byte)':';
                        used++;
                        valueSize = JsonHelper.WriteInt((IntPtr)pfrag, destSize - used, patchVersion.RemoteVersion);
                        used += valueSize;
                        pfrag += valueSize;
                        *pfrag++ = (byte)',';
                        used++;

                        valueSize = JsonHelper.WriteStringAsIs((IntPtr)pfrag, destSize - used, patchVersion.LocalVersionPropertyName);
                        used += valueSize;
                        pfrag += valueSize;
                        *pfrag++ = (byte)':';
                        used++;
                        valueSize = JsonHelper.WriteInt((IntPtr)pfrag, destSize - used, patchVersion.LocalVersion);
                        used += valueSize;
                        pfrag += valueSize;

                        if (exposedProperties.Count > 0) {
                            *pfrag++ = (byte)',';
                            used++;
                        }
                    }
                }

                for (int i = 0; i < exposedProperties.Count; i++) {
                    Template tProperty = exposedProperties[i];

                    valueSize = JsonHelper.WriteStringAsIs((IntPtr)pfrag, destSize - used, tProperty.TemplateName);
                    used += valueSize;
                    pfrag += valueSize;

                    *pfrag++ = (byte)':';
                    used++;

                    // TODO:
                    // If we have an object with another serializer set, this code wont work since it will bypass the setting.
                    valueSize = serializePerTemplate[(int)tProperty.TemplateTypeId](serializer, json, tProperty, (IntPtr)pfrag, destSize - used);
                    used += valueSize;
                    pfrag += valueSize;

                    if ((i + 1) < exposedProperties.Count) {
                        *pfrag++ = (byte)',';
                        used++;
                    }
                }
                
                if (wrapInAppName) {
                    *pfrag++ = (byte)'}';
                    used++;

                    // Checking if we have any siblings. Since the array contains all stepsiblings (including this object)
                    // we check if we have more than one stepsibling.
                    if (!json.calledFromStepSibling && json.Siblings != null && json.Siblings.Count != 1) {
                        bool addComma = true;

                        // Serializing every sibling first.
                        for (int s = 0; s < json.Siblings.Count; s++) {
                            var pp = json.Siblings[s];

                            if (pp == json)
                                continue;

                            if (addComma) {
                                addComma = false;
                                *pfrag++ = (byte)',';
                                used++;
                            }

                            pp.calledFromStepSibling = true;
                            try {
                                // Checking if there is any partial Html provided.
                                if (!String.IsNullOrEmpty(pp.GetHtmlPartialUrl())) {
                                    if (htmlUriMerged != null)
                                        htmlUriMerged += "&";

                                    htmlUriMerged += pp.appName + "=" + pp.GetHtmlPartialUrl();
                                }

                                valueSize = JsonHelper.WriteStringAsIs((IntPtr)pfrag, destSize - used, pp.appName);
                                used += valueSize;
                                pfrag += valueSize;

                                *pfrag++ = (byte)':';
                                used++;

                                valueSize = pp.ToJsonUtf8((IntPtr)pfrag, destSize - used);
                                pfrag += valueSize;
                                used += valueSize;

                                if ((s + 1) < json.Siblings.Count) {
                                    addComma = true;
                                }
                            } finally {
                                pp.calledFromStepSibling = false;
                            }
                        }
                    }

                    // Adding Html property.
                    *pfrag++ = (byte)',';
                    used++;

                    // Adding Html property to outer level.
                    valueSize = JsonHelper.WriteStringAsIs((IntPtr)pfrag, destSize - used, "Html");
                    used += valueSize;
                    pfrag += valueSize;

                    *pfrag++ = (byte)':';
                    used++;

                    // Checking if merging Html URI was constructed.
                    if (null != htmlUriMerged) {
                        htmlUriMerged = StarcounterConstants.HtmlMergerPrefix + htmlUriMerged;

                        valueSize = JsonHelper.WriteString((IntPtr)pfrag, destSize - used, htmlUriMerged);
                        used += valueSize;
                        pfrag += valueSize;
                    } else {
                        // Inserting an empty string.
                        *pfrag++ = (byte)'\"';
                        used++;
                        *pfrag++ = (byte)'\"';
                        used++;
                    }
                }

                *pfrag++ = (byte)'}';
                used++;
            }

            return used;
        }

        private static int ScopeAndSerializeArray(TypedJsonSerializer serializer, Json json, Template template, IntPtr dest, int destSize) {
            Json parent = json;

            if (template != parent.Template)
                json = ((TObjArr)template).Getter(json);

            if (json != null) {
                return json.Scope<TypedJsonSerializer, Json, IntPtr, int, int>(SerializeArray, serializer, json, dest, destSize);
            } else {
                unsafe {
                    byte* pdest = (byte*)dest;
                    *pdest++ = (byte)'[';
                    *pdest++ = (byte)']';
                    return 2;
                }
            }
        }

        private static int SerializeArray(TypedJsonSerializer serializer, Json json, IntPtr dest, int destSize) {
            int used = 0;
            int valueSize;
            IList arrList;
            Json arrItem;
            
            unsafe {
                byte* pfrag = (byte*)dest;

                *pfrag++ = (byte)'[';
                used++;


                arrList = (IList)json;
                for (int i = 0; i < arrList.Count; i++) {
                    arrItem = (Json)arrList[i];

                    if (arrItem != null) {
                        valueSize = arrItem.ToJsonUtf8((IntPtr)pfrag, destSize - used);
                    } else {
                        // TODO:
                        // Handle nullvalues.
                        *pfrag++ = (byte)'{';
                        *pfrag++ = (byte)'}';
                        valueSize = 2;
                    }

                    pfrag += valueSize;
                    used += valueSize;

                    if ((i + 1) < arrList.Count) {
                        *pfrag++ = (byte)',';
                        used++;
                    }
                }
                *pfrag++ = (byte)']';
                used++;

                return used;
            }
        }

        private static int SerializeTrigger(TypedJsonSerializer serializer, Json json, Template template, IntPtr dest, int destSize) {
            return JsonHelper.WriteNull(dest, destSize);
        }

        /// <summary>
        /// Returns true if the specified json should be wrapped in namespace when
        /// serializing. 
        /// Only certain kinds of json will be namespaced.
        /// 1. Is stateful (i.e state stored on a session)
        /// 2. Session have the namespace option enabled.
        /// 3. Is a possible attachpoint for siblings of other viewmodels (partial/merged).
        /// </summary>
        /// <param name="json"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        private static bool ShouldBeNamespaced(Json json, Session session) {
            if (json.wrapInAppName
                && session != null
                && session.enableNamespaces
                && session.CheckOption(SessionOptions.IncludeNamespaces)
                && (session.PublicViewModel != json)
                && !json.calledFromStepSibling)
                return true;
            return false;
        }
    }

    public class StandardJsonSerializer : StandardJsonSerializerBase {
        private delegate void PopulateDelegate(Json json, Template template, JsonReader reader);

        private static PopulateDelegate[] populatePerTemplate;

        static StandardJsonSerializer() {
            populatePerTemplate = new PopulateDelegate[9];
            populatePerTemplate[(int)TemplateTypeEnum.Unknown] = PopulateException;
            populatePerTemplate[(int)TemplateTypeEnum.Bool] = PopulateBool;
            populatePerTemplate[(int)TemplateTypeEnum.Decimal] = PopulateDecimal;
            populatePerTemplate[(int)TemplateTypeEnum.Double] = PopulateDouble;
            populatePerTemplate[(int)TemplateTypeEnum.Long] = PopulateLong;
            populatePerTemplate[(int)TemplateTypeEnum.String] = PopulateString;
            populatePerTemplate[(int)TemplateTypeEnum.Object] = PopulateObject;
            populatePerTemplate[(int)TemplateTypeEnum.Array] = PopulateArray;
            populatePerTemplate[(int)TemplateTypeEnum.Trigger] = PopulateTrigger;
        }

        private static void PopulateException(Json json, Template template, JsonReader reader) {
            throw new Exception("Cannot populate Json. Unknown template: " + template.GetType());
        }

        private static void PopulateBool(Json json, Template template, JsonReader reader) {
            if (template == null)
                template = json.Template;

            try {
                ((TBool)template).Setter(json, reader.ReadBool());
            } catch (InvalidCastException ex) {
                JsonHelper.ThrowWrongValueTypeException(ex, template.TemplateName, template.JsonType, reader.ReadString());
            }
        }

        private static void PopulateDecimal(Json json, Template template, JsonReader reader) {
            if (template == null)
                template = json.Template;

            try {
            ((TDecimal)template).Setter(json, reader.ReadDecimal());
            } catch (InvalidCastException ex) {
                JsonHelper.ThrowWrongValueTypeException(ex, template.TemplateName, template.JsonType, reader.ReadString());
            }
        }

        private static void PopulateDouble(Json json, Template template, JsonReader reader) {
            if (template == null)
                template = json.Template;

            try {
                ((TDouble)template).Setter(json, reader.ReadDouble());
            } catch (InvalidCastException ex) {
                JsonHelper.ThrowWrongValueTypeException(ex, template.TemplateName, template.JsonType, reader.ReadString());
            }
        }

        private static void PopulateLong(Json json, Template template, JsonReader reader) {
            if (template == null)
                template = json.Template;

            try {
                ((TLong)template).Setter(json, reader.ReadLong());
            } catch (InvalidCastException ex) {
                JsonHelper.ThrowWrongValueTypeException(ex, template.TemplateName, template.JsonType, reader.ReadString());
            }
        }

        private static void PopulateString(Json json, Template template, JsonReader reader) {
            if (template == null)
                template = json.Template;

            try {
                ((TString)template).Setter(json, reader.ReadString());
            } catch (InvalidCastException ex) {
                JsonHelper.ThrowWrongValueTypeException(ex, template.TemplateName, template.JsonType, reader.ReadString());
            }
        }

        private static void PopulateObject(Json json, Template template, JsonReader reader) {
            string propertyName;
            Template tProperty;
            TObject tObject;
            JsonToken token;

            if (template != null) {
                tObject = (TObject)template;
                json = tObject.Getter(json);
            } else {
                tObject = (TObject)json.Template;
            }

            token = reader.ReadNext();
            if (!(token == JsonToken.StartObject))
                JsonHelper.ThrowInvalidJsonException("Expected object but found: " + token.ToString());

            reader.Skip(1);

            while (true) {
                token = reader.ReadNext();

                if (token == JsonToken.EndObject) {
                    reader.Skip(1);
                    break;
                }

                if (token == JsonToken.End)
                    JsonHelper.ThrowInvalidJsonException("No end object token found");

                if (!(token == JsonToken.PropertyName))
                    JsonHelper.ThrowInvalidJsonException("Expected name of property but found token: " + token.ToString());

                propertyName = reader.ReadString();
                tProperty = tObject.Properties.GetExposedTemplateByName(propertyName);
                if (tProperty == null) {
                    JsonHelper.ThrowPropertyNotFoundException(propertyName);
                }

                token = reader.ReadNext();
                if (tProperty.TemplateTypeId == TemplateTypeEnum.Object) {
                    var childObj = ((TObject)tProperty).Getter(json);
                    var valueSize = childObj.PopulateFromJson(reader.CurrentPtr, reader.Size - reader.Position);
                    reader.Skip(valueSize);
                } else {
                    populatePerTemplate[(int)tProperty.TemplateTypeId](json, tProperty, reader);
                }
            }
        }

        private static void PopulateArray(Json json, Template template, JsonReader reader) {
            int valueSize;
            Json childObj;
            JsonToken token;

            if (template != null) {
                json = ((TObjArr)template).Getter(json);
            }

            token = reader.ReadNext();

            if (!(token == JsonToken.StartArray))
                JsonHelper.ThrowInvalidJsonException("Expected array but found: " + token.ToString());

            reader.Skip(1);

            while (true) {
                token = reader.ReadNext();
                if (token == JsonToken.EndArray) {
                    reader.Skip(1);
                    break;
                }

                if (token == JsonToken.End)
                    JsonHelper.ThrowInvalidJsonException("No end array token found");

                childObj = json.NewItem();
                valueSize = childObj.PopulateFromJson(reader.CurrentPtr, reader.Size - reader.Position);
                reader.Skip(valueSize);
            }
            
            //while (arrayReader.GotoNextObject()) {
            //    childObj = json.NewItem();
            //    arrayReader.PopulateObject(childObj);
            //}
            //reader.Skip(arrayReader.Used + 1);
        }

        private static void PopulateTrigger(Json json, Template template, JsonReader reader) {
            // Should not get here. Not sure how triggers should be handled.
            throw new NotImplementedException();
        }

        public override int Populate(Json json, IntPtr source, int sourceSize) {
            var reader = new JsonReader(source, sourceSize);
            populatePerTemplate[(int)json.Template.TemplateTypeId](json, null, reader);
            return reader.Position;
        }
    }
}