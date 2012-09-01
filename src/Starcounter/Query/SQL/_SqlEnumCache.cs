using System;
using System.Collections.Generic;

using Starcounter;
using Starcounter.Internal;
using Starcounter.Query.Sql;
using Sc.Server.Binding;
using Sc.Server.Internal;
using Starcounter.Query.Execution;

namespace Starcounter.Query.Sql
{
/// <summary>
/// This cache is used for storing SQL enumerators corresponding to queries.
/// This cache shared between threads of one virtual processor.
/// </summary>
public sealed class SqlEnumCache
{
    // Used for fast access to needed enumerator with unique query ID. Never flushed, only extends.
    LinkedListNode<LinkedList<IExecutionEnumerator>>[] enumArray = new LinkedListNode<LinkedList<IExecutionEnumerator>>[GlobalQueryCache.MaxUniqueQueries];

    // Total number of cached enumerators in this cache.
    Int32 totalCachedEnum = 0;

    // Index of the last used enumerator.
    Int32 lastUsedEnumIndex = 0;

    // Just a temporary buffer.
    internal Byte[] TempBuffer = new Byte[SqlConnectivityInterface.RECREATION_KEY_MAX_BYTES];

    /// <summary>
    /// Gets an already existing enumerator given the unique query ID.
    /// </summary>
    internal IExecutionEnumerator GetCachedEnumerator(Int32 uniqueQueryId)
    {
        IExecutionEnumerator execEnum = null;

        // Getting the enumerator list.
        LinkedListNode<LinkedList<IExecutionEnumerator>> enumListListNode = enumArray[uniqueQueryId];
        if (enumListListNode != null)
        {
            // Getting enumerator list inside the node.
            LinkedList<IExecutionEnumerator> enumList = enumListListNode.Value;

            // Checking if there are any enumerators in the list.
            if (enumList.Count == 0)
            {
                // Always using first cached enumerator for cloning (because of dynamic ranges).
                execEnum = Scheduler.GlobalCache.GetEnumClone(uniqueQueryId);

                // Increasing the number of enumerators.
                totalCachedEnum++;

                // Giving the cache where all subsequent enumerators should be returned.
                execEnum.AttachToCache(enumList);
            }
            else
            {
                // Cutting last enumerator.
                execEnum = enumList.Last.Value;
                enumList.RemoveLast();
            }
        }
        else
        {
            // Fetching existing enumerator from the global cache.
            execEnum = Scheduler.GlobalCache.GetEnumClone(uniqueQueryId);

            // Increasing the number of enumerators
            totalCachedEnum++;

            // Creating new list for enumerators of the same query.
            LinkedList<IExecutionEnumerator> newEnumList = new LinkedList<IExecutionEnumerator>();

            // Creating node with enumerator list identified by query and adding it to cache dictionary.
            enumListListNode = new LinkedListNode<LinkedList<IExecutionEnumerator>>(newEnumList);

            // Adding new enumerator to the array.
            enumArray[uniqueQueryId] = enumListListNode;

            // Giving the cache where all subsequent enumerators should be returned.
            execEnum.AttachToCache(newEnumList);

            /*
            // Creating code generation engine.
            CodeGenStringGenerator stringGen = new CodeGenStringGenerator(uniqueQueryID);

            // Generating code from managed execution plan.
            String rootEnumName = execEnum.GetUniqueName(stringGen.SeqNumber());

            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "DLL_EXPORT INT32 CALL_CONV InitEnumerator_" + execEnum.UniqueQueryID + "(UINT8 *queryParameters)");
            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "{");
            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "g_QueryParamsData = queryParameters;");
            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "// Chopping query parameters here.");
            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "ProcessQueryParameters(g_QueryParamsData);");
            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "return 0;");
            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "}" + CodeGenStringGenerator.ENDL);

            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "DLL_EXPORT INT32 CALL_CONV MoveNext_" + execEnum.UniqueQueryID + "(UINT64 *oid, UINT64 *eti, UINT16 *ci)");
            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "{");
            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "INT32 errCode = " + rootEnumName + "_MoveNext();");
            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "if (errCode != 0)");
            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "  return errCode;" + CodeGenStringGenerator.ENDL);

            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "// Since the scan was successful, copying the results.");
            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "*oid = g_" + rootEnumName + "->oid;");
            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "*eti = g_" + rootEnumName + "->eti;");
            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "*ci = g_" + rootEnumName + "->ci;");
            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "return 0;");
            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "}" + CodeGenStringGenerator.ENDL);

            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "DLL_EXPORT INT32 CALL_CONV Reset_" + execEnum.UniqueQueryID + "()");
            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "{");
            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "return " + rootEnumName + "_Reset();");
            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "}" + CodeGenStringGenerator.ENDL);

            // Calling recursive code generation for the root enumerator.
            execEnum.GenerateCompilableCode(stringGen);

            // Compiling and verifying library.
            if (CompilerHelper.CompileAndVerifyLibrary(stringGen.GetGeneratedCode(), execEnum.UniqueQueryID) != null)
                execEnum.UniqueQueryID = 0; // Code generation has failed.
            */
        }

        // Adding to the sorting list.
        lastUsedEnumIndex = uniqueQueryId;

        return execEnum;
    }

    /// <summary>
    /// Gets an already existing enumerator corresponding to the query from the cache or creates a new one.
    /// </summary>
    internal IExecutionEnumerator GetCachedEnumerator(String query)
    {
        // Trying last used enumerator.
        if (query == Scheduler.GlobalCache.GetQueryString(lastUsedEnumIndex))
        {
            return GetCachedEnumerator(lastUsedEnumIndex);
        }

        // We have to ask dictionary for the index.
        Int32 enumIndex = Scheduler.GlobalCache.GetEnumIndex(query);

        // Checking if its completely new query.
        if (enumIndex < 0)
        {
            enumIndex = Scheduler.GlobalCache.AddNewQuery(query);
        }

        // Fetching existing enumerator using index.
        return GetCachedEnumerator(enumIndex);
    }

    /// <summary>
    /// Logs the current status of server SQL query cache.
    /// </summary>
    internal String SQLCacheStatus()
    {
        return String.Format("Server SQL query cache status: Totally amount of enumerators = {0}.", totalCachedEnum);
    }
}
}
