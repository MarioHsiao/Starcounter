using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Starcounter.Advanced.XSON;
using Starcounter.Internal;
using Starcounter.Templates;
using Starcounter.XSON.Interfaces;
using Newt = Newtonsoft.Json;

namespace Starcounter.XSON {
    public class NewtonSoftSerializer : ITypedJsonSerializer {
        // TODOS:
        // - Try to find a way to not box valuetypes when deserializing.
        // - Same serializer is used for children instead of checking if custom serializer is specified.

        private delegate void SerializeDelegate(Json json, Template template, Newt.JsonWriter writer, JsonSerializerSettings settings);
        private delegate void DeserializeDelegate(Json json, Template template, Newt.JsonTextReader reader, JsonSerializerSettings settings);

        private static SerializeDelegate[] serializePerTemplate;
        private static DeserializeDelegate[] deserializePerTemplate;
        
        static NewtonSoftSerializer() {
            serializePerTemplate = new SerializeDelegate[8];
            serializePerTemplate[(int)TemplateTypeEnum.Unknown] = SerializeException;
            serializePerTemplate[(int)TemplateTypeEnum.Bool] = SerializeBool;
            serializePerTemplate[(int)TemplateTypeEnum.Decimal] = SerializeDecimal;
            serializePerTemplate[(int)TemplateTypeEnum.Double] = SerializeDouble;
            serializePerTemplate[(int)TemplateTypeEnum.Long] = SerializeLong;
            serializePerTemplate[(int)TemplateTypeEnum.String] = SerializeString;
            serializePerTemplate[(int)TemplateTypeEnum.Object] = ScopeAndSerializeObject;
            serializePerTemplate[(int)TemplateTypeEnum.Array] = ScopeAndSerializeArray;

            deserializePerTemplate = new DeserializeDelegate[8];
            deserializePerTemplate[(int)TemplateTypeEnum.Unknown] = DeserializeException;
            deserializePerTemplate[(int)TemplateTypeEnum.Bool] = DeserializeBool;
            deserializePerTemplate[(int)TemplateTypeEnum.Decimal] = DeserializeDecimal;
            deserializePerTemplate[(int)TemplateTypeEnum.Double] = DeserializeDouble;
            deserializePerTemplate[(int)TemplateTypeEnum.Long] = DeserializeLong;
            deserializePerTemplate[(int)TemplateTypeEnum.String] = DeserializeString;
            deserializePerTemplate[(int)TemplateTypeEnum.Object] = DeserializeObject;
            deserializePerTemplate[(int)TemplateTypeEnum.Array] = DeserializeArray;
        }

        public string Serialize(Json json, JsonSerializerSettings settings = null) {
            return Serialize(json, json.Template, settings);
        }

        public void Serialize(Json json, Stream stream, JsonSerializerSettings settings = null) {
            Serialize(json, json.Template, stream, settings);
        }

        public void Serialize(Json json, TextWriter textWriter, JsonSerializerSettings settings = null) {
            Serialize(json, json.Template, textWriter, settings); 
        }

        public string Serialize(Json json, Template template, JsonSerializerSettings settings = null) {
            StringBuilder sb = new StringBuilder();
            Serialize(json, template, new StringWriter(sb), settings);
            return sb.ToString();
        }

        public void Serialize(Json json, Template template, Stream stream, JsonSerializerSettings settings = null) {
            Serialize(json, template, new StreamWriter(stream), settings);
        }

        public void Serialize(Json json, Template template, TextWriter textWriter, JsonSerializerSettings settings = null) {
            if (settings == null)
                settings = JsonSerializerSettings.Default;

            using (var writer = new Newt.JsonTextWriter(textWriter)) {
                writer.CloseOutput = false;
                json.Scope(() => {
                    if (template != null) {
                        serializePerTemplate[(int)template.TemplateTypeId](json, template, writer, settings);
                    } else {
                        // No template defined. Assuming object.
                        SerializeObject(json, writer, settings);
                    }
                });
                writer.Flush();
            }
        }

        public T Deserialize<T>(string source, JsonSerializerSettings settings = null) where T : Json, new() {
            T json = new T();
            Deserialize(json, source, settings);
            return json;
        }

        public T Deserialize<T>(Stream stream, JsonSerializerSettings settings = null) where T : Json, new() {
            T json = new T();
            Deserialize(json, stream, settings);
            return json;
        }

        public T Deserialize<T>(TextReader textReader, JsonSerializerSettings settings = null) where T : Json, new() {
            T json = new T();
            Deserialize(json, textReader, settings);
            return json;
        }

