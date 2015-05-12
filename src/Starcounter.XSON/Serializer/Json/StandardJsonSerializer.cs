
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
            estimatePerTemplate[(int)TemplateTypeEnum.Object] = EstimateObject;
            estimatePerTemplate[(int)TemplateTypeEnum.Array] = EstimateArray;
            estimatePerTemplate[(int)TemplateTypeEnum.Trigger] = EstimateTrigger;

            serializePerTemplate = new SerializeDelegate[9];
            serializePerTemplate[(int)TemplateTypeEnum.Unknown] = SerializeException;
            serializePerTemplate[(int)TemplateTypeEnum.Bool] = SerializeBool;
            serializePerTemplate[(int)TemplateTypeEnum.Decimal] = SerializeDecimal;
            serializePerTemplate[(int)TemplateTypeEnum.Double] = SerializeDouble;
            serializePerTemplate[(int)TemplateTypeEnum.Long] = SerializeLong;
            serializePerTemplate[(int)TemplateTypeEnum.String] = SerializeString;
            serializePerTemplate[(int)TemplateTypeEnum.Object] = SerializeObject;
            serializePerTemplate[(int)TemplateTypeEnum.Array] = SerializeArray;
            serializePerTemplate[(int)TemplateTypeEnum.Trigger] = SerializeTrigger;
        }

        public override int EstimateSizeBytes(Json json) {
            return json.Scope<TypedJsonSerializer, Json, int>((TypedJsonSerializer tjs, Json j) => {
                return estimatePerTemplate[(int)j.Template.TemplateTypeId](tjs, j, null);
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
            return json.Scope<TypedJsonSerializer, Json, IntPtr, int, int>((TypedJsonSerializer tjs, Json j, IntPtr d, int ds) => {
                return serializePerTemplate[(int)j.Template.TemplateTypeId](tjs, j, null, d, ds);
            },
            this,
            json, 
            dest, 
            destSize);
        }

        public override int Serialize(Json json, Template property, IntPtr dest, int destSize) {
            return json.Scope<TypedJsonSerializer, Json, Template, IntPtr, int, int>((TypedJsonSerializer tjs, Json j, Template t, IntPtr d, int ds) => {
                return serializePerTemplate[(int)t.TemplateTypeId](tjs, j, t, d, ds);
            },
            this,
            json,
            property,
            dest,
            destSize);
        }

        private static bool WrapInAppName(Session session, Json obj) {
            if (!string.IsNullOrEmpty(obj._appName)
                && session != null
                && session.CheckOption(SessionOptions.IncludeNamespaces)
                && (session.PublicViewModel != obj)
                && !obj.calledFromStepSibling)
                return true;
            return false;
        }

        private static int EstimateException(TypedJsonSerializer serializer, Json json, Template template) {
            throw new Exception("Cannot estimate size of Json. The type of template is unknown: " + template.GetType());
        }

        private static int EstimateBool(TypedJsonSerializer serializer, Json json, Template template) {
            return 5;
        }

        private static int EstimateDecimal(TypedJsonSerializer serializer, Json json, Template template) {
            return 32;
        }

        private static int EstimateDouble(TypedJsonSerializer serializer, Json json, Template template) {
            return 32;
        }

        private static int EstimateLong(TypedJsonSerializer serializer, Json json, Template template) {
            return 32;
        }

        private static int EstimateString(TypedJsonSerializer serializer, Json json, Template template) {
            if (template == null) {
                // This is a root. Take template from json.
                template = json.Template;
            }

            string value = ((TString)template).Getter(json);

            if (value != null)
                return value.Length * 2 + 2;
            return 2;
        }

        private static int EstimateObject(TypedJsonSerializer serializer, Json json, Template template) {
            Session session = json.Session;
            int sizeBytes = 0;
            string partialConfigId = null;
            string activeAppName;
            string htmlUriMerged = null;

            if (template != null) {
                // Not a root. Get correct value from template getter.
                json = ((TObject)template).Getter(json);
            }

            if (json.Template == null) {
                sizeBytes += 1; // 1 for "}".
                return sizeBytes;
            }

            // Checking if application name should wrap the JSON.
            bool wrapInAppName = WrapInAppName(session, json);

            sizeBytes += 1; // 1 for "{".
            List<Template> exposedProperties;
            Template tProperty;

            if (session != null && json == session.PublicViewModel && session.CheckOption(SessionOptions.PatchVersioning)) {
                // add serverversion and clientversion to the serialized json if we have a root.
                sizeBytes += Starcounter.XSON.JsonPatch.ClientVersionPropertyName.Length + 35;
                sizeBytes += Starcounter.XSON.JsonPatch.ServerVersionPropertyName.Length + 35;
            }

            exposedProperties = ((TObject)json.Template).Properties.ExposedProperties;
            for (int i = 0; i < exposedProperties.Count; i++) {
                tProperty = exposedProperties[i];

                sizeBytes += tProperty.TemplateName.Length + 3; // 1 for ":" and to for quotation marks around string.
                sizeBytes += estimatePerTemplate[(int)tProperty.TemplateTypeId](serializer, json, tProperty);
                sizeBytes += 1; // 1 for comma.
            }

            if (wrapInAppName) {
                sizeBytes += json._appName.Length + 4; // 2 for ":{" and 2 for quotation marks around string.

                // Checking if active application is defined.
                if (json._activeAppName != null) {
                    if (!json.calledFromStepSibling && json.StepSiblings != null && json.StepSiblings.Count != 1) {
                        // Serializing every sibling first.
                        for (int s = 0; s < json.StepSiblings.Count; s++) {
                            var pp = json.StepSiblings[s];

                            if (pp == json)
                                continue;

                            // Checking if application name is the same.
                            if (json._activeAppName == pp._appName) {
                                // Checking if there is any partial Html provided.
                                partialConfigId = pp.GetHtmlPartialUrl();
                                break;
                            }
                        }
                    }
                    activeAppName = json._activeAppName;
                } else {
                    // Checking if there is any partial Html provided.
                    partialConfigId = json.GetHtmlPartialUrl();
                    activeAppName = json._appName;

                    if (!String.IsNullOrEmpty(partialConfigId)) {
                        htmlUriMerged = activeAppName + "=" + partialConfigId;
                        sizeBytes += 15 + partialConfigId.Length; // "PartialId":"",
                    }
                }
            
                // Checking if we have any siblings. Since the array contains all stepsiblings (including this object)
                // we check if we have more than one stepsibling.
                if (!json.calledFromStepSibling && json.StepSiblings != null && json.StepSiblings.Count != 1) {
                    // For comma.
                    sizeBytes++;

                    // Calculating the size for each step sibling.
                    foreach (Json pp in json.StepSiblings) {
                        if (pp == json)
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
                    htmlUriMerged = StarcounterConstants.PolyjuiceHtmlMergerPrefix + htmlUriMerged;
                    sizeBytes += htmlUriMerged.Length;

                    if (!string.IsNullOrEmpty(partialConfigId)) {

                        // "AppName":"",
                        sizeBytes += 13 + activeAppName.Length;

                        // "PartialId":"",
                        sizeBytes += 15 + partialConfigId.Length;

                        string setupStr = null;
                        try {
                            setupStr = StarcounterBase._DB.SQL<string>("SELECT p.Value FROM JuicyTilesSetup p WHERE p.Key = ?", partialConfigId).First;
                        } catch { }

                        if (setupStr != null) {
                            sizeBytes += setupStr.Length + 21; // "juicyTilesSetup":"",
                        }
                    }
                }
            }

            sizeBytes += 1; // 1 for "}".
            return sizeBytes;
        }

        private static int EstimateArray(TypedJsonSerializer serializer, Json json, Template template) {
            Json arr = json;

            if (template != null) {
                // Not a root. Get correct value from getter.
                arr = ((TObjArr)template).Getter(json);
            }

            int sizeBytes = 2; // 2 for "[]".
            IList arrList = (IList)arr;

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

        private static int SerializeObject(TypedJsonSerializer serializer, Json json, Template template, IntPtr dest, int destSize) {
            int valueSize;
            int used = 0;
            List<Template> exposedProperties;
            string htmlUriMerged = null;
            string partialConfigId = null;
            string activeAppName = null;
            Session session;

            if (template != null)
                json = ((TObject)template).Getter(json);

            session = json.Session;

            // Checking if application name should wrap the JSON.
            bool wrapInAppName = WrapInAppName(session, json);

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
                    // Checking if active application is defined.
                    if (json._activeAppName != null) {
                        if (!json.calledFromStepSibling && json.StepSiblings != null && json.StepSiblings.Count != 1) {
                            // Serializing every sibling first.
                            for (int s = 0; s < json.StepSiblings.Count; s++) {
                                var pp = json.StepSiblings[s];

                                if (pp == json)
                                    continue;

                                // Checking if application name is the same.
                                if (json._activeAppName == pp._appName) {
                                    // Checking if there is any partial Html provided.
                                    partialConfigId = pp.GetHtmlPartialUrl();
                                    break;
                                }
                            }
                        }

                        activeAppName = json._activeAppName;
                        json._activeAppName = null;
                    } else {
                        // Checking if there is any partial Html provided.
                        partialConfigId = json.GetHtmlPartialUrl();
                        activeAppName = json._appName;

                        if (!String.IsNullOrEmpty(partialConfigId)) {
                            htmlUriMerged = activeAppName + "=" + partialConfigId;
                        }
                    }

                    valueSize = JsonHelper.WriteStringAsIs((IntPtr)pfrag, destSize - used, json._appName);
                    pfrag += valueSize;
                    used += valueSize;

                    *pfrag++ = (byte)':';
                    used++;

                    *pfrag++ = (byte)'{';
                    used++;
                }

                exposedProperties = ((TObject)json.Template).Properties.ExposedProperties;

                if (session != null && json == session.PublicViewModel && session.CheckOption(SessionOptions.PatchVersioning)) {
                    // add serverversion and clientversion to the serialized json if we have a root.
                    valueSize = JsonHelper.WriteStringAsIs((IntPtr)pfrag, destSize - used, Starcounter.XSON.JsonPatch.ClientVersionPropertyName);
                    used += valueSize;
                    pfrag += valueSize;
                    *pfrag++ = (byte)':';
                    used++;
                    valueSize = JsonHelper.WriteInt((IntPtr)pfrag, destSize - used, session.ClientVersion);
                    used += valueSize;
                    pfrag += valueSize;
                    *pfrag++ = (byte)',';
                    used++;

                    valueSize = JsonHelper.WriteStringAsIs((IntPtr)pfrag, destSize - used, Starcounter.XSON.JsonPatch.ServerVersionPropertyName);
                    used += valueSize;
                    pfrag += valueSize;
                    *pfrag++ = (byte)':';
                    used++;
                    valueSize = JsonHelper.WriteInt((IntPtr)pfrag, destSize - used, session.ServerVersion);
                    used += valueSize;
                    pfrag += valueSize;

                    if (exposedProperties.Count > 0) {
                        *pfrag++ = (byte)',';
                        used++;
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

                // Wrapping in application name.
                if (wrapInAppName) {
                    *pfrag++ = (byte)'}';
                    used++;

                    // Checking if we have any siblings. Since the array contains all stepsiblings (including this object)
                    // we check if we have more than one stepsibling.
                    if (!json.calledFromStepSibling && json.StepSiblings != null && json.StepSiblings.Count != 1) {
                        bool addComma = true;

                        // Serializing every sibling first.
                        for (int s = 0; s < json.StepSiblings.Count; s++) {
                            var pp = json.StepSiblings[s];

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

                                    htmlUriMerged += pp._appName + "=" + pp.GetHtmlPartialUrl();
                                }

                                valueSize = JsonHelper.WriteStringAsIs((IntPtr)pfrag, destSize - used, pp._appName);
                                used += valueSize;
                                pfrag += valueSize;

                                *pfrag++ = (byte)':';
                                used++;

                                valueSize = pp.ToJsonUtf8((IntPtr)pfrag, destSize - used);
                                pfrag += valueSize;
                                used += valueSize;

                                if ((s + 1) < json.StepSiblings.Count) {
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
                        htmlUriMerged = StarcounterConstants.PolyjuiceHtmlMergerPrefix + htmlUriMerged;

                        valueSize = JsonHelper.WriteString((IntPtr)pfrag, destSize - used, htmlUriMerged);
                        used += valueSize;
                        pfrag += valueSize;

                        string setupStr = null;
                        if (!string.IsNullOrEmpty(partialConfigId)) {

                            *pfrag++ = (byte)',';
                            used++;

                            valueSize = JsonHelper.WriteString((IntPtr)pfrag, destSize - used, "AppName");
                            used += valueSize;
                            pfrag += valueSize;

                            *pfrag++ = (byte)':';
                            used++;

                            valueSize = JsonHelper.WriteString((IntPtr)pfrag, destSize - used, activeAppName);
                            used += valueSize;
                            pfrag += valueSize;

                            *pfrag++ = (byte)',';
                            used++;

                            valueSize = JsonHelper.WriteString((IntPtr)pfrag, destSize - used, "PartialId");
                            used += valueSize;
                            pfrag += valueSize;

                            *pfrag++ = (byte)':';
                            used++;

                            valueSize = JsonHelper.WriteString((IntPtr)pfrag, destSize - used, partialConfigId);
                            used += valueSize;
                            pfrag += valueSize;

                            try {
                                setupStr = StarcounterBase._DB.SQL<string>("SELECT p.Value FROM JuicyTilesSetup p WHERE p.Key = ?", partialConfigId).First;
                            } catch { }
                        }

                        if (setupStr != null) {
                            *pfrag++ = (byte)',';
                            used++;

                            valueSize = JsonHelper.WriteString((IntPtr)pfrag, destSize - used, "juicyTilesSetup");
                            used += valueSize;
                            pfrag += valueSize;

                            *pfrag++ = (byte)':';
                            used++;

                            valueSize = JsonHelper.WriteStringNoQuotations(pfrag, destSize - used, setupStr);
                            used += valueSize;
                            pfrag += valueSize;
                        }

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

        private static int SerializeArray(TypedJsonSerializer serializer, Json json, Template template, IntPtr dest, int destSize) {
            int used = 0;
            int valueSize;
            IList arrList;
            Json arr = json;

            if (template != null)
                arr = ((TObjArr)template).Getter(json);

            unsafe {
                byte* pfrag = (byte*)dest;

                *pfrag++ = (byte)'[';
                used++;


                arrList = (IList)arr;
                for (int i = 0; i < arrList.Count; i++) {
                    valueSize = ((Json)arrList[i]).ToJsonUtf8((IntPtr)pfrag, destSize - used);

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

            if (template != null) {
                tObject = (TObject)template;
                json = tObject.Getter(json);
            } else {
                tObject = (TObject)json.Template;
            }

            while (reader.GotoProperty()) {
                propertyName = reader.ReadString();
                tProperty = tObject.Properties.GetExposedTemplateByName(propertyName);
                if (tProperty == null) {
                    JsonHelper.ThrowPropertyNotFoundException(propertyName);
                }

                reader.GotoValue();

                if (tProperty.TemplateTypeId == (int)TemplateTypeEnum.Object) {
                    reader.PopulateObject(((TObject)tProperty).Getter(json));
                } else {
                    populatePerTemplate[(int)tProperty.TemplateTypeId](json, tProperty, reader);
                }
            }
            reader.Skip(1);
        }

        private static void PopulateArray(Json json, Template template, JsonReader reader) {
            Json childObj;

            if (template != null) {
                json = ((TObjArr)template).Getter(json);
            }
            
            JsonReader arrayReader = reader.CreateSubReader();
            while (arrayReader.GotoNextObject()) {
                childObj = json.NewItem();
                arrayReader.PopulateObject(childObj);
            }
            reader.Skip(arrayReader.Used + 1);
        }

        private static void PopulateTrigger(Json json, Template template, JsonReader reader) {
            // Should not get here. Not sure how triggers should be handled.
            throw new NotImplementedException();
        }

        public override int Populate(Json json, IntPtr source, int sourceSize) {
            var reader = new JsonReader(source, sourceSize);
            populatePerTemplate[(int)json.Template.TemplateTypeId](json, null, reader);
            return reader.Used;
        }
    }
}