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
        
        internal void Parse() {
            string propertyName = null;
            
            while (reader.Read()) {
                switch (reader.TokenType) {
                    case JsonToken.PropertyName:
                        propertyName = (string)reader.Value;
                        break;
                    case JsonToken.StartObject:
                        BeginObject(propertyName);
                        propertyName = null;
                        break;
                    case JsonToken.EndObject:
                        EndObject(propertyName);
                        propertyName = null;
                        break;
                    case JsonToken.StartArray:
                        BeginArray(propertyName);
                        propertyName = null;
                        break;
                    case JsonToken.EndArray:
                        EndArray(propertyName);
                        propertyName = null;
                        break;
                    case JsonToken.Boolean:
                        Property(propertyName, (bool)reader.Value);
                        propertyName = null;
                        break;
                    case JsonToken.Date:
                        Property(propertyName, reader.Value?.ToString());
                        propertyName = null;
                        break;
                    case JsonToken.Float:
                        Property(propertyName, (decimal)reader.Value);
                        propertyName = null;
                        break;
                    case JsonToken.Integer:
                        Property(propertyName, (long)reader.Value);
                        propertyName = null;
                        break;
                    case JsonToken.String:
                        Property(propertyName, (string)reader.Value);
                        propertyName = null;
                        break;
                    case JsonToken.Comment:
                        // We ignore comments. The reader is already in the correct position.
                        break;
                    case JsonToken.Null:
                        throw new NotSupportedException("Null is currently not supported in Json-by-example");
                    case JsonToken.Bytes:
                    case JsonToken.None:
                    case JsonToken.StartConstructor:
                    case JsonToken.EndConstructor:
                    case JsonToken.Raw:
                    case JsonToken.Undefined:
                        throw new NotSupportedException();
                }
            }
        }
        
        public void Dispose() {
            this.reader?.Close();
        }
    }
}