        public void Deserialize(Json json, string source, JsonSerializerSettings settings = null) {
            Deserialize(json, new StringReader(source), settings);
        }

        public void Deserialize(Json json, Stream stream, JsonSerializerSettings settings = null) {
            Deserialize(json, new StreamReader(stream), settings);
        }

        public void Deserialize(Json json, TextReader textReader, JsonSerializerSettings settings = null) {
            var reader = new Newt.JsonTextReader(textReader);
            reader.DateParseHandling = Newt.DateParseHandling.None; // Treat dates as strings.

            if (settings == null)
                settings = JsonSerializerSettings.Default;

            var template = json.Template;

            SpecialRead(reader, template);
            deserializePerTemplate[(int)template.TemplateTypeId](json, template, reader, settings);
        }

        private static void SerializeException(Json json, 
                                               Template template,
                                               Newt.JsonWriter writer, 
                                               JsonSerializerSettings settings) {
            throw new Exception("Cannot serialize Json. The type of template is unknown: " + template.GetType());
        }

        private static void SerializeBool(Json json, 
                                          Template template,
                                          Newt.JsonWriter writer,
                                          JsonSerializerSettings settings) {
            if (template == null)
                template = json.Template;

            bool value = ((TBool)template).Getter(json);
            writer.WriteValue(value);
        }

        private static void SerializeDecimal(Json json, 
                                            Template template,
                                            Newt.JsonWriter writer,
                                            JsonSerializerSettings settings) {
            if (template == null)
                template = json.Template;

            decimal value = ((TDecimal)template).Getter(json);
            writer.WriteValue(value);
        }

        private static void SerializeDouble(Json json, 
                                           Template template,
                                           Newt.JsonWriter writer,
                                           JsonSerializerSettings settings) {
            if (template == null)
                template = json.Template;

            double value = ((TDouble)template).Getter(json);
            writer.WriteValue(value);
        }

        private static void SerializeLong(Json json, 
                                         Template template,
                                         Newt.JsonWriter writer,
                                         JsonSerializerSettings settings) {
            if (template == null)
                template = json.Template;

            long value = ((TLong)template).Getter(json);
            writer.WriteValue(value);
        }

        private static void SerializeString(Json json, 
                                           Template template,
                                           Newt.JsonWriter writer,
                                           JsonSerializerSettings settings) {
            if (template == null)
                template = json.Template;

            string value = ((TString)template).Getter(json);
            writer.WriteValue(value);
        }

        private static void ScopeAndSerializeObject(Json json, 
                                                    Template template,
                                                    Newt.JsonWriter writer,
                                                    JsonSerializerSettings settings) {
            Json parent = json;

            if (template != parent.Template)
                json = ((TObject)template).Getter(json);

            if (json != null) {
                json.Scope(() => {
                    SerializeObject(json, writer, settings);
                });
            } else {
                writer.WriteStartObject();
                writer.WriteEndObject();
            }
        }

        private static void SerializeObject(Json json,
                                            Newt.JsonWriter writer,
                                            JsonSerializerSettings settings) {
            List<Template> exposedProperties = null;
            Session session = json.Session;

            // Checking if application name should wrap the JSON.
            bool wrapInAppName = ShouldBeNamespaced(json, session);

            writer.WriteStartObject();

            if (wrapInAppName) {
                writer.WritePropertyName(json.appName);
                writer.WriteStartObject();
            }

            if (json.Template != null) {
                exposedProperties = ((TObject)json.Template).Properties.ExposedProperties;
            }

            if (session != null && json == session.PublicViewModel) {
                var patchVersion = (json.ChangeLog != null) ? json.ChangeLog.Version : null;
                if (patchVersion != null) {
                    // add serverversion and clientversion to the serialized json if we have a root.
                    writer.WritePropertyName(patchVersion.RemoteVersionPropertyName);
                    writer.WriteValue(patchVersion.RemoteVersion);

                    writer.WritePropertyName(patchVersion.LocalVersionPropertyName);
                    writer.WriteValue(patchVersion.LocalVersion);
                }
            }

            if (exposedProperties != null) {
                for (int i = 0; i < exposedProperties.Count; i++) {
                    Template tProperty = exposedProperties[i];

                    writer.WritePropertyName(tProperty.TemplateName);
                    
                    // TODO:
                    // If we have an object with another serializer set, this code wont 
                    // work since it will bypass the setting.
                    serializePerTemplate[(int)tProperty.TemplateTypeId](json, tProperty, writer, settings);
                }
            }

            if (wrapInAppName) {
                writer.WriteEndObject();

                // Checking if we have any siblings. Since the array contains all stepsiblings,
                // including this object, we check if we have more than one stepsibling.
                if (!json.calledFromStepSibling && json.Siblings != null && json.Siblings.Count != 1) {
                    // Serializing every sibling first.
                    for (int s = 0; s < json.Siblings.Count; s++) {
                        var pp = json.Siblings[s];
                        if (pp == json)
                            continue;

                        pp.calledFromStepSibling = true;
                        try {
                            writer.WritePropertyName(pp.appName);

                            // TODO:
                            // If we have an object with another serializer set, this code will not 
                            // work since it will bypass the setting.
                            int templateType = (pp.Template != null) ? (int)pp.Template.TemplateTypeId : (int)TemplateTypeEnum.Object;
                            serializePerTemplate[templateType](pp, pp.Template, writer, settings);
                        } finally {
                            pp.calledFromStepSibling = false;
                        }
                    }
                }
            }

            writer.WriteEndObject();
        }

