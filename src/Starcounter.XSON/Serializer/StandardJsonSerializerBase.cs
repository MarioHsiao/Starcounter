using System;
using System.Collections;
using System.Collections.Generic;
using Starcounter.Internal;
using Starcounter.Templates;
using Starcounter.XSON;

namespace Starcounter.Advanced.XSON {
    public abstract class StandardJsonSerializerBase : TypedJsonSerializer {
        private delegate int EstimateSizeDelegate(TypedJsonSerializer serializer, 
                                                  Json json, 
                                                  Template template, 
                                                  JsonSerializerSettings settings = null);

        private delegate int SerializeDelegate(TypedJsonSerializer serializer, 
                                               Json json, 
                                               Template template, 
                                               IntPtr dest, 
                                               int destSize, 
                                               JsonSerializerSettings settings = null);

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

        public override int EstimateSizeBytes(Json json, JsonSerializerSettings settings = null) {
            return json.Scope<TypedJsonSerializer, Json, JsonSerializerSettings, int>((tjs, j, s) => {
                int size = 0;

                if (s == null)
                    s = TypedJsonSerializer.DefaultSettings;

                if (j.Template != null) {
                    return estimatePerTemplate[(int)j.Template.TemplateTypeId](tjs, j, j.Template);
                } else {
                    // No template defined. Assuming object.
                    size = EstimateObject(this, j, s);
                }

                AssertEstimatedSize(json, null, size);
                return size;
            },
            this,
            json,
            settings);
        }

        public override int EstimateSizeBytes(Json json, Template property, JsonSerializerSettings settings = null) {
            if (settings == null)
                settings = TypedJsonSerializer.DefaultSettings;

            return json.Scope<TypedJsonSerializer, Json, Template, JsonSerializerSettings, int>((tjs, j, t, s) => {
                int size = estimatePerTemplate[(int)t.TemplateTypeId](tjs, j, t);
                AssertEstimatedSize(json, property, size);
                return size;
            },
            this,
            json,
            property,
            settings);
        }

        public override int Serialize(Json json, IntPtr dest, int destSize, JsonSerializerSettings settings = null) {  
            bool oldValue = json.checkBoundProperties;
            try {
                json.checkBoundProperties = false;

                if (settings == null)
                    settings = TypedJsonSerializer.DefaultSettings;

                int realSize = json.Scope<TypedJsonSerializer, Json, IntPtr, int, JsonSerializerSettings, int>(
                        (tjs, j, d, ds, s) => {
                            if (j.Template != null) {
                                return serializePerTemplate[(int)j.Template.TemplateTypeId](tjs, j, j.Template, d, ds, s);
                            } else {
                                // No template defined. Assuming object.
                                return SerializeObject(this, j, d, ds, s);
                            }
                        },
                        this,
                        json,
                        dest,
                        destSize,
                        settings);

                AssertWrittenSize(json, null, realSize, destSize);
                return realSize;
            } finally {
                json.checkBoundProperties = oldValue;
            }
        }

        public override int Serialize(Json json, 
                                      Template property, 
                                      IntPtr dest, 
                                      int destSize, 
                                      JsonSerializerSettings settings = null) {
            bool oldValue = json.checkBoundProperties;
            try {
                json.checkBoundProperties = false;

                if (settings == null)
                    settings = TypedJsonSerializer.DefaultSettings;

                int realSize 
                    = json.Scope<TypedJsonSerializer, Json, Template, IntPtr, int, JsonSerializerSettings, int>(
                        (tjs, j, t, d, ds, s) => {
                            return serializePerTemplate[(int)t.TemplateTypeId](tjs, j, t, d, ds, s);
                        },
                        this,
                        json,
                        property,
                        dest,
                        destSize,
                        settings);

                AssertWrittenSize(json, property, realSize, destSize);
                return realSize;
            } finally {
                json.checkBoundProperties = oldValue;
            }
        }

        private static void AssertEstimatedSize(Json json, Template template, int estimatedSize) {
            if (estimatedSize > StarcounterConstants.NetworkConstants.MaxResponseSize) {
                var errMsg = "TypedJson serializer: Estimated needed size for serializing is larger ";
                errMsg += "than max allowed size (500MB).\r\n";
                errMsg += "Estimated needed size: " + estimatedSize + "\r\n";
                errMsg += "Json: " + JsonDebugHelper.ToBasicString(json, template);
                throw new Exception(errMsg);
            }
        }

