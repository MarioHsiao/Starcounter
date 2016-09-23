
using System;

namespace Starcounter.UnitTesting
{
    /// <summary>
    /// A request to run (or just load) a set of assemblies containing
    /// tests.
    /// </summary>
    [Serializable]
    public class TestRequest
    {
        public string Application { get; set; }

        public byte[] ToBytes()
        {
            return SimpleSerializer.SerializeToByteArray(this);
        }

        public static TestRequest FromBytes(byte[] bytes)
        {
            return SimpleSerializer.DeserializeFromByteArray<TestRequest>(bytes);
        }
    }
}