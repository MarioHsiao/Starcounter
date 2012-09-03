
using Starcounter;
using Starcounter.Binding;
using Starcounter.Internal;
using System;


namespace Sc.Server.Internal
{

    public static class DbState
    {

        public static void Insert(Entity proxy, ulong typeAddr, Sc.Server.Binding.TypeBinding typeBinding)
        {
            uint e;
            ulong oid;
            ulong addr;

            unsafe
            {
                e = sccoredb.sc_insert(typeAddr, &oid, &addr);
            }
            if (e == 0)
            {
                proxy.Attach(addr, oid, typeBinding);
#if false
                try
                {
                    proxy.InvokeOnNew();
                }
                catch (Exception exception)
                {
                    if (exception is ThreadAbortException) throw;
                    throw ErrorCode.ToException(Error.SCERRERRORINHOOKCALLBACK, exception);
                }
#endif
                return;
            }
            throw sccoreerr.TranslateErrorCode(e);
        }

        public static Int16 ReadInt16(DbObject obj, Int32 index)
        {
            return (Int16)ReadInt64(obj, index);
        }

        public static Int32 ReadInt32(DbObject obj, Int32 index)
        {
            return (Int32)ReadInt64(obj, index);
        }

        public static Int64 ReadInt64(DbObject obj, Int32 index)
        {
            Int64 value;
            UInt16 flags;
            ObjectRef thisRef;
            UInt32 ec;
            thisRef = obj.ThisRef;
            unsafe
            {
                flags = sccoredb.Mdb_ObjectReadInt64(thisRef.ObjectID, thisRef.ETI, index, &value);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0)
            {
                return value;
            }
            ec = sccoredb.Mdb_GetLastError();
            throw sccoreerr.TranslateErrorCode(ec);
        }

        public static String ReadString(DbObject obj, Int32 index)
        {
            unsafe
            {
                ObjectRef thisRef;
                UInt16 flags;
                Byte* value;
                Int32 sl;
                UInt32 ec;

                thisRef = obj.ThisRef;

                flags = sccoredb.SCObjectReadStringW2(
                    thisRef.ObjectID,
                    thisRef.ETI,
                    index,
                    &value
                );

                if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0)
                {
                    if ((flags & sccoredb.Mdb_DataValueFlag_Null) == 0)
                    {
                        sl = *((Int32*)value);
                        return new String((Char*)(value + 4), 0, sl);
                    }
                    return null;
                }

                ec = sccoredb.Mdb_GetLastError();
                throw sccoreerr.TranslateErrorCode(ec);
            }
        }

        public static void WriteInt16(DbObject obj, Int32 index, Int16 value)
        {
            WriteInt64(obj, index, value);
        }

        public static void WriteInt32(DbObject obj, Int32 index, Int32 value)
        {
            WriteInt64(obj, index, value);
        }

        public static void WriteInt64(DbObject obj, Int32 index, Int64 value)
        {
            ObjectRef thisRef;
            int br;
            thisRef = obj.ThisRef;
            br = sccoredb.Mdb_ObjectWriteInt64(thisRef.ObjectID, thisRef.ETI, index, value);
            if (br != 0)
            {
                return;
            }
            throw sccoreerr.TranslateErrorCode(sccoredb.Mdb_GetLastError());
        }

        public static void WriteString(DbObject obj, Int32 index, String value)
        {
            ObjectRef thisRef;
            int br;
            thisRef = obj.ThisRef;
            unsafe
            {
                fixed (Char* p = value)
                {
                    br = sccoredb.Mdb_ObjectWriteString16(
                        thisRef.ObjectID,
                        thisRef.ETI,
                        index,
                        p
                    );
                }
            }
            if (br != 0)
            {
                return;
            }
            throw sccoreerr.TranslateErrorCode(sccoredb.Mdb_GetLastError());
        }
    }
}
