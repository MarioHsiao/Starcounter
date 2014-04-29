#if false
// ***********************************************************************
// <copyright file="SqlConnectivity.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query.Sql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime.InteropServices;
using Sc.Query.Execution;
using Starcounter.Internal;

namespace Starcounter.Query.Execution
{
internal static class SqlConnectivity
{
    /// <summary>
    /// Needs to be static so that function pointers do not get destroyed.
    /// </summary>
    internal static Starcounter.Internal.SC_SQL_CALLBACKS sqlCallbacks = new Starcounter.Internal.SC_SQL_CALLBACKS();

    // Returns the error based on the raised exception.
    internal static UInt32 ReturnSqlConnError(Exception exc, UInt32 defErrorCode)
    {
        String excMessage = null;
        if (!ErrorCode.TryGetOrigMessage(exc, out excMessage))
        {
            excMessage = exc.Message;
        }

        // Fetching inner exception.
        if (exc.InnerException != null)
        {
            excMessage = exc.InnerException.Message + Environment.NewLine + excMessage;
        }

        // Saving current exception message.
        Scheduler.AddErrorMessage(excMessage + Environment.NewLine + exc.StackTrace);

        // Trying to return the embedded error code.
        UInt32 errCode;
        if (ErrorCode.TryGetCode(exc, out errCode))
        {
            return errCode;
        }

        // Returning the given code.
        return defErrorCode;
    }

    /// <summary>
    /// Connects native pointers with managed SQL functions.
    /// </summary>
    internal static unsafe UInt32 InitSqlFunctions()
    {
        try
        {
            sqlCallbacks.pSqlConn_GetQueryUniqueId = SqlConnectivity.GetQueryUniqueId;
            sqlCallbacks.pSqlConn_GetResults = SqlConnectivity.GetResults;
            sqlCallbacks.pSqlConn_GetInfo = SqlConnectivity.GetInfo;

            return SqlConnectivityInterface.SqlConn_InitManagedFunctions(ref sqlCallbacks);
        }
        catch (Exception exc)
        {
            return ReturnSqlConnError(exc, Error.SCERRCONNINITSQLFUNCTIONS);
        }
    }

    /// <summary>
    /// Called from native code to create SQL enumerator and retrieve unique query ID.
    /// </summary>
    internal static unsafe UInt32 GetQueryUniqueId(Char *query, UInt64 *uniqueQueryId, UInt32 *queryFlags)
    {
        try
        {
            // Emptying the query ID.
            *uniqueQueryId = 0;

            // Emptying the flags.
            *queryFlags = 0;

            // Creating managed string wrapper from native.
            String _query = new String(query);
            //SqlDebugHelper.PrintDelimiter("Processing: \"" + _query + "\"");

            // Creating enumerator for current virtual processor.
            using (IExecutionEnumerator sqlEnum = Scheduler.GetInstance().SqlEnumCache.GetCachedEnumerator(_query))
            {
                // Getting needed fields.
                *uniqueQueryId = sqlEnum.UniqueQueryID;

                // Populating query flags.
                sqlEnum.PopulateQueryFlags(queryFlags);

                return 0;
            }
        }
        catch (Exception exc)
        {
            return ReturnSqlConnError(exc, Error.SCERRCONNGETQUERYID);
        }
    }