        private static void AssertWrittenSize(Json json, Template template, int realSize, int destSize) {
            if (realSize > destSize) {
                var errMsg = "TypedJson serializer: written size is larger than size of destination!\r\n";
                errMsg += "Written: " + realSize + ", destination: " + destSize + "\r\n";
                errMsg += "Json: " + JsonDebugHelper.ToBasicString(json, template);
                throw new Exception(errMsg);
            }
        }

        private static int EstimateException(TypedJsonSerializer serializer, 
                                             Json json, 
                                             Template template, 
                                             JsonSerializerSettings settings) {
            throw new Exception("Cannot estimate size of Json. The type of template is unknown: " 
                                + template.GetType());
        }

        private static int EstimateBool(TypedJsonSerializer serializer, 
                                        Json json, 
                                        Template template, 
                                        JsonSerializerSettings settings) {
            ((TBool)template).SetCachedReads(json);
            return 5;
        }

        private static int EstimateDecimal(TypedJsonSerializer serializer, 
                                           Json json, 
                                           Template template, 
                                           JsonSerializerSettings settings) {
            ((TDecimal)template).SetCachedReads(json);
            return 32;
        }

        private static int EstimateDouble(TypedJsonSerializer serializer, 
                                          Json json, 
                                          Template template, 
                                          JsonSerializerSettings settings) {
            ((TDouble)template).SetCachedReads(json);
            return 32;
        }

        private static int EstimateLong(TypedJsonSerializer serializer, 
                                        Json json, 
                                        Template template, 
                                        JsonSerializerSettings settings) {
            ((TLong)template).SetCachedReads(json);
            return 32;
        }

        private static int EstimateString(TypedJsonSerializer serializer, 
                                          Json json, 
                                          Template template, 
                                          JsonSerializerSettings settings) {
            TString strTemplate = (TString)template;
            strTemplate.SetCachedReads(json);
            string value = strTemplate.Getter(json);

            if (value != null)
                return value.Length * 2 + 2;
            return 2;
        }

        private static int ScopeAndEstimateObject(TypedJsonSerializer serializer, 
                                                  Json json, 
                                                  Template template, 
                                                  JsonSerializerSettings settings) {
            Json parent = json;
            if (template != json.Template) {
                // Template points to a property in the specified jsonobject. Get correct value from template getter.
                ((TObject)template).SetCachedReads(json);
                json = ((TObject)template).Getter(parent);
            }

            int size = 2;
            if (json != null) {
                size = json.Scope<TypedJsonSerializer, Json, JsonSerializerSettings, int>(EstimateObject, 
                                                                                          serializer, 
                                                                                          json, 
                                                                                          settings);
            }
            return size;
        }

        private static int EstimateObject(TypedJsonSerializer serializer, Json json, JsonSerializerSettings settings) {
            Session session = json.Session;
            int sizeBytes = 0;

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

            if (json.Template != null) {
                exposedProperties = ((TObject)json.Template).Properties.ExposedProperties;
                for (int i = 0; i < exposedProperties.Count; i++) {
                    tProperty = exposedProperties[i];

                    sizeBytes += tProperty.TemplateName.Length + 3; // 1 for : and to for quotation marks around string.
                    sizeBytes += estimatePerTemplate[(int)tProperty.TemplateTypeId](serializer, json, tProperty);
                    sizeBytes += 1; // 1 for comma.
                }
            }

            if (wrapInAppName) {
                sizeBytes += json.appName.Length + 4; // 2 for ":{" and 2 for quotation marks around string.

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
                            sizeBytes += pp.appName.Length + 1; // 1 for ":".
                            sizeBytes += serializer.EstimateSizeBytes(pp) + 2; // 2 for ",".
                        } finally {
                            pp.calledFromStepSibling = false;
                        }
                    }
                }

