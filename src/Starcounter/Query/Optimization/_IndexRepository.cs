
using Starcounter.Query.Execution;
using Starcounter.Query.Optimization;
using Sc.Server.Binding;
using Sc.Server.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Starcounter.Query.Optimization
{
internal static class IndexRepository
{
    /// <summary>
    /// Dictionary of available indexes including information about if they should be used 
    /// ascending or descending.
    /// Dictionary keys are created from the type name and names
    /// of the included paths and their specified sort orderings.
    /// Several keys for the same index are created because all paths in a combined index
    /// do not have to be used.
    /// Used to find an appropriate index for optimization by removing sorting.
    /// </summary>

    // TODO: Should be private. Only temporary internal for debugging purposes.
    static internal Dictionary<String, IndexUseInfo> indexDictionaryBySortSpecification;

    /// <summary>
    /// Dictionary of available indexes with the names of the indexes as keys.
    /// </summary>
    static Dictionary<String, IndexInfo> indexDictionaryByName;

    // Called during startup.
    internal static void Initiate()
    {
#if false // TODO EOH:
        Transaction t = null;
        if (Transaction.Current == null)
            t = Transaction.NewCurrent();

        try
        {
            // Create index dictionary for optimization by removing sorting.
            IndexRepository.CreateIndexDictionaryBySortSpecification();

            // Create index dictionary for finding an index by name.
            IndexRepository.CreateIndexDictionaryByName();
        }
        finally
        {
            if (t != null) 
                t.Dispose();
        }
#endif
    }

    internal static void CreateIndexDictionaryBySortSpecification()
    {
        indexDictionaryBySortSpecification = new Dictionary<String, IndexUseInfo>();
        IEnumerator<TypeBinding> enumerator = TypeRepository.GetAllTypeBindings();
        IndexInfo[] indexInfoArray = null;
        IndexUseInfo indexUseInfo = null;
        String key = null;

        while (enumerator.MoveNext())
        {
            if ((typeof(TypeOrExtensionBinding).IsAssignableFrom(enumerator.Current.GetType())))
            {
#if false // TODO EOH:
                indexInfoArray = (enumerator.Current as TypeOrExtensionBinding).GetAllIndexInfos();
#endif
                for (Int32 i = 0; i < indexInfoArray.Length; i++)
                {
                    for (Int32 j = 1; j <= indexInfoArray[i].AttributeCount; j++)
                    {
                        // Create entry for using index ascending.
                        indexUseInfo = new IndexUseInfo(indexInfoArray[i], SortOrder.Ascending);
                        key = SortSpecification.CreateIndexDictionaryKey(enumerator.Current, indexInfoArray[i], j);
                        if (!indexDictionaryBySortSpecification.ContainsKey(key))
                        {
                            indexDictionaryBySortSpecification.Add(key, indexUseInfo);
                        }
                        // Create entry for iusing index descending.
                        indexUseInfo = new IndexUseInfo(indexInfoArray[i], SortOrder.Descending);
                        key = SortSpecification.CreateReversedIndexDictionaryKey(enumerator.Current, indexInfoArray[i], j);
                        if (!indexDictionaryBySortSpecification.ContainsKey(key))
                        {
                            indexDictionaryBySortSpecification.Add(key, indexUseInfo);
                        }
                    }
                }
            }
        }
    }

    internal static IndexUseInfo GetIndexBySortSpecification(String key)
    {
        IndexUseInfo indexUseInfo = null;
        if (indexDictionaryBySortSpecification.TryGetValue(key, out indexUseInfo))
        {
            return indexUseInfo;
        }
        return null;
    }

    internal static void CreateIndexDictionaryByName()
    {
        indexDictionaryByName = new Dictionary<String, IndexInfo>();
        IEnumerator<TypeBinding> enumerator = TypeRepository.GetAllTypeBindings();
        IndexInfo[] indexInfoArray = null;
        while (enumerator.MoveNext())
        {
            if ((typeof(TypeOrExtensionBinding).IsAssignableFrom(enumerator.Current.GetType())))
            {
                indexInfoArray = (enumerator.Current as TypeOrExtensionBinding).GetAllIndexInfos();
                for (Int32 i = 0; i < indexInfoArray.Length; i++)
                {
                    if (indexDictionaryByName.ContainsKey(indexInfoArray[i].Name))
                    {
                        throw ErrorCode.ToException(Error.SCERRSQLDUPLICATEDIDENTIFIER, "An index with the same name already exists: " + indexInfoArray[i].Name);
                    }
                    indexDictionaryByName.Add(indexInfoArray[i].Name, indexInfoArray[i]);
                }
            }
        }
    }

    internal static IndexInfo GetIndexByName(String name)
    {
        IndexInfo indexInfo = null;
        if (indexDictionaryByName.TryGetValue(name, out indexInfo))
        {
            return indexInfo;
        }
        return null;
    }
}
}