    /// <summary>
    /// Gets query results by executing corresponding SQL enumerator.
    /// </summary>
    internal static unsafe UInt32 GetResults(
        UInt64 uniqueQueryId,
        Byte *queryParams,
        Byte *results,
        UInt32 resultsMaxBytes,
        UInt32 *resultsNum,
        Byte *recreationKey,
        UInt32 recreationKeyMaxBytes,
        UInt32 *flags)
    {
        //Application.Profiler.Start("Server-GetResults", 1);

        // Setting results length immediately.
        (*(UInt32 *) results) = 8;

        // Fetching-related constants.
        Boolean hasRecreationKey = false;
        Boolean finalFetch = (((*flags) & SqlConnectivityInterface.FLAG_LAST_FETCH) != 0);

        // Checking if its a first page execution.
        if ((*(UInt32*)recreationKey) > IteratorHelper.RK_EMPTY_LEN)
        {
            hasRecreationKey = true;
        }

        try
        {
            // Fetching scheduler id.
            Byte schedId = (Byte)((uniqueQueryId >> 24) & 0xFF);

            // Fetching query id.
            Int32 queryId = (Int32)(uniqueQueryId & 0xFFFFFF);

            // Getting the enumerator from cache.
            IExecutionEnumerator sqlEnum = Scheduler.GetInstance(schedId).SqlEnumCache.GetCachedEnumerator(queryId);

            // Attaching to current transaction.
            if (Starcounter.Transaction.Current != null)
                sqlEnum.TransactionId = Starcounter.Transaction.Current.TransactionId;
            else
                sqlEnum.TransactionId = 0;

            try
            {
                if (hasRecreationKey)
                {
                    // Setting the enumerator recreation key.
                    sqlEnum.VarArray.RecreationKeyData = recreationKey;
                }

                // Populating the query variables.
                if (*((UInt32*)queryParams) > IteratorHelper.RK_EMPTY_LEN)
                {
                    sqlEnum.InitVariablesFromBuffer(queryParams);
                }

                //Application.Profiler.Start("FillupFoundObjectIDs", 3);

                // Obtaining all results from enumerator.
                UInt32 err = sqlEnum.FillupFoundObjectIDs(
                    results,
                    resultsMaxBytes,
                    resultsNum,
                    flags);

                //Application.Profiler.Stop(3);

                // Checking that no error occurred.
                if (err == 0)
                {
                    // Checking if it was last fetch.
                    if (finalFetch)
                    {
                        (*flags) &= (~SqlConnectivityInterface.FLAG_MORE_RESULTS);
                    }
                    // Saving the enumerator state.
                    else if (((*flags) & SqlConnectivityInterface.FLAG_MORE_RESULTS) != 0)
                    {
                        //Application.Profiler.Start("SaveEnumerator", 10);

                        Int32 globalOffset = 0;

                        // Writing number of enumerators if enumerator was not recreated.
                        if (!hasRecreationKey)
                        {
                            // Getting the amount of leaves in execution tree (number of enumerators).
                            Int32 leavesNum = sqlEnum.RowTypeBinding.ExtentOrder.Count;
                            globalOffset = ((leavesNum << 3) + IteratorHelper.RK_HEADER_LEN);

                            // Saving number of enumerators.
                            (*(Int32*)(recreationKey + IteratorHelper.RK_ENUM_NUM_OFFSET)) = leavesNum;
                        }

                        // Saving static data (or obtaining absolute position of the first dynamic data).
                        globalOffset = sqlEnum.SaveEnumerator(recreationKey, globalOffset, false);

                        // Saving dynamic data.
                        globalOffset = sqlEnum.SaveEnumerator(recreationKey, globalOffset, true);

                        // Saving full recreation key length.
                        (*(Int32 *)recreationKey) = globalOffset;

                        //Application.Profiler.Stop(10);
                    }
                }

                return err;
            }
            finally
            {
                // Reseting the variable array shared data.
                sqlEnum.VarArray.Reset();

                // Disposing the enumerator and returning to cache.
                sqlEnum.Dispose();

                //Application.Profiler.Stop(1);
            }
        }
        catch (Exception exc)
        {
            return ReturnSqlConnError(exc, Error.SCERRCONNGETQUERYRESULTS);
        }
    }

    // Throws converted server error.
    public static void ThrowConvertedServerError(UInt32 errCode)
    {
        String serverException = SqlConnectivity.GetServerStatusString(SqlConnectivityInterface.GET_LAST_ERROR, 0);
        throw ErrorCode.ToException(errCode, serverException);
    }

    // Gets status description from the server and returns it as a string.
    public static String GetServerStatusString(Byte statusType, UInt64 param)
    {
        Char[] status = new Char[SqlConnectivityInterface.MAX_STATUS_STRING_LEN];

        UInt32 outLenBytes = 0;
        unsafe
        {
            fixed (Char *pStatus = status)
            {
                SqlConnectivityInterface.SqlConn_GetInfo(
                    statusType,
                    param,
                    (Byte *)pStatus,
                    SqlConnectivityInterface.MAX_STATUS_STRING_LEN * 2,
                    &outLenBytes);
            }
        }

        return new String(status, 0, (Int32)(outLenBytes >> 1));
    }

