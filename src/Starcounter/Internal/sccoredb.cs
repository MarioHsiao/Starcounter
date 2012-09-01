
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Starcounter.Internal
{
    
    [SuppressUnmanagedCodeSecurity]
    internal static class sccoredb
    {

        internal const ulong INVALID_DEFINITION_ADDR = 0xFFFFFFFFFF;

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
        internal unsafe extern static ushort Mdb_ObjectReadInt64(
            ulong objectOID,
            ulong objectETI,
            int index,
            long* pReturnValue
            );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static ushort SCObjectReadStringW2(
            ulong objectOID,
            ulong objectETI,
            int index,
            byte** pReturnValue
            );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal extern static int Mdb_ObjectWriteInt64(
            ulong objectOID,
            ulong objectETI,
            int index,
            long value
            );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal unsafe extern static int Mdb_ObjectWriteString16(
            ulong objectOID,
            ulong objectETI,
            int index,
            char* pValue
            );
    }
}
