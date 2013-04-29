// ***********************************************************************
// <copyright file="IteratorHelper.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;

using Starcounter;
//using Starcounter.Query.Sql;
//using Starcounter.Query.Execution;
//using Sc.Query.Execution;
using Starcounter.Internal;

namespace Starcounter
{
    /// <summary>
    /// Class IteratorHelper
    /// </summary>
    public class IteratorHelper
    {
        private UInt64 filterHandle; // Handle for generated filter.
        private Byte[] dataStream; // Supplied data stream for filter.
        private readonly UInt64 indexHandle; // Index handle.

        // Adding new filter for code generation scan.
        /// <summary>
        /// Adds the generated filter.
        /// </summary>
        /// <param name="newFilterHandle">The new filter handle.</param>
        public void AddGeneratedFilter(UInt64 newFilterHandle)
        {
            filterHandle = newFilterHandle;
        }

        // Adding new data stream for the filter.
        /// <summary>
        /// Adds the data stream.
        /// </summary>
        /// <param name="newDataStream">The new data stream.</param>
        public void AddDataStream(Byte[] newDataStream)
        {
            dataStream = newDataStream;
        }

        // Method to support combined indexes.
        /// <summary>
        /// Gets the index.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <returns>IteratorHelper.</returns>
        public static IteratorHelper GetIndex(UInt64 handle)
        {
            return new IteratorHelper(handle);
        }

        // Constructor.
        /// <summary>
        /// Initializes a new instance of the <see cref="IteratorHelper" /> class.
        /// </summary>
        /// <param name="handle">The handle.</param>
        public IteratorHelper(UInt64 handle)
        {
            indexHandle = handle;
            filterHandle = 0;
            dataStream = null;
        }

        // Empty recreation key length.
        /// <summary>
        /// 
        /// </summary>
        public const Int32 RK_EMPTY_LEN = 4;

        // Offset in bytes for number of enumerators.
        /// <summary>
        /// 
        /// </summary>
        public const Int32 RK_ENUM_NUM_OFFSET = 4;

        // Offset in bytes for dynamic data.
        /// <summary>
        /// 
        /// </summary>
        public const Int32 RK_FIRST_DYN_DATA_OFFSET = 8;

        // Length of recreation key header in bytes.
        /// <summary>
        /// 
        /// </summary>
        public const Int32 RK_HEADER_LEN = 12;

        // Gets the information about saved object in the iterator.
        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyData"></param>
        /// <param name="extentNumber"></param>
        /// <param name="keyOid"></param>
        /// <param name="keyEti"></param>
        public static unsafe void RecreateEnumerator_GetObjectInfo(
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rangeFlags"></param>
        /// <param name="firstKey"></param>
        /// <param name="lastKey"></param>
        /// <param name="cachedEnum"></param>
        public void GetEnumeratorCached_NoCodeGenFilter(
            UInt32 rangeFlags,
            Byte[] firstKey,
            Byte[] lastKey,
            Enumerator cachedEnum)
        {
            int retry = 0;

        go:
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

            try {
                throw ErrorCode.ToException(err);
            }
            catch (DbException ex) {
                retry = HandleCreateException(ex, retry);
                if (retry != 0) goto go;
                throw;
            }
        }

        private int HandleCreateException(DbException ex, int retry) {
            if (ex.ErrorCode == Error.SCERRTOMANYOPENITERATORS) {
                var thread = ThreadData.Current;
                while (retry < 2) {
                    retry++;
                    if (thread.CollectAndTryToCleanupDeadObjects(retry == 2)) return retry;
                }
            }
            return 0;
        }

        // No code generation filter is used here.
        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyData"></param>
        /// <param name="extentNumber"></param>
        /// <param name="cachedEnum"></param>
        /// <returns></returns>
        public unsafe Boolean RecreateEnumerator_NoCodeGenFilter(
            Byte* keyData,
            Int32 extentNumber,
            Enumerator cachedEnum,
            UInt32 flags, 
            Byte *lastKey)
        {
            int retry = 0;

        go:
            // Position of enumerator static data.
            Byte* staticDataOffset = keyData + (extentNumber << 3) + RK_HEADER_LEN;

            // Checking if its possible to recreate the key (dynamic data offset).
            UInt32 dynDataOffset = (*(UInt32*)(staticDataOffset + 4));
            if (dynDataOffset == 0)
                return false;

            UInt32 err;
            UInt64 hCursor, verify;

            Byte* staticData = keyData + (*(UInt32*)(staticDataOffset));
            //UInt32 flags = *((UInt32*)staticData);
            //Byte* lastKey = staticData + 4; // Skipping flags.

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

            try {
                throw ErrorCode.ToException(err);
            }
            catch (DbException ex) {
                retry = HandleCreateException(ex, retry);
                if (retry != 0) goto go;
                throw;
            }
        }

        // Scan which uses code generation.
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rangeFlags"></param>
        /// <param name="firstKey"></param>
        /// <param name="secondKey"></param>
        /// <param name="cachedEnum"></param>
        public void GetEnumeratorCached_CodeGenFilter(
            UInt32 rangeFlags,
            Byte[] firstKey,
            Byte[] secondKey,
            Enumerator cachedEnum)
        {
            int retry = 0;

        go:
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

            try {
                throw ErrorCode.ToException(err);
            }
            catch (DbException ex) {
                retry = HandleCreateException(ex, retry);
                if (retry != 0) goto go;
                throw;
            }
        }

        // Code generation filter is used here.
        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyData"></param>
        /// <param name="extentNumber"></param>
        /// <param name="cachedEnum"></param>
        /// <returns></returns>
        public unsafe Boolean RecreateEnumerator_CodeGenFilter(
            Byte* keyData,
            Int32 extentNumber,
            Enumerator cachedEnum,
            UInt64 filterHandle,
            UInt32 flags,
            Byte* lastKey)
        {
            int retry = 0;

        go:
            // Position of enumerator static data.
            Byte* staticDataOffset = keyData + (extentNumber << 3) + RK_HEADER_LEN;

            // Checking if its possible to recreate the key (dynamic data offset).
            UInt32 dynDataOffset = (*(UInt32*)(staticDataOffset + 4));
            if (dynDataOffset == 0)
                return false;

            UInt64 hCursor, verify;
            UInt32 err;

            Byte* staticData = keyData + (*(UInt32*)(staticDataOffset));
            //UInt32 flags = *((UInt32*)staticData);
            //UInt64 filterHandle = *((UInt64*)(staticData + 4));
            Byte* varStream = staticData + 12;
            //Byte* lastKey = *((UInt32*)varStream) + varStream;

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

            try {
                throw ErrorCode.ToException(err);
            }
            catch (DbException ex) {
                retry = HandleCreateException(ex, retry);
                if (retry != 0) goto go;
                throw;
            }
        }
    }
}
