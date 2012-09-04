
using Starcounter.Binding;
using Starcounter.Internal;
using System;

namespace Starcounter
{
    
    public static class DbHelper
    {

        public static int StringCompare(string str1, string str2)
        {
            // TODO: Implement efficient string comparison.
            UInt32 ec;
            Int32 result;
            if (str1 == null)
            {
                throw new ArgumentNullException("str1");
            }
            if (str2 == null)
            {
                throw new ArgumentNullException("str2");
            }
            ec = sccoredb.SCCompareUTF16Strings(str1, str2, out result);
            if (ec == 0)
            {
                return result;
            }
            throw ErrorCode.ToException(ec);
        }

        public static Entity FromID(ulong oid)
        {
            Boolean br;
            UInt16 codeClassIdx;
            ulong addr;
            unsafe
            {
                br = sccoredb.Mdb_OIDToETIEx(oid, &addr, &codeClassIdx);
            }
            if (br)
            {
                if (addr != sccoredb.INVALID_RECORD_ADDR)
                {
                    return Bindings.GetTypeBinding(codeClassIdx).NewInstance(addr, oid);
                }
                return null;
            }
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
        }
    }
}
