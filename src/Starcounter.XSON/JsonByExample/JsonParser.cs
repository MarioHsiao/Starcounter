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

        protected virtual void BeginObject(string name) {}
        protected virtual void EndObject(string name) {}
        protected virtual void BeginArray(string name) {}
        protected virtual void EndArray(string name) {}
        protected virtual void Property(string name, bool value) {}
        protected virtual void Property(string name, decimal value) {}
        protected virtual void Property(string name, long value) {}
        protected virtual void Property(string name, string value) {}
        protected virtual void Null(string name) {}
        
        internal void Parse() {
            string propertyName = null;
            bool resetPropertyName;
            while (reader.Read()) {
                resetPropertyName = true;
                switch (reader.TokenType) {
                    case JsonToken.PropertyName:
                        propertyName = (string)reader.Value;
                        resetPropertyName = false;
                        break;
                    case JsonToken.StartObject:
                        BeginObject(propertyName);
                        break;
                    case JsonToken.EndObject:
                        EndObject(propertyName);
                        break;
                    case JsonToken.StartArray:
                        BeginArray(propertyName);
                        break;
                    case JsonToken.EndArray:
                        EndArray(propertyName);
                        break;
                    case JsonToken.Boolean:
                        Property(propertyName, (bool)reader.Value);
                        break;
                    case JsonToken.Date:
                        Property(propertyName, reader.Value?.ToString());
                        break;
                    case JsonToken.Float:
                        Property(propertyName, (decimal)reader.Value);
                        break;
                    case JsonToken.Integer:
                        Property(propertyName, (long)reader.Value);
                        break;
                    case JsonToken.String:
                        Property(propertyName, (string)reader.Value);
                        break;
                    case JsonToken.Comment:
                        // We ignore comments. The reader is already in the correct position.
                        resetPropertyName = false;
                        break;
                    case JsonToken.Null:
                        Null(propertyName);
                        break;
                    case JsonToken.Bytes:
                    case JsonToken.None:
                    case JsonToken.StartConstructor:
                    case JsonToken.EndConstructor:
                    case JsonToken.Raw:
                    case JsonToken.Undefined:
                        throw new NotSupportedException();
                }

                if (resetPropertyName)
                    propertyName = null;
            }
        }
        
        public void Dispose() {
            this.reader?.Close();
        }
    }
}
