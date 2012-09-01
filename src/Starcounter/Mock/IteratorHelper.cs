using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;

using Starcounter;
//using Starcounter.Query.Sql;
using Sc.Server.Binding;
using Sc.Server.Internal;
//using Starcounter.Query.Execution;
//using Sc.Query.Execution;
using Starcounter.Internal;

namespace Starcounter
{
    public class IteratorHelper
    {
        private UInt64 filterHandle; // Handle for generated filter.
        private Byte[] dataStream; // Supplied data stream for filter.
        private readonly UInt64 indexHandle; // Index handle.

        // Adding new filter for code generation scan.
        public void AddGeneratedFilter(UInt64 newFilterHandle)
        {
            filterHandle = newFilterHandle;
        }

        // Adding new data stream for the filter.
        public void AddDataStream(Byte[] newDataStream)
        {
            dataStream = newDataStream;
        }

        // Method to support combined indexes.
        public static IteratorHelper GetIndex(UInt64 handle)
        {
            return new IteratorHelper(handle);
        }

        // Constructor.
        public IteratorHelper(UInt64 handle)
        {
            indexHandle = handle;
            filterHandle = 0;
            dataStream = null;
        }

        // Empty recreation key length.
        public const Int32 RK_EMPTY_LEN = 4;

        // Offset in bytes for number of enumerators.
        public const Int32 RK_ENUM_NUM_OFFSET = 4;

        // Offset in bytes for dynamic data.
        public const Int32 RK_FIRST_DYN_DATA_OFFSET = 8;

        // Length of recreation key header in bytes.
        public const Int32 RK_HEADER_LEN = 12;

        // Gets the information about saved object in the iterator.
        public unsafe void RecreateEnumerator_GetObjectInfo(
            Byte* keyData,
            Int32 extentNumber,
            out UInt64 keyOid,
            out UInt64 keyEti)
        {
            // Position of enumerator static data.
            Byte* staticDataOffset = keyData + (extentNumber << 3) + RK_HEADER_LEN;

            // Dynamic data.
            Byte* recreationKey = keyData + (*(UInt32*)(staticDataOffset + 4));

            // Fetching OID and ETI.
            UInt32 keyLen = (*(UInt32*)recreationKey);
            keyOid = *(UInt64*)(recreationKey + keyLen - 12);
            keyEti = *(UInt64*)(recreationKey + keyLen - 20);
        }

        // No code generation is used here.
        public void GetEnumeratorCached_NoCodeGenFilter<T>(
            UInt32 rangeFlags,
            Byte[] firstKey,
            Byte[] lastKey,
            Enumerator<T> cachedEnum) where T : Entity
        {
            UInt32 err;
            UInt64 hCursor, verify;

            // Printing range keys.
            //SqlDebugHelper.PrintByteBuffer("First key", firstKey, true);
            //SqlDebugHelper.PrintByteBuffer("Last key", lastKey, true);

            // Address should be the same for treating keys equal.
            if (firstKey == lastKey)
            {
                unsafe
                {
                    fixed (byte* sameKey = firstKey)
                    {
                        err = sccoredb.SCIteratorCreate(
                            indexHandle,
                            rangeFlags,
                            sameKey,
                            sameKey,
                            &hCursor,
                            &verify
                        );
                    }
                }
            }
            else
            {
                unsafe
                {
                    fixed (byte* fk = firstKey, lk = lastKey)
                    {
                        err = sccoredb.SCIteratorCreate(
                            indexHandle,
                            rangeFlags,
                            fk,
                            lk,
                            &hCursor,
                            &verify
                        );
                    }
                }
            }

            // Checking error code.
            if (err == 0)
            {
                cachedEnum.UpdateCached(hCursor, verify);
                return;
            }

            throw ErrorCode.ToException(err);
        }

