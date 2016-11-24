
using System.IO;
using Starcounter.Advanced.XSON;
using Starcounter.Templates;

namespace Starcounter.XSON.Interfaces {
    public interface ITypedJsonSerializer {
        string Serialize(Json json, JsonSerializerSettings settings = null);
        void Serialize(Json json, Stream stream, JsonSerializerSettings settings = null);
        void Serialize(Json json, TextWriter textWriter, JsonSerializerSettings settings = null);

        string Serialize(Json json, Template template, JsonSerializerSettings settings = null);
        void Serialize(Json json, Template template, Stream stream, JsonSerializerSettings settings = null);
        void Serialize(Json json, Template template, TextWriter textWriter, JsonSerializerSettings settings = null);

        T Deserialize<T>(string source, JsonSerializerSettings settings = null) where T : Json, new();
        T Deserialize<T>(Stream stream, JsonSerializerSettings settings = null) where T : Json, new();
        T Deserialize<T>(TextReader reader, JsonSerializerSettings settings = null) where T : Json, new();
        void Deserialize(Json json, string source, JsonSerializerSettings settings = null);
        void Deserialize(Json json, Stream stream, JsonSerializerSettings settings = null);
        void Deserialize(Json json, TextReader reader, JsonSerializerSettings settings = null);
    }
}