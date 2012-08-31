
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


        [StructLayout(LayoutKind.Sequential, Pack=8)]
        internal unsafe struct Mdb_DefinitionInfo
        {
            internal char* PtrCodeClassName;
            internal int NumAttributes;
            internal ulong ETIBasedOn;
            internal ushort TableID;
            internal ushort Flags;
        }

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        internal extern static int Mdb_DefinitionFromCodeClassString(
            string name,
            out ulong etiDefinition
            );

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal extern static int Mdb_DefinitionToDefinitionInfo(
            UInt64 etiDefinition,
            out Mdb_DefinitionInfo definitionInfo
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
    }
}