                // ,"Html":"" is 10 characters
                sizeBytes += 10;
            }

            sizeBytes += 1; // 1 for "}".
            return sizeBytes;
        }

        private static int ScopeAndEstimateArray(TypedJsonSerializer serializer, 
                                                 Json json, 
                                                 Template template, 
                                                 JsonSerializerSettings settings) {
            Json parent = json;

            if (template != parent.Template) {
                // Not a root. Get correct value from getter.
                ((TObjArr)template).SetCachedReads(json);
                json = ((TObjArr)template).Getter(parent);
            }
            int size = 2;
            if (json != null) {
                size = json.Scope<TypedJsonSerializer, Json, JsonSerializerSettings, int>(EstimateArray, 
                                                                                          serializer, 
                                                                                          json, 
                                                                                          settings);
            }
            return size;
        }

        private static int EstimateArray(TypedJsonSerializer serializer, Json json, JsonSerializerSettings settings) {
            int sizeBytes = 2; // 2 for "[]".
            IList arrList = (IList)json;

            for (int i = 0; i < arrList.Count; i++) {
                var rowJson = (Json)arrList[i];
                sizeBytes += serializer.EstimateSizeBytes(rowJson) + 1;

                AssertEstimatedSize(json, null, sizeBytes);
            }
            return sizeBytes;
        }

        private static int EstimateTrigger(TypedJsonSerializer serializer, 
                                           Json json, 
                                           Template template, 
                                           JsonSerializerSettings settings) {
            return 4;
        }

        private static int SerializeException(TypedJsonSerializer serializer, 
                                              Json json, 
                                              Template template, 
                                              IntPtr dest, 
                                              int destSize, 
                                              JsonSerializerSettings settings) {
            throw new Exception("Cannot serialize Json. The type of template is unknown: " + template.GetType());
        }

        private static int SerializeBool(TypedJsonSerializer serializer, 
                                         Json json, 
                                         Template template, 
                                         IntPtr dest, 
                                         int destSize, 
                                         JsonSerializerSettings settings) {
            if (template == null)
                template = json.Template;

            bool value = ((TBool)template).Getter(json);
            return JsonHelper.WriteBool(dest, destSize, value);
        }

        private static int SerializeDecimal(TypedJsonSerializer serializer, 
                                            Json json, 
                                            Template template, 
                                            IntPtr dest, 
                                            int destSize, 
                                            JsonSerializerSettings settings) {
            if (template == null)
                template = json.Template;

            decimal value = ((TDecimal)template).Getter(json);
            return JsonHelper.WriteDecimal(dest, destSize, value);
        }

        private static int SerializeDouble(TypedJsonSerializer serializer, 
                                           Json json, 
                                           Template template, 
                                           IntPtr dest, 
                                           int destSize, 
                                           JsonSerializerSettings settings) {
            if (template == null)
                template = json.Template;

            double value = ((TDouble)template).Getter(json);
            return JsonHelper.WriteDouble(dest, destSize, value);
        }

        private static int SerializeLong(TypedJsonSerializer serializer, 
                                         Json json, 
                                         Template template, 
                                         IntPtr dest, 
                                         int destSize, 
                                         JsonSerializerSettings settings) {
            if (template == null)
                template = json.Template;

            long value = ((TLong)template).Getter(json);
            return JsonHelper.WriteInt(dest, destSize, value);
        }

        private static int SerializeString(TypedJsonSerializer serializer, 
                                           Json json, 
                                           Template template, 
                                           IntPtr dest, 
                                           int destSize, 
                                           JsonSerializerSettings settings) {
            if (template == null)
                template = json.Template;

            string value = ((TString)template).Getter(json);
            return JsonHelper.WriteString(dest, destSize, value);
        }

        private static int ScopeAndSerializeObject(TypedJsonSerializer serializer, 
                                                   Json json, 
                                                   Template template, 
                                                   IntPtr dest, 
                                                   int destSize, 
                                                   JsonSerializerSettings settings) {
            Json parent = json;

            if (template != parent.Template)
                json = ((TObject)template).Getter(json);

            if (json != null) {
                return json.Scope<TypedJsonSerializer, Json, IntPtr, int, JsonSerializerSettings, int>(SerializeObject, 
                                                                                                       serializer, 
                                                                                                       json, 
                                                                                                       dest, 
                                                                                                       destSize, 
                                                                                                       settings);
            } else {
                unsafe
                {
                    byte* pdest = (byte*)dest;
                    *pdest++ = (byte)'{';
                    *pdest++ = (byte)'}';
                }
                return 2;
            }
        }

        private static int SerializeObject(TypedJsonSerializer serializer, 
                                           Json json, 
                                           IntPtr dest, 
                                           int destSize, 
                                           JsonSerializerSettings settings) {
            int valueSize;
            int used = 0;
            List<Template> exposedProperties = null;
            Session session = json.Session;

            // Checking if application name should wrap the JSON.
            bool wrapInAppName = ShouldBeNamespaced(json, session);

            unsafe
            {
                byte* pfrag = (byte*)dest;

                *pfrag++ = (byte)'{';
                used++;

                if (wrapInAppName) {
                    valueSize = JsonHelper.WriteStringAsIs((IntPtr)pfrag, destSize - used, json.appName);
                    pfrag += valueSize;
                    used += valueSize;

                    *pfrag++ = (byte)':';
                    used++;

                    *pfrag++ = (byte)'{';
                    used++;
                }

                if (json.Template != null) {
                    exposedProperties = ((TObject)json.Template).Properties.ExposedProperties;
                }

                if (session != null && json == session.PublicViewModel) {
                    var patchVersion = (json.ChangeLog != null) ? json.ChangeLog.Version : null;
                    if (patchVersion != null) {
                        // add serverversion and clientversion to the serialized json if we have a root.
                        valueSize = JsonHelper.WriteStringAsIs((IntPtr)pfrag, 
                                                               destSize - used, 
                                                               patchVersion.RemoteVersionPropertyName);

                        used += valueSize;
                        pfrag += valueSize;
                        *pfrag++ = (byte)':';
                        used++;
                        valueSize = JsonHelper.WriteInt((IntPtr)pfrag, destSize - used, patchVersion.RemoteVersion);
                        used += valueSize;
                        pfrag += valueSize;
                        *pfrag++ = (byte)',';
                        used++;

                        valueSize = JsonHelper.WriteStringAsIs((IntPtr)pfrag, 
                                                               destSize - used, 
                                                               patchVersion.LocalVersionPropertyName);
                        used += valueSize;
                        pfrag += valueSize;
                        *pfrag++ = (byte)':';
                        used++;
                        valueSize = JsonHelper.WriteInt((IntPtr)pfrag, destSize - used, patchVersion.LocalVersion);
                        used += valueSize;
                        pfrag += valueSize;

                        if (exposedProperties != null && exposedProperties.Count > 0) {
                            *pfrag++ = (byte)',';
                            used++;
                        }
                    }
                }

                if (exposedProperties != null) {
                    for (int i = 0; i < exposedProperties.Count; i++) {
                        Template tProperty = exposedProperties[i];

                        valueSize = JsonHelper.WriteStringAsIs((IntPtr)pfrag, destSize - used, tProperty.TemplateName);
                        used += valueSize;
                        pfrag += valueSize;

                        *pfrag++ = (byte)':';
                        used++;

                        // TODO:
                        // If we have an object with another serializer set, this code wont 
                        // work since it will bypass the setting.
                        valueSize = serializePerTemplate[(int)tProperty.TemplateTypeId](serializer, 
                                                                                        json, 
                                                                                        tProperty, 
                                                                                        (IntPtr)pfrag, 
                                                                                        destSize - used);
                        used += valueSize;
                        pfrag += valueSize;

                        if ((i + 1) < exposedProperties.Count) {
                            *pfrag++ = (byte)',';
                            used++;
                        }
                    }
                }

                if (wrapInAppName) {
                    *pfrag++ = (byte)'}';
                    used++;

                    // Checking if we have any siblings. Since the array contains all stepsiblings,
                    // including this object, we check if we have more than one stepsibling.
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
                }

                *pfrag++ = (byte)'}';
                used++;
            }

            return used;
        }

        private static int ScopeAndSerializeArray(TypedJsonSerializer serializer, 
                                                  Json json, 
                                                  Template template, 
                                                  IntPtr dest, 
                                                  int destSize, 
                                                  JsonSerializerSettings settings) {
            Json parent = json;

            if (template != parent.Template)
                json = ((TObjArr)template).Getter(json);

            if (json != null) {
                return json.Scope<TypedJsonSerializer, Json, IntPtr, int, JsonSerializerSettings, int>(SerializeArray, 
                                                                                                       serializer, 
                                                                                                       json, 
                                                                                                       dest, 
                                                                                                       destSize, 
                                                                                                       settings);
            } else {
                unsafe
                {
                    byte* pdest = (byte*)dest;
                    *pdest++ = (byte)'[';
                    *pdest++ = (byte)']';
                    return 2;
                }
            }
        }

        private static int SerializeArray(TypedJsonSerializer serializer, 
                                          Json json, 
                                          IntPtr dest, 
                                          int destSize, 
                                          JsonSerializerSettings settings) {
            int used = 0;
            int valueSize;
            IList arrList;
            Json arrItem;

            unsafe
            {
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

        private static int SerializeTrigger(TypedJsonSerializer serializer, 
                                            Json json, 
                                            Template template, 
                                            IntPtr dest, 
                                            int destSize, 
                                            JsonSerializerSettings settings) {
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
}
