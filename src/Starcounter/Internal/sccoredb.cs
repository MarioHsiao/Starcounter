
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Starcounter.Internal
{
    
    [SuppressUnmanagedCodeSecurity]
    internal static class sccoredb
    {

        internal const ulong MDBIT_OBJECTID = 0; // TODO:

        internal const ulong INVALID_DEFINITION_ADDR = 0xFFFFFFFFFF;
        internal const ulong INVALID_RECORD_ADDR = 0xFFFFFFFFFF;


        internal const byte SC_BASETYPE_UINT64 = 0x01;
        internal const byte SC_BASETYPE_SINT64 = 0x02;
        internal const byte SC_BASETYPE_SINGLE = 0x03;
        internal const byte SC_BASETYPE_DOUBLE = 0x04;
        internal const byte SC_BASETYPE_BINARY = 0x05;
        internal const byte SC_BASETYPE_STRING = 0x06;
        internal const byte SC_BASETYPE_DECIMAL = 0x07;
        internal const byte SC_BASETYPE_OBJREF = 0x08;
        internal const byte SC_BASETYPE_LBINARY = 0x09;

        internal const byte Mdb_Type_Boolean = (0x10 | SC_BASETYPE_UINT64);
        internal const byte Mdb_Type_Byte = (0x20 | SC_BASETYPE_UINT64);
        internal const byte Mdb_Type_UInt16 = (0x30 | SC_BASETYPE_UINT64);
        internal const byte Mdb_Type_UInt32 = (0x40 | SC_BASETYPE_UINT64);
        internal const byte Mdb_Type_UInt64 = (0x50 | SC_BASETYPE_UINT64);
        internal const byte Mdb_Type_DateTime = (0x60 | SC_BASETYPE_UINT64);
        internal const byte Mdb_Type_TimeSpan = (0x70 | SC_BASETYPE_UINT64);
        internal const byte Mdb_Type_SByte = (0x10 | SC_BASETYPE_SINT64);
        internal const byte Mdb_Type_Int16 = (0x20 | SC_BASETYPE_SINT64);
        internal const byte Mdb_Type_Int32 = (0x30 | SC_BASETYPE_SINT64);
        internal const byte Mdb_Type_Int64 = (0x40 | SC_BASETYPE_SINT64);
        internal const byte Mdb_Type_Single = (0x10 | SC_BASETYPE_SINGLE);
        internal const byte Mdb_Type_Double = (0x10 | SC_BASETYPE_DOUBLE);
        internal const byte Mdb_Type_Binary = (0x10 | SC_BASETYPE_BINARY);
        internal const byte Mdb_Type_String = (0x10 | SC_BASETYPE_STRING);
        internal const byte Mdb_Type_Decimal = (0x10 | SC_BASETYPE_DECIMAL);
        internal const byte Mdb_Type_ObjectID = (0x10 | SC_BASETYPE_OBJREF);
        internal const byte Mdb_Type_LargeBinary = (0x10 | SC_BASETYPE_LBINARY);


        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static uint Mdb_GetLastError();

    
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern uint SCConfigSetValue(string key, string value);

        internal const uint SCCOREDB_LOAD_DATABASE = 0x00100000;

        internal const uint SCCOREDB_COMPLETE_INIT = 0x00200000;

        internal const uint SCCOREDB_ENABLE_CHECK_FILE_ON_LOAD = 0x00010000;

        internal const uint SCCOREDB_ENABLE_CHECK_FILE_ON_CHECKP = 0x00020000;

        internal const uint SCCOREDB_ENABLE_CHECK_FILE_ON_BACKUP = 0x00040000;

        internal const uint SCCOREDB_ENABLE_CHECK_MEMORY_ON_CHECKP = 0x00080000;

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint sccoredb_connect(uint flags, void* hsched, ulong hmenv, ulong hlogs, int* pempty);

        internal const uint SCCOREDB_UNLOAD_DATABASE = 0x00200000;

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint sccoredb_disconnect(uint flags);

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint SCAttachThread(byte scheduler_number, int init);

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint SCDetachThread(uint yield_reason);

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint SCResetThread();

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint SCConfigureVP();

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint SCBackgroundTask();

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint sccoredb_advance_clock(uint scheduler_index);

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint SCIdleTask(int* pCallAgainIfStillIdle);

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint SCLowMemoryAlert(uint lr);


        internal const ushort MDB_ATTRFLAG_NULLABLE = 0x0040;
        
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal unsafe struct Mdb_DefinitionInfo
        {
            internal char* PtrCodeClassName;
            internal int NumAttributes;
            internal ulong ETIBasedOn;
            internal ushort TableID;
            internal ushort Flags;
        }

        [StructLayout(LayoutKind.Sequential, Pack=8)]
        internal unsafe struct Mdb_AttributeInfo
        {
            internal ushort Flags;
            internal char* PtrName;
            internal ushort Index;
            internal byte Type;
            internal ulong RefDef;
        }

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        internal extern static int Mdb_DefinitionFromCodeClassString(
            string name,
            out ulong etiDefinition
            );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static int Mdb_DefinitionToDefinitionInfo(
            UInt64 etiDefinition,
            out Mdb_DefinitionInfo definitionInfo
            );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static int Mdb_DefinitionAttributeIndexToInfo(
            ulong etiDefinition,
            ushort index,
            out Mdb_AttributeInfo attributeInfo
            );
        
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal unsafe struct SC_COLUMN_DEFINITION
        {
            internal byte type;
            internal byte is_nullable;
            internal byte* name;
        }

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint sc_create_table(
            byte *name,
            ulong base_definition_addr,
            SC_COLUMN_DEFINITION *column_definitions
            );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        internal static extern uint sc_rename_table(
            ushort table_id,
            string new_name
            );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint sc_drop_table(ushort table_id);


        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static UInt32 SCSchemaGetIndexes(
            UInt64 definitionAddr,
            UInt32* pic,
            SC_INDEX_INFO* piis
        );

        [StructLayout(LayoutKind.Sequential, Pack=8)]
        internal unsafe struct SC_INDEX_INFO
        {
            internal UInt64 handle;
            internal UInt64 definitionAddr;
            internal Char* name;
            internal Int16 attributeCount;
            internal UInt16 sortMask;
            internal Int16 attrIndexArr_0;
            internal Int16 attrIndexArr_1;
            internal Int16 attrIndexArr_2;
            internal Int16 attrIndexArr_3;
            internal Int16 attrIndexArr_4;
            internal Int16 attrIndexArr_5;
            internal Int16 attrIndexArr_6;
            internal Int16 attrIndexArr_7;
            internal Int16 attrIndexArr_8;
            internal Int16 attrIndexArr_9;
            internal Int16 attrIndexArr_10;
        };

        internal const UInt32 SC_INDEXCREATE_UNIQUE_CONSTRAINT = 0x00000001;

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        internal extern unsafe static UInt32 sc_create_index(
            ulong definition_addr,
            string name,
            ushort sort_mask,
            short* column_indexes,
            uint flags
            );

        
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static uint sccoredb_create_transaction_and_set_current(
            uint flags,
            out ulong transaction_id,
            out ulong handle,
            out ulong verify
            );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static uint sccoredb_free_transaction(
            ulong handle,
            ulong verify
            );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static uint sccoredb_begin_commit(
            out ulong hiter,
            out ulong viter
            );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static uint sccoredb_complete_commit(
            int detach_and_free,
            out ulong new_transaction_id
            );

        
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static UInt32 sc_insert(
            ulong definition_addr,
            ulong* pnew_oid,
            ulong* pnew_addr
            );


        internal const ushort Mdb_DataValueFlag_Null = 0x0001;

        internal const ushort Mdb_DataValueFlag_Transactional = 0x0002;

        internal const ushort Mdb_DataValueFlag_Error = 0x1000;

        internal const ushort Mdb_DataValueFlag_WouldBlock = 0x2000;

        internal const ushort Mdb_DataValueFlag_DeletedVersion = 0x0010;

        internal const ushort Mdb_DataValueFlag_DeletedPublic = 0x0020;

        internal const ushort Mdb_DataValueFlag_Exceptional = 0x1030; // (Mdb_DataValueFlag_Error | Mdb_DataValueFlag_DeletedVersion | Mdb_DataValueFlag_DeletedPublic);

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static UInt16 Mdb_ObjectReadBinary(
            UInt64 objectOID,
            UInt64 objectETI,
            Int32 index,
            Byte** ppReturnValue
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static UInt16 Mdb_ObjectReadBool2(
            UInt64 objectOID,
            UInt64 objectETI,
            Int32 index,
            Byte* pReturnValue
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static UInt16 SCObjectReadDecimal2(
            UInt64 objectOID,
            UInt64 objectETI,
            Int32 index,
            Int32** ppArray4
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static UInt16 Mdb_ObjectReadDouble(
            UInt64 objectOID,
            UInt64 objectETI,
            Int32 index,
            Double* pReturnValue
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static UInt16 Mdb_ObjectReadInt64(
            UInt64 objectOID,
            UInt64 objectETI,
            Int32 index,
            Int64* pReturnValue
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static UInt16 SCObjectReadLargeBinary(
            UInt64 objectOID,
            UInt64 objectETI,
            Int32 index,
            Byte** ppReturnValue
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static void Mdb_ObjectReadObjRef(
            UInt64 objectOID,
            UInt64 objectETI,
            Int32 index,
            UInt64* pReturnOID,
            UInt64* pReturnETI,
            UInt16* pClassIndex,
            UInt16* pFlags
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static UInt16 Mdb_ObjectReadSingle(
            UInt64 objectOID,
            UInt64 objectETI,
            Int32 index,
            Single* pReturnValue
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static UInt16 SCObjectReadStringW2(
            UInt64 objectOID,
            UInt64 objectETI,
            Int32 index,
            Byte** pReturnValue
        );
        
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static UInt16 Mdb_ObjectReadUInt64(
            UInt64 objectOID,
            UInt64 objectETI,
            Int32 index,
            UInt64* pReturnValue
        );
        
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static UInt32 Mdb_ObjectWriteBinary(
            UInt64 objectOID,
            UInt64 objectETI,
            Int32 index,
            Byte[] value
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static Boolean Mdb_ObjectWriteBool2(
            UInt64 objectOID,
            UInt64 objectETI,
            Int32 index,
            Byte value
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static Boolean Mdb_ObjectWriteDecimal(
            UInt64 objectOID,
            UInt64 objectETI,
            Int32 index,
            Int32 low,
            Int32 mid,
            Int32 high,
            Int32 scale_sign
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static Boolean Mdb_ObjectWriteDouble(
            UInt64 objectOID,
            UInt64 objectETI,
            Int32 index,
            Double value
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static Boolean Mdb_ObjectWriteInt64(
            UInt64 objectOID,
            UInt64 objectETI,
            Int32 index,
            Int64 value
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static UInt32 SCObjectWriteLargeBinary(
            UInt64 objectOID,
            UInt64 objectETI,
            Int32 index,
            Byte[] value
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static Boolean Mdb_ObjectWriteObjRef(
            UInt64 objectOID,
            UInt64 objectETI,
            Int32 index,
            UInt64 valueOID,
            UInt64 valueETI
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static Boolean Mdb_ObjectWriteSingle(
            UInt64 objectOID,
            UInt64 objectETI,
            Int32 index,
            Single value
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static Boolean Mdb_ObjectWriteAttributeState(
            UInt64 objectOID,
            UInt64 objectETI,
            Int32 index,
            UInt16 value
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static Boolean Mdb_ObjectWriteString16(
            UInt64 objectOID,
            UInt64 objectETI,
            Int32 index,
            Char* pValue
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static Boolean Mdb_ObjectWriteUInt64(
            UInt64 objectOID,
            UInt64 objectETI,
            Int32 index,
            UInt64 value
        );

        internal const UInt32 SC_ITERATOR_RANGE_VALID_LSKEY = 0x00000001;

        internal const UInt32 SC_ITERATOR_RANGE_INCLUDE_LSKEY = 0x00000010;

        internal const UInt32 SC_ITERATOR_RANGE_VALID_GRKEY = 0x00000002;

        internal const UInt32 SC_ITERATOR_RANGE_INCLUDE_GRKEY = 0x00000020;

        internal const UInt32 SC_ITERATOR_SORTED_DESCENDING = 0x00080000;

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static UInt32 SCIteratorCreate(
            UInt64 hIndex,
            UInt32 flags,
            Byte* lesserKey,
            Byte* greaterKey,
            UInt64* ph,
            UInt64* pv
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static UInt32 SCIteratorCreate2(
            UInt64 hIndex,
            UInt32 flags,
            Byte* lesserKey,
            Byte* greaterKey,
            UInt64 hfilter,
            IntPtr varstr,
            UInt64* ph,
            UInt64* pv
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static UInt32 SCIteratorNext(
            UInt64 h,
            UInt64 v,
            UInt64* pObjectOID,
            UInt64* pObjectETI,
            UInt16* pClassIndex,
            UInt64* pData
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static UInt32 SCIteratorFillUp(
            UInt64 h,
            UInt64 v,
            Byte* results,
            UInt32 resultsMaxBytes,
            UInt32* resultsNum,
            UInt32* flags);

        //
        // The iterator is freed if no errors (and only if no errors).
        //
        // If no error and *precreate_key == NULL then no key was generated because the
        // iterator was positioned after the end of the range. The iterator will still
        // have been freed.
        //
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static UInt32 sc_get_recreate_key_and_free_iterator(
            UInt64 h,
            UInt64 v,
            UInt32 flags,
            Byte** precreate_key
            );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static UInt32 sc_get_index_position_key(
            UInt64 index_addr,
            UInt64 record_id,
            UInt64 record_addr,
            Byte** precreate_key
            );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static UInt32 sc_recreate_iterator(
            UInt64 hindex,
            UInt32 flags,
            Byte* recreate_key,
            Byte* last_key,
            UInt64* ph,
            UInt64* pv
            );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static UInt32 sc_recreate_iterator_with_filter(
            UInt64 hindex,
            UInt32 flags,
            Byte* recreate_key,
            Byte* last_key,
            UInt64 hfilter,
            Byte* varstr,
            UInt64* ph,
            UInt64* pv
            );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static UInt32 sccoredb_iterator_get_local_time(
            UInt64 iter_handle,
            UInt64 iter_verify,
            UInt32* plocal_time
            );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static UInt32 SCIteratorFree(
            UInt64 h,
            UInt64 v
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern unsafe UInt32 SCConvertUTF16StringToNative(
            String input,
            UInt32 flags,
            Byte** output
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern unsafe UInt32 SCConvertUTF16StringToNative2(
            String input,
            UInt32 flags,
            Byte* output,
            UInt32 outlen);

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern unsafe UInt32 SCConvertNativeStringToUTF16(
            Byte* inBuf,
            UInt32 inlen,
            Char* output,
            UInt32* poutlen);

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe Int32 SCCompareNativeStrings(
            /* const */ Byte* str1,
            /* const */ Byte* str2
            );
   
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal extern static UInt32 SCCompareUTF16Strings(
            String str1,
            String str2,
            out Int32 result
            );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static Boolean Mdb_OIDToETIEx(
            UInt64 objID,
            UInt64* pEtiPubl,
            UInt16* pCodeClassIndex
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static int Mdb_ObjectIssueDelete(
            UInt64 objectOID,
            UInt64 objectETI
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static int Mdb_ObjectDelete(
            UInt64 objectOID,
            UInt64 objectETI,
            int execute
        );
    }

    [SuppressUnmanagedCodeSecurity]
    internal static class NewCodeGen
    {
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern Int32 NewCodeGen_LoadGenCodeLibrary(UInt64 queryID, String pathToGenLibrary);

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe Int32 NewCodeGen_InitEnumerator(UInt64 queryID, Byte* queryParameters);

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe Int32 NewCodeGen_MoveNext(UInt64 queryID, UInt64* oid, UInt64* eti, UInt16* currentCCI);

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern Int32 NewCodeGen_Reset(UInt64 queryID);
    }
    
    [SuppressUnmanagedCodeSecurity]
    internal static class CodeGenFilterNativeInterface
    {

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe UInt32 SCCreateFilter(
            UInt64 definitionAddr,
            UInt32 stackSize,
            UInt32 varCount,
            UInt32 instrCount,
            UInt32* instrstr,
            UInt64* ph
        );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern UInt32 SCReleaseFilter(UInt64 h);
    }

    internal unsafe delegate UInt32 SqlConn_GetQueryUniqueId_Type(
        Char* query,
        UInt64* uniqueQueryId,
        UInt32* flags);

    internal unsafe delegate UInt32 SqlConn_GetResults_Type(
        UInt64 uniqueQueryId,
        Byte* queryParams,
        Byte* results,
        UInt32 resultsMaxBytes,
        UInt32* resultsNum,
        Byte* recreationKey,
        UInt32 recreationKeyMaxBytes,
        UInt32* flags);

    internal unsafe delegate UInt32 SqlConn_GetInfo_Type(
        Byte infoType,
        UInt64 param,
        Byte* result,
        UInt32 maxBytes,
        UInt32* outLenBytes);
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct SC_SQL_CALLBACKS
    {
        internal SqlConn_GetQueryUniqueId_Type pSqlConn_GetQueryUniqueId;
        internal SqlConn_GetResults_Type pSqlConn_GetResults;
        internal SqlConn_GetInfo_Type pSqlConn_GetInfo;
    }

    [SuppressUnmanagedCodeSecurity]
    internal static class SqlConnectivityInterface
    {
        // Connectivity constants.
        internal const UInt32 RECREATION_KEY_MAX_BYTES = 4096; // Maximum length in bytes of recreation key.
        internal const UInt32 MAX_HITS_PER_PAGE_SORTING = 8196; // Maximum amount of hits per page in case of sorting.
        internal const UInt32 MAX_STATUS_STRING_LEN = 8196; // Maximum length of the status string.

        // SQL query flags.
        internal const UInt32 FLAG_MORE_RESULTS = 1;
        internal const UInt32 FLAG_HAS_PROJECTION = 2;
        internal const UInt32 FLAG_HAS_SORTING = 4;
        internal const UInt32 FLAG_HAS_AGGREGATION = 8;
        internal const UInt32 FLAG_FETCH_VARIABLE = 16;
        internal const UInt32 FLAG_RECREATION_KEY_VARIABLE = 32;
        internal const UInt32 FLAG_POST_MANAGED_FILTER = 64;
        internal const UInt32 FLAG_LAST_FETCH = 128;

        // Flags that are used to get SQL status information.
        internal const Byte GET_LAST_ERROR = 0;
        internal const Byte GET_ENUMERATOR_EXEC_PLAN = 1;
        internal const Byte GET_QUERY_CACHE_STATUS = 2;
        internal const Byte GET_FETCH_VARIABLE = 3;
        internal const Byte GET_RECREATION_KEY_VARIABLE = 4;
        internal const Byte PRINT_PROFILER_RESULTS = 5;

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern UInt32 SqlConn_InitManagedFunctions(ref SC_SQL_CALLBACKS managedSqlFunctions);

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern unsafe UInt32 SqlConn_GetQueryUniqueId(
            String query, // [IN] SQL query string.
            UInt64* uniqueQueryId, // [OUT] Unique query ID, Fixed-size 8 bytes.
            UInt32* flags // [OUT] Populated query flags, Fixed-size 4 bytes.
            );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe UInt32 SqlConn_GetResults(
            UInt64 uniqueQueryId, // [IN] Uniquely identifies the SQL query on server.
            Byte* queryParams, // [IN] Buffer containing SQL query parameters (total length in bytes in first 4 bytes).
            Byte* results, // [OUT] Buffer that should contain query execution results.
            UInt32 resultsMaxBytes, // [IN] Maximum size in bytes of the buffer (needed for allocation in Blast).
            UInt32* resultsNum, // [IN] Number of hits to retrieve. [OUT] Number of fetched hits. Fixed-size 4 bytes data.
            Byte* recreationKey, // [IN] Given key to recreate enumerator. [OUT] Serialized recreation key for further use. Total length in bytes in first 4 bytes.
            UInt32 recreationKeyMaxBytes, // [IN] Maximum size in bytes of the buffer (needed for allocation in Blast).
            UInt32* flags // [IN] SQL flags that are needed to pass. [OUT] Contains returned SQL flags. Fixed-size 4 bytes data.
            );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe UInt32 SqlConn_GetInfo(
            Byte infoType, // [IN] Needed SQL information type.
            UInt64 param, // [IN] Additional parameter (e.g. SQL unique query ID).
            Byte* results, // [OUT] Obtained information.
            UInt32 maxBytes, // [IN] Maximum size in bytes of the result buffer (needed for allocation in Blast).
            UInt32* outLenBytes // [OUT] Length in bytes of the result data.
            );

        // Types of variable in query.
        internal const Byte QUERY_VARTYPE_DEFINED = 1;
        internal const Byte QUERY_VARTYPE_INT = 2;
        internal const Byte QUERY_VARTYPE_UINT = 3;
        internal const Byte QUERY_VARTYPE_DOUBLE = 4;
        internal const Byte QUERY_VARTYPE_DECIMAL = 5;
        internal const Byte QUERY_VARTYPE_STRING = 6;
        internal const Byte QUERY_VARTYPE_OBJECT = 7;
        internal const Byte QUERY_VARTYPE_BINARY = 8;
        internal const Byte QUERY_VARTYPE_DATETIME = 9;
        internal const Byte QUERY_VARTYPE_BOOLEAN = 10;
    }
}
