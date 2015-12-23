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

        /// <summary>
        /// Empty recreation key length.
        /// </summary>
        public const UInt16 RK_EMPTY_LEN = 4;

        /// <summary>
        /// Offset in bytes for number of enumerators.
        /// Skips length of the offset.
        /// </summary>
        public const UInt16 RK_NODE_NUM_OFFSET = 2;

        /// <summary>
        /// Offset in bytes for dynamic data.
        /// Skips number of nodes.
        /// </summary>
        public const UInt16 RK_FIRST_DYN_DATA_OFFSET = RK_NODE_NUM_OFFSET + 1;

        /// <summary>
        /// Length of recreation key header in bytes.
        /// Skips first written offset of dynamic data.
        /// </summary>
        public const UInt16 RK_HEADER_LEN = RK_FIRST_DYN_DATA_OFFSET + 2;

        // Gets the information about saved object in the iterator.
        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyData"></param>
        /// <param name="extentNumber"></param>
        /// <param name="keyOid"></param>
        /// <param name="keyEti"></param>
        public static unsafe void RecreateEnumerator_GetObjectInfo(
            Byte* recreationKey,
            out UInt64 keyOid,
            out UInt64 keyEti)
        {
#if true
            // Fetching OID and ETI.
            UInt32 keyLen = (*(UInt32*)recreationKey);
            keyOid = *(UInt64*)(recreationKey + keyLen - 8);
            keyEti = *(UInt64*)(recreationKey + keyLen - 16);
#else
            // Fetching OID and ETI.
            UInt32 keyLen = (*(UInt32*)recreationKey);
            keyOid = *(UInt64*)(recreationKey + keyLen - 12);
            keyEti = *(UInt64*)(recreationKey + keyLen - 20);
#endif
            // To get the record handle from the record reference in the index (which is what is
            // stored in the recreate key). Not very pretty but a temporary fix. Record handle
            // should not be used to identify a record. Issue in tracker: #3066.
            keyEti >>= 1;
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
                        err = sccoredb.star_context_create_iterator(
                            ThreadData.ContextHandle,
                            indexHandle,
                            rangeFlags,
                            sameKey,
                            sameKey,
                            &hCursor
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
                        err = sccoredb.star_context_create_iterator(
                            ThreadData.ContextHandle,
                            indexHandle,
                            rangeFlags,
                            fk,
                            lk,
                            &hCursor
                        );
                    }
                }
            }

            // Checking error code.
            if (err == 0)
            {
                verify = ThreadData.ObjectVerify;
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
            Byte* recreationKey,
            Enumerator cachedEnum,
            UInt32 flags, 
            Byte[] lastKey)
        {
            int retry = 0;

        go:
            UInt32 err;
            UInt64 hCursor, verify;

            // Recreating iterator using obtained data.
            //SqlDebugHelper.PrintByteBuffer("IndexScan Using Recreation Key", recreationKey, true);
            //Application.Profiler.Start("sc_recreate_iterator", 7);
            fixed (Byte* lastKeyPointer = lastKey) {
                err = sccoredb.star_context_recreate_iterator(
                    ThreadData.ContextHandle, indexHandle, flags, recreationKey, lastKeyPointer,
                    &hCursor
                );
            }
            //Application.Profiler.Stop(7);

            // Checking error code.
            if (err == 0)
            {
                verify = ThreadData.ObjectVerify;
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
            FilterEnumerator cachedEnum)
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
                    err = sccoredb.star_context_create_filter_iterator(
                        ThreadData.ContextHandle,
                        indexHandle,
                        rangeFlags,
                        fk,
                        sk,
                        filterHandle,
                        vs,
                        &hCursor
                    );
                }
            }

            // Checking error code.
            if (err == 0)
            {
                verify = ThreadData.ObjectVerify;
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
            Byte* recreationKey,
            FilterEnumerator cachedEnum,
            UInt64 filterHandle,
            UInt32 flags,
            Byte[] filterDataStream,
            Byte[] lastKey)
        {
            int retry = 0;

        go:

            UInt64 hCursor, verify;
            UInt32 err;

            //SqlDebugHelper.PrintByteBuffer("FullTableScan Using Recreation Key", recreationKey, true);
            fixed (Byte* varStream = filterDataStream, lastKeyPointer = lastKey) {
                // Recreating iterator using obtained data.
                err = sccoredb.star_context_recreate_filter_iterator(
                    ThreadData.ContextHandle,
                    indexHandle,
                    flags,
                    recreationKey,
                    lastKeyPointer,
                    filterHandle,
                    varStream,
                    &hCursor
                );
            }

            // Checking error code.
            if (err == 0)
            {
                verify = ThreadData.ObjectVerify;
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
