using System;
using Starcounter.Templates;

namespace Starcounter.Advanced.XSON {
    public class StandardJsonSerializer : StandardJsonSerializerBase {
        private delegate void PopulateDelegate(Json json, Template template, JsonReader reader, JsonSerializerSettings settings);

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

        private static void PopulateException(Json json, Template template, JsonReader reader, JsonSerializerSettings settings) {
            throw new Exception("Cannot populate Json. Unknown template: " + template.GetType());
        }

        private static void PopulateBool(Json json, Template template, JsonReader reader, JsonSerializerSettings settings) {
            if (template == null)
                template = json.Template;

            try {
                ((TBool)template).Setter(json, reader.ReadBool());
            } catch (InvalidCastException ex) {
                JsonHelper.ThrowWrongValueTypeException(ex, template.TemplateName, template.JsonType, reader.ReadString());
            }
        }

        private static void PopulateDecimal(Json json, Template template, JsonReader reader, JsonSerializerSettings settings) {
            if (template == null)
                template = json.Template;

            try {
            ((TDecimal)template).Setter(json, reader.ReadDecimal());
            } catch (InvalidCastException ex) {
                JsonHelper.ThrowWrongValueTypeException(ex, template.TemplateName, template.JsonType, reader.ReadString());
            }
        }

        private static void PopulateDouble(Json json, Template template, JsonReader reader, JsonSerializerSettings settings) {
            if (template == null)
                template = json.Template;

            try {
                ((TDouble)template).Setter(json, reader.ReadDouble());
            } catch (InvalidCastException ex) {
                JsonHelper.ThrowWrongValueTypeException(ex, template.TemplateName, template.JsonType, reader.ReadString());
            }
        }

        private static void PopulateLong(Json json, Template template, JsonReader reader, JsonSerializerSettings settings) {
            if (template == null)
                template = json.Template;

            try {
                ((TLong)template).Setter(json, reader.ReadLong());
            } catch (InvalidCastException ex) {
                JsonHelper.ThrowWrongValueTypeException(ex, template.TemplateName, template.JsonType, reader.ReadString());
            }
        }

        private static void PopulateString(Json json, Template template, JsonReader reader, JsonSerializerSettings settings) {
            if (template == null)
                template = json.Template;

            try {
                ((TString)template).Setter(json, reader.ReadString());
            } catch (InvalidCastException ex) {
                JsonHelper.ThrowWrongValueTypeException(ex, template.TemplateName, template.JsonType, reader.ReadString());
            }
        }

        private static void PopulateObject(Json json, Template template, JsonReader reader, JsonSerializerSettings settings) {
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
                if (tProperty != null) {
                    token = reader.ReadNext();
                    if (tProperty.TemplateTypeId == TemplateTypeEnum.Object) {
                        var childObj = ((TObject)tProperty).Getter(json);
                        var valueSize = childObj.PopulateFromJson(reader.CurrentPtr, reader.Size - reader.Position);
                        reader.Skip(valueSize);
                    } else {
                        populatePerTemplate[(int)tProperty.TemplateTypeId](json, tProperty, reader, settings);
                    }
                } else {
                    // Property is not found in the template. 
                    // Depending on the settings we either raise an error or simply skip it and continue.
                    if (settings.MissingMemberHandling == MissingMemberHandling.Error) {
                        JsonHelper.ThrowPropertyNotFoundException(propertyName);
                    } else {
                        // TODO:
                        // Test this code with all different types.

                        reader.ReadNext();
                        reader.SkipCurrent();
                    }
                }
            }
        }

        private static void PopulateArray(Json json, Template template, JsonReader reader, JsonSerializerSettings settings) {
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

        private static void PopulateTrigger(Json json, Template template, JsonReader reader, JsonSerializerSettings settings) {
            // Should not get here. Not sure how triggers should be handled.
            throw new NotImplementedException();
        }

        public override int Populate(Json json, IntPtr source, int sourceSize, JsonSerializerSettings settings = null) {
            var reader = new JsonReader(source, sourceSize);

            if (settings == null)
                settings = TypedJsonSerializer.DefaultSettings;

            populatePerTemplate[(int)json.Template.TemplateTypeId](json, null, reader, settings);
            return reader.Position;
        }
    }
}