        private static void ScopeAndSerializeArray(Json json, 
                                                   Template template,
                                                   Newt.JsonWriter writer,
                                                   JsonSerializerSettings settings) {
            Json parent = json;

            if (template != parent.Template)
                json = ((TObjArr)template).Getter(json);

            if (json != null) {
                json.Scope(() => {
                    SerializeArray(json, writer, settings);
                });
            } else {
                writer.WriteStartArray();
                writer.WriteEndArray();
            }
        }

        private static void SerializeArray(Json json,
                                           Newt.JsonWriter writer,
                                           JsonSerializerSettings settings) {
            IList arrList;
            Json arrItem;

            writer.WriteStartArray();

            arrList = (IList)json;
            for (int i = 0; i < arrList.Count; i++) {
                arrItem = (Json)arrList[i];

                if (arrItem != null) {
                    // TODO:
                    // If we have an object with another serializer set, this code wont 
                    // work since it will bypass the setting.
                    int templateType = (arrItem.Template != null) ? (int)arrItem.Template.TemplateTypeId : (int)TemplateTypeEnum.Object;
                    serializePerTemplate[templateType](arrItem, arrItem.Template, writer, settings);
                } else {
                    writer.WriteStartObject();
                    writer.WriteEndObject();
                }
            }

            writer.WriteEndArray();
        }
        
        private static void DeserializeException(Json json, Template template, Newt.JsonTextReader reader, JsonSerializerSettings settings) {
            throw new Exception("Cannot populate Json. Unknown template: " + template.GetType());
        }

        private static void DeserializeBool(Json json, Template template, Newt.JsonTextReader reader, JsonSerializerSettings settings) {
            if (template == null)
                template = json.Template;

            bool value = default(bool);
            if (reader.TokenType == Newt.JsonToken.Boolean)
                value = ((bool?)reader.Value).GetValueOrDefault();
            else if (reader.TokenType != Newt.JsonToken.Null)
                ExceptionHelper.ThrowWrongValueType(null, template.TemplateName, template.JsonType, reader.Value?.ToString());
            ((TBool)template).Setter(json, value);
        }

        private static void DeserializeDecimal(Json json, Template template, Newt.JsonTextReader reader, JsonSerializerSettings settings) {
            if (template == null)
                template = json.Template;

            decimal value = default(decimal);
            if (reader.TokenType == Newt.JsonToken.Float)
                value = ((decimal?)reader.Value).GetValueOrDefault();
            else if (reader.TokenType != Newt.JsonToken.Null)
                ExceptionHelper.ThrowWrongValueType(null, template.TemplateName, template.JsonType, reader.Value?.ToString());
            
            ((TDecimal)template).Setter(json, value);
        }

        private static void DeserializeDouble(Json json, Template template, Newt.JsonTextReader reader, JsonSerializerSettings settings) {
            if (template == null)
                template = json.Template;
            double value = default(double);
            if (reader.TokenType == Newt.JsonToken.Float)
                value = ((double?)reader.Value).GetValueOrDefault();
            else if (reader.TokenType != Newt.JsonToken.Null)
                ExceptionHelper.ThrowWrongValueType(null, template.TemplateName, template.JsonType, reader.Value?.ToString());

            ((TDouble)template).Setter(json, value);
        }

        private static void DeserializeLong(Json json, Template template, Newt.JsonTextReader reader, JsonSerializerSettings settings) {
            if (template == null)
                template = json.Template;

            long value = default(long);
            if (reader.TokenType == Newt.JsonToken.Integer)
                value = ((long?)reader.Value).GetValueOrDefault();
            else if (reader.TokenType != Newt.JsonToken.Null)
                ExceptionHelper.ThrowWrongValueType(null, template.TemplateName, template.JsonType, reader.Value?.ToString());

            ((TLong)template).Setter(json, value);
        }

