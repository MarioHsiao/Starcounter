using System;
using System.Diagnostics;

namespace Starcounter.Internal {
    public static class MemcpyUtil {
        internal unsafe static void Memcpy16fwd(byte* dest, byte* src, uint len) {
            if (len >= 16) {
                do {
                    *(long*)dest = *(long*)src;
                    *(long*)(dest + 8) = *(long*)(src + 8);
                    dest += 16;
                    src += 16;
                }
                while ((len -= 16) >= 16);
            }
            if (len > 0) {
                if ((len & 8) != 0) {
                    *(long*)dest = *(long*)src;
                    dest += 8;
                    src += 8;
                }
                if ((len & 4) != 0) {
                    *(int*)dest = *(int*)src;
                    dest += 4;
                    src += 4;
                }
                if ((len & 2) != 0) {
                    *(short*)dest = *(short*)src;
                    dest += 2;
                    src += 2;
                }
                if ((len & 1) != 0) {
                    byte* expr_75 = dest;
                    dest = expr_75 + 1;
                    byte* expr_7C = src;
                    src = expr_7C + 1;
                    *expr_75 = *expr_7C;
                }
            }
        }

        internal unsafe static void Memcpy16rwd(byte* dest, byte* src, uint len) {
            byte* destEnd = dest + len;
            byte* srcEnd = src + len;
            if (len >= 16) {
                do {
                    destEnd -= 16;
                    srcEnd -= 16;
                    *(long*)(destEnd + 8) = *(long*)(srcEnd + 8);
                    *(long*)destEnd = *(long*)srcEnd;
                }
                while ((len -= 16) >= 16);
            }
            if (len > 0) {
                if ((len & 8) != 0) {
                    destEnd -= 8;
                    srcEnd -= 8;
                    *(long*)destEnd = *(long*)srcEnd;
                }
                if ((len & 4) != 0) {
                    destEnd -= 4;
                    srcEnd -= 4;
                    *(int*)destEnd = *(int*)srcEnd;
                }
                if ((len & 2) != 0) {
                    destEnd -= 2;
                    srcEnd -= 2;
                    *(short*)destEnd = *(short*)srcEnd;
                }
                if ((len & 1) != 0) {
                    destEnd -= 1;
                    srcEnd -= 1;
                    *destEnd = *srcEnd;
                }
            }
            Debug.Assert(dest == destEnd);
            Debug.Assert(src == srcEnd);
        }

    }
}
