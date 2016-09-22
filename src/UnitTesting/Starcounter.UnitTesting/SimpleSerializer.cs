
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Starcounter.UnitTesting
{
    /// <summary>
    /// Simple as possible, but ok for our needs at this point.
    /// </summary>
    internal static class SimpleSerializer
    {
        public static byte[] SerializeToByteArray<T>(T target)
        {
            var f = new BinaryFormatter();
            var bytes = new MemoryStream();

            f.Serialize(bytes, target);

            return bytes.ToArray();
        }

        public static T DeserializeFromByteArray<T>(byte[] content)
        {
            var f = new BinaryFormatter();
            return (T)f.Deserialize(new MemoryStream(content));
        }
    }
}