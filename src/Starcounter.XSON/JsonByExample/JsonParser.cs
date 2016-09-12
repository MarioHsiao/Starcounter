using System;
using System.IO;
using Newtonsoft.Json;

namespace Starcounter.XSON.JsonByExample {
    internal class JsonParser : IDisposable {
        private string json;
        protected JsonTextReader reader;

        internal JsonParser(string json) {
            this.json = json;
            this.reader = CreateReader();
        }

        private JsonTextReader CreateReader() {
            var reader = new JsonTextReader(new StringReader(this.json));
            reader.DateParseHandling = DateParseHandling.None;
            reader.FloatParseHandling = FloatParseHandling.Decimal;
            reader.CloseInput = true;
            return reader;
        }

        internal virtual void OnStartObject(string name, string dotnetName, bool isMetadata, bool isEditable) {
        }

        internal virtual void OnEndObject(string name) {
        }
        
        internal virtual void OnStartArray(string name, string dotnetName, bool isMetadata, bool isEditable) {
        }

        internal virtual void OnEndArray(string name) {
        }

        internal virtual void OnBoolean(string name, string dotnetName, bool isMetadata, bool isEditable, bool value) {
        }

        internal virtual void OnFloat(string name, string dotnetName, bool isMetadata, bool isEditable, decimal value) {
        }

        internal virtual void OnInteger(string name, string dotnetName, bool isMetadata, bool isEditable, long value) {
        }

        internal virtual void OnString(string name, string dotnetName, bool isMetadata, bool isEditable, string value) {
        }
        
        protected void Walk() {
            bool isMetadata = false;
            bool isEditable = false;
            string propertyName = null;
            string dotnetName = null;
            
            while (reader.Read()) {
                switch (reader.TokenType) {
                    case JsonToken.PropertyName:
                        propertyName = (string)reader.Value;
                        dotnetName = InspectPropertyName(propertyName, out isMetadata, out isEditable);
                        break;
                    case JsonToken.StartObject:
                        OnStartObject(propertyName, dotnetName, isMetadata, isEditable);
                        break;
                    case JsonToken.EndObject:
                        OnEndObject(propertyName);
                        propertyName = null;
                        dotnetName = null;
                        break;
                    case JsonToken.StartArray:
                        OnStartArray(propertyName, dotnetName, isMetadata, isEditable);
                        break;
                    case JsonToken.EndArray:
                        OnEndArray(propertyName);
                        propertyName = null;
                        dotnetName = null;
                        break;
                    case JsonToken.Boolean:
                        OnBoolean(propertyName, dotnetName, isMetadata, isEditable, (bool)reader.Value);
                        break;
                    case JsonToken.Date:
                        OnString(propertyName, dotnetName, isMetadata, isEditable, reader.Value?.ToString());
                        break;
                    case JsonToken.Float:
                        OnFloat(propertyName, dotnetName, isMetadata, isEditable, (decimal)reader.Value);
                        break;
                    case JsonToken.Integer:
                        OnInteger(propertyName, dotnetName, isMetadata, isEditable, (long)reader.Value);
                        break;
                    case JsonToken.String:
                        OnString(propertyName, dotnetName, isMetadata, isEditable, (string)reader.Value);
                        break;
                    case JsonToken.Comment:
                        throw new NotImplementedException();
                    case JsonToken.Null:
                        throw new NotSupportedException("Null is currently not supported in Json-by-example");
                    case JsonToken.Bytes:
                        throw new NotImplementedException();
                    case JsonToken.None:
                        throw new NotImplementedException();
                    case JsonToken.StartConstructor:
                        throw new NotImplementedException();
                    case JsonToken.EndConstructor:
                        throw new NotImplementedException();
                    case JsonToken.Raw:
                        throw new NotImplementedException();
                    case JsonToken.Undefined:
                        throw new NotImplementedException();
                }
            }
        }

        private string InspectPropertyName(string propertyName, out bool isMetadata, out bool isEditable) {
            if (string.IsNullOrEmpty(propertyName)) {
                isMetadata = false;
                isEditable = false;
                return null;
            }

            string legalName = "";
            isMetadata = propertyName.StartsWith("$");
            isEditable = propertyName.EndsWith("$");

            legalName = propertyName;
            if (isMetadata)
                legalName = legalName.Substring(1);

            if (isEditable && legalName.Length > 0)
                legalName = legalName.Substring(0, propertyName.Length - 1);

            return legalName;
        }

        public void Dispose() {
            this.reader?.Close();
        }
    }
}
