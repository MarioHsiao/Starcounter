using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using Starcounter.Templates;

namespace Starcounter.Internal {
    public abstract class TypedJsonSerializer {
        public abstract string ToJson(Obj obj);
        public abstract byte[] ToJsonUtf8(Obj obj);
        public abstract int ToJsonUtf8(Obj obj, out byte[] buffer);

        public abstract int PopulateFromJson(Obj obj, string json);
        public abstract int PopulateFromJson(Obj obj, byte[] src, int srcSize);
        public abstract int PopulateFromJson(Obj obj, IntPtr src, int srcSize);
    }

    public abstract class CodegenTypedJsonSerializer : TypedJsonSerializer {
        // These two needs to be implemented by the genererated code:
        //public abstract int ToJson(Obj obj, IntPtr buf, int bufferSize);
        //public abstract int PopulateFromJson(Obj obj, IntPtr src, int srcSize);
        
        public override string ToJson(Obj obj) {
            return Encoding.UTF8.GetString(ToJsonUtf8(obj));
        }

        public override int ToJsonUtf8(Obj obj, out byte[] apa) {
            apa = null;
            return -1;
        }

        public override byte[] ToJsonUtf8(Obj obj) {
            byte[] apa;
            int usedSize = ToJsonUtf8(obj, out apa);

            if (usedSize != -1) {
                byte[] retArr = new byte[usedSize];
                Buffer.BlockCopy(apa, 0, retArr, 0, usedSize);
                return retArr;
            }
            return null;   
        }

        public override int PopulateFromJson(Obj obj, string json) {
            unsafe {
                byte[] buffer = Encoding.UTF8.GetBytes(json);
                return PopulateFromJson(obj, buffer, buffer.Length);
            }
        }

        public override int PopulateFromJson(Obj obj, byte[] src, int srcSize) {
            unsafe {
                fixed (byte* p = src) {
                    return PopulateFromJson(obj, (IntPtr)p, srcSize);
                }
            }
        }
    }

    
}