        // No code generation filter is used here.
        public unsafe Boolean RecreateEnumerator_NoCodeGenFilter<T>(
            Byte* keyData,
            Int32 extentNumber,
            Enumerator<T> cachedEnum) where T : Entity
        {
            // Position of enumerator static data.
            Byte* staticDataOffset = keyData + (extentNumber << 3) + RK_HEADER_LEN;

            // Checking if its possible to recreate the key (dynamic data offset).
            UInt32 dynDataOffset = (*(UInt32*)(staticDataOffset + 4));
            if (dynDataOffset == 0)
                return false;

            UInt32 err;
            UInt64 hCursor, verify;

            Byte* staticData = keyData + (*(UInt32*)(staticDataOffset));
            UInt32 flags = *((UInt32*)staticData);
            Byte* lastKey = staticData + 4; // Skipping flags.

            // Dynamic data.
            Byte* recreationKey = keyData + dynDataOffset;

            // Recreating iterator using obtained data.
            //SqlDebugHelper.PrintByteBuffer("IndexScan Using Recreation Key", recreationKey, true);
            //Application.Profiler.Start("sc_recreate_iterator", 7);
            err = sccoredb.sc_recreate_iterator(
                indexHandle,
                flags,
                recreationKey,
                lastKey,
                &hCursor,
                &verify
            );
            //Application.Profiler.Stop(7);

            // Checking error code.
            if (err == 0)
            {
                cachedEnum.UpdateCached(hCursor, verify);
                return true;
            }
            throw ErrorCode.ToException(err);
        }

        // Scan which uses code generation.
        public void GetEnumeratorCached_CodeGenFilter<T>(
            UInt32 rangeFlags,
            Byte[] firstKey,
            Byte[] secondKey,
            Enumerator<T> cachedEnum) where T : Entity
        {
            UInt32 err;
            UInt64 hCursor, verify;

            unsafe
            {
                fixed (byte* vs = dataStream, fk = firstKey, sk = secondKey)
                {
                    // Recreating enumerator using key.
                    err = sccoredb.SCIteratorCreate2(
                        indexHandle,
                        rangeFlags,
                        fk,
                        sk,
                        filterHandle,
                        (IntPtr)vs,
                        &hCursor,
                        &verify
                    );
                }
            }

            // Checking error code.
            if (err == 0)
            {
                cachedEnum.UpdateCached(hCursor, verify);
                return;
            }

            throw ErrorCode.ToException(err);
        }

        // Code generation filter is used here.
        public unsafe Boolean RecreateEnumerator_CodeGenFilter<T>(
            Byte* keyData,
            Int32 extentNumber,
            Enumerator<T> cachedEnum) where T : Entity
        {
            // Position of enumerator static data.
            Byte* staticDataOffset = keyData + (extentNumber << 3) + RK_HEADER_LEN;

            // Checking if its possible to recreate the key (dynamic data offset).
            UInt32 dynDataOffset = (*(UInt32*)(staticDataOffset + 4));
            if (dynDataOffset == 0)
                return false;

            UInt64 hCursor, verify;
            UInt32 err;

            Byte* staticData = keyData + (*(UInt32*)(staticDataOffset));
            UInt32 flags = *((UInt32*)staticData);
            UInt64 filterHandle = *((UInt64*)(staticData + 4));
            Byte* varStream = staticData + 12;
            Byte* lastKey = *((UInt32*)varStream) + varStream;

            // Dynamic data.
            Byte* recreationKey = keyData + dynDataOffset;
            //SqlDebugHelper.PrintByteBuffer("FullTableScan Using Recreation Key", recreationKey, true);

            // Recreating iterator using obtained data.
            err = sccoredb.sc_recreate_iterator_with_filter(
                indexHandle,
                flags,
                recreationKey,
                lastKey,
                filterHandle,
                varStream,
                &hCursor,
                &verify
            );

            // Checking error code.
            if (err == 0)
            {
                cachedEnum.UpdateCached(hCursor, verify);
                return true;
            }

            throw ErrorCode.ToException(err);
        }
    }
}
