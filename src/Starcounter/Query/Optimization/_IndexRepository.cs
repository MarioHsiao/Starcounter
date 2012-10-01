
using Starcounter.Binding;
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

    // Called during startup.
    internal static void Initiate()
    {
        Db.Transaction(() =>
        {
            // Create index dictionary for optimization by removing sorting.
            IndexRepository.CreateIndexDictionaryBySortSpecification();
        });
    }

    internal static void CreateIndexDictionaryBySortSpecification()
    {
        indexDictionaryBySortSpecification = new Dictionary<String, IndexUseInfo>();
        IEnumerator<TypeDef> enumerator = Starcounter.Binding.Bindings.GetAllTypeDefs().GetEnumerator();
        IndexInfo[] indexInfoArray = null;
        IndexUseInfo indexUseInfo = null;
        String key = null;

        while (enumerator.MoveNext())
        {
            indexInfoArray = enumerator.Current.TableDef.GetAllIndexInfos();
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

    internal static IndexUseInfo GetIndexBySortSpecification(String key)
    {
        IndexUseInfo indexUseInfo = null;
        if (indexDictionaryBySortSpecification.TryGetValue(key, out indexUseInfo))
        {
            return indexUseInfo;
        }
        return null;
    }
}
}