    // Gets profiling results from the server and returns them as a string.
    public static String GetServerProfilingString()
    {
        Char[] profResults = new Char[SqlConnectivityInterface.MAX_STATUS_STRING_LEN];

        UInt32 outLenBytes = 0;
        unsafe
        {
            fixed (Char* pProfResults = profResults)
            {
                SqlConnectivityInterface.SqlConn_GetInfo(
                    SqlConnectivityInterface.PRINT_PROFILER_RESULTS,
                    0,
                    (Byte*)pProfResults,
                    SqlConnectivityInterface.MAX_STATUS_STRING_LEN * 2,
                    &outLenBytes);
            }
        }

        return new String(profResults, 0, (Int32)(outLenBytes >> 1));
    }

    // Copies string to char array.
    static unsafe void CopyStringToCharBuffer(String s, Char *result, UInt32 maxBytes, UInt32 *outLenBytes)
    {
        // Number of characters to copy.
        UInt32 charsToCopy = (UInt32)s.Length;

        // Maximum characters fitting in buffer.
        UInt32 maxChars = (maxBytes >> 1);

        // Checking if string fits into buffer.
        if (charsToCopy > maxChars)
        {
            charsToCopy = maxChars;
        }

        // Copying character by character.
        for (Int32 i = 0; i < charsToCopy; i++)
        {
            result[i] = s[i];
        }

        // Copying the length in bytes.
        *outLenBytes = (charsToCopy << 1);
    }

    /// <summary>
    /// Fetches information about the query.
    /// </summary>
    internal static unsafe UInt32 GetInfo(
        Byte infoType,
        UInt64 param,
        Byte *results,
        UInt32 maxBytes,
        UInt32 *outLenBytes)
    {
        // Setting length to 0 immediately.
        *outLenBytes = 0;

        // Switching between user choices.
        switch (infoType)
        {
            case SqlConnectivityInterface.GET_LAST_ERROR:
            {
                // Copying the error message.
                CopyStringToCharBuffer(Scheduler.GetErrorMessages(),
                    (Char *)results,
                    maxBytes,
                    outLenBytes);

                break;
            }

            case SqlConnectivityInterface.GET_QUERY_CACHE_STATUS:
            {
                StringBuilder allCachesStatus = new StringBuilder();

                // Getting state of the global query cache.
                allCachesStatus.AppendLine(Scheduler.GlobalCache.SQLCacheStatus());

                // Iterating through all virtual processors.
                for (Byte i = 0; i < Scheduler.SchedulerCount; i++)
                {
                    allCachesStatus.AppendLine(Scheduler.GetInstance(i).SqlEnumCache.SQLCacheStatus());
                }

                // Copying string to output.
                CopyStringToCharBuffer(allCachesStatus.ToString(), (Char *)results, maxBytes, outLenBytes);

                break;
            }

            case SqlConnectivityInterface.GET_ENUMERATOR_EXEC_PLAN:
            case SqlConnectivityInterface.GET_FETCH_VARIABLE:
            case SqlConnectivityInterface.GET_RECREATION_KEY_VARIABLE:
            {
                // Getting the enumerator from cache.
                using (IExecutionEnumerator sqlEnum = Scheduler.GetInstance().SqlEnumCache.GetCachedEnumerator((Int32)param))
                {
                    if (infoType == SqlConnectivityInterface.GET_ENUMERATOR_EXEC_PLAN)
                    {
                        // Creating the string with enumerator execution plan.
                        CopyStringToCharBuffer(sqlEnum.ToString(), (Char *)results, maxBytes, outLenBytes);
                    }
                    else // Getting other types of info from enumerator.
                    {
                        sqlEnum.GetInfo(infoType, param, results, maxBytes, outLenBytes);
                    }
                }

                break;
            }

            case SqlConnectivityInterface.PRINT_PROFILER_RESULTS:
            {
                // Copying the profiling results.
                CopyStringToCharBuffer(Application.Profiler.GetResultsInJson(),
                    (Char*)results,
                    maxBytes,
                    outLenBytes);

                break;
            }
        }

        return 0;
    }
}
}
#endif