        private static void DeserializeString(Json json, Template template, Newt.JsonTextReader reader, JsonSerializerSettings settings) {
            if (template == null)
                template = json.Template;

            string value = "";
            if (reader.TokenType == Newt.JsonToken.String)
                value = (string)reader.Value;
            else if (reader.TokenType != Newt.JsonToken.Null) // if value is null we keep original value.
                ExceptionHelper.ThrowWrongValueType(null, template.TemplateName, template.JsonType, reader.Value?.ToString());
            
            ((TString)template).Setter(json, value);
        }

        private static void DeserializeObject(Json json, Template template, Newt.JsonTextReader reader, JsonSerializerSettings settings) {
            string propertyName;
            Template tProperty;
            TObject tObject;
            Newt.JsonToken token;

            token = reader.TokenType;
            if (token == Newt.JsonToken.Null) {
                reader.Skip();
                return;
            }

            if (token != Newt.JsonToken.StartObject)
                ExceptionHelper.ThrowInvalidJson("Expected object but found: " + reader.TokenType.ToString());

            tObject = (TObject)template;
            if (template != json.Template) {
                json = tObject.Getter(json);
            } 
            
            while (true) {
                if (!reader.Read())
                    throw ErrorCode.ToException(Error.SCERRJSONUNEXPECTEDENDOFCONTENT);

                token = reader.TokenType;
                if (token == Newt.JsonToken.EndObject) {
                    break;
                }
                
                if (!(token == Newt.JsonToken.PropertyName))
                    ExceptionHelper.ThrowInvalidJson("Expected name of property but found token: " + token.ToString());

                propertyName = (string)reader.Value;
                tProperty = tObject.Properties.GetExposedTemplateByName(propertyName);
                if (tProperty != null) {
                    // TODO:
                    // This will always use the same serializer and reader. So this wont allow a child to specify a different serializer.
                    SpecialRead(reader, tProperty);
                    deserializePerTemplate[(int)tProperty.TemplateTypeId](json, tProperty, reader, settings);
                } else {
                    // Property is not found in the template. 
                    // Depending on the settings we either raise an error or simply skip it and continue.
                    if (settings.MissingMemberHandling == MissingMemberHandling.Error) {
                        ExceptionHelper.ThrowPropertyNotFound(propertyName);
                    } else {
                        // TODO:
                        // Test this code with all different types.
                        reader.Skip();
                    }
                }
            }
        }

        private static void DeserializeArray(Json json, Template template, Newt.JsonTextReader reader, JsonSerializerSettings settings) {
            Json childObj;
            Newt.JsonToken token;

            token = reader.TokenType;
            if (token == Newt.JsonToken.Null) {
                reader.Skip();
                return;
            }

            if (token != Newt.JsonToken.StartArray)
                ExceptionHelper.ThrowInvalidJson("Expected array but found: " + token.ToString());

            TObjArr tArr = (TObjArr)template;
            if (tArr.ElementType == null) {
                // There is no way for us to know which template should be used for the child. Create a dynamic json from the array?
                reader.Skip();
                return;
            }

            if (template != json.Template) {
                json = tArr.Getter(json);
            }

            while (true) {
                SpecialRead(reader, tArr.ElementType);
                token = reader.TokenType;

                if (token == Newt.JsonToken.EndArray)
                    break;

                // TODO:
                // This will always use the same serializer and reader. So this wont allow a child to specify a different serializer.
                childObj = (Json)tArr.ElementType.CreateInstance();
                deserializePerTemplate[(int)childObj.Template.TemplateTypeId](childObj, childObj.Template, reader, settings);
                json._Add(childObj);
            }
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

        /// <summary>
        /// Sets the handling for float values depending of the type of the template, 
        /// and reads the next token. 
        /// </summary>
        /// <remarks>
        /// This method assumes that there are more tokens to read. If the reader returns
        /// false an exception is thrown.
        /// </remarks>
        /// <param name="reader">The reader</param>
        /// <param name="template">The template to check type from</param>
        /// <returns></returns>
        private static void SpecialRead(Newt.JsonTextReader reader, Template template) {
            if (template.TemplateTypeId == TemplateTypeEnum.Double)
                reader.FloatParseHandling = Newt.FloatParseHandling.Double;
            else 
                reader.FloatParseHandling = Newt.FloatParseHandling.Decimal;

            if (!reader.Read())
                ExceptionHelper.ThrowUnexpectedEndOfContent(null);       
        }
    }
}
