using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace GenerateMetadataClasses.Deserializer {
    /// <summary>
    /// Primitive wrapper over .NET supported JSON deserialization, just the
    /// minimum of what we need for our task.
    /// </summary>
    public static class JsonDeserializer {
        public static T JsonDeserialize<T>(string jsonString) {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
            T obj = (T)ser.ReadObject(ms);
            return obj;
        }
    }
}