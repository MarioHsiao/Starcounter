using System;
using System.IO;
using System.Runtime.InteropServices;
using Starcounter.Templates;

#pragma warning disable CS0672

namespace Starcounter.Advanced.XSON {
    [Obsolete("This class is deprecated and will be removed soon. Please use ITypedJSonSerializer interface and classes that implements it instead.")]
    public abstract class StandardJsonSerializerBase : TypedJsonSerializer {
        public override int EstimateSizeBytes(Json json, JsonSerializerSettings settings = null) {
            string s = json.Serializer.Serialize(json, settings);
            return s.Length;
        }

        public override int EstimateSizeBytes(Json json, Template property, JsonSerializerSettings settings = null) {
            string s = json.Serializer.Serialize(json, property, settings);
            return s.Length;
        }

        public override int Serialize(Json json, IntPtr dest, int destSize, JsonSerializerSettings settings = null) {


            string s = json.Serializer.Serialize(json, settings);
            return s.Length;
        }

        public override int Serialize(Json json,
                                      Template property,
                                      IntPtr dest,
                                      int destSize,
                                      JsonSerializerSettings settings = null) {
            return -1;
        }
    }

    [Obsolete("This class is deprecated and will be removed soon. Please use ITypedJSonSerializer interface and classes that implements it instead.")]
    public class StandardJsonSerializer : StandardJsonSerializerBase {
        internal static TypedJsonSerializer Default = new StandardJsonSerializer();

        public override int Populate(Json json, IntPtr source, int sourceSize, JsonSerializerSettings settings = null) {
            byte[] tmp = new byte[sourceSize];
            Marshal.Copy(source, tmp, 0, sourceSize);
            var stream = new MemoryStream(tmp);
            json.Serializer.Deserialize(json, stream, settings);
            return (int)stream.Position;
        }
    }
}

#pragma warning restore CS0672
