
using System.Runtime.InteropServices;

namespace Starcounter.Server {

    internal static class KernelAPI {
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct NativeStructStarUUID {
            public fixed ulong Data[2];
        };

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public unsafe struct NativeStructImageHeader {
            public uint Magic;
            public uint Version;
            NativeStructStarUUID Identifier;
            public uint DiskSectorSize;
            public uint DatabasePageSize;
            public uint TotalPageCount;
            public uint SmallPageCount;
            public fixed byte StringFormat[32];
        };
    }
}