﻿
using Starcounter.Binding;
using Starcounter.Internal;
using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;

namespace Starcounter.Query.Optimization
{
/// <summary>
/// Class that holds information about the sort specification of a query.
/// </summary>
internal class SortSpecification
{
    /// <summary>
    /// The type binding of the resulting objects of the current query.
    /// </summary>
    CompositeTypeBinding resultTypeBind;

    /// <summary>
    /// A list of sort specifications of a query (specified in the ORDER BY clause).
    /// </summary>
    List<ISingleComparer> sortSpecItemList;

    /// <summary>
    /// Constructor.
    /// </summary>
    internal SortSpecification(CompositeTypeBinding resultTypeBind, List<ISingleComparer> sortSpecItemList)
    {
        if (resultTypeBind == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect resultTypeBind.");
        }
        if (sortSpecItemList == null && sortSpecItemList.Count > 0)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect sortSpecList.");
        }
        this.resultTypeBind = resultTypeBind;
        this.sortSpecItemList = sortSpecItemList;
    }

    /// <summary>
    /// Creates a list of extent numbers representing the required extent order for sort optimization.
    /// Returns "null" when no sort optimization is possible.
    /// </summary>
    /// <returns>Returns a list of extent numbers, or "null".</returns>
    internal List<Int32> CreateSortExtentOrder()
    {
        List<Int32> sortExtentOrder = new List<Int32>();
        IPathComparer pathComparer = null;
        Int32 extentNum = -1;
        for (Int32 i = 0; i < sortSpecItemList.Count; i++)
        {
            if (sortSpecItemList[i] is IPathComparer)
            {
                pathComparer = (sortSpecItemList[i] as IPathComparer);
                if (pathComparer.Path.ExtentNumber != extentNum)
                {
                    extentNum = pathComparer.Path.ExtentNumber;
                    sortExtentOrder.Add(extentNum);
                }
            }
            else
            {
                return null;
            }
        }
        return sortExtentOrder;
    }

    /// <summary>
    /// Creates a list of indexes to be used for sort optimization.
    /// Returns "null" if no such list of indexes exist.
    /// </summary>
    /// <returns>A list of index-use-specifications, or "null".</returns>
    internal List<IndexUseInfo> CreateIndexUseInfoList()
    {
        // Investigate if all the expressions in the sort-specification (sortSpecItemList) are paths, 
        // and save the sort-specification-items (pathComparer:s) in a pathComparerListList, where each 
        // pathComparerList includes sort-specification-items for one extent.
        List<List<IPathComparer>> pathComparerListList = new List<List<IPathComparer>>();
        List<IPathComparer> pathComparerList = null;
        IPathComparer pathComparer = null;
        Int32 extentNum = -1;
        for (Int32 i = 0; i < sortSpecItemList.Count; i++)
        {
            if (sortSpecItemList[i] is IPathComparer)
            {
                pathComparer = (sortSpecItemList[i] as IPathComparer);
                if (pathComparer.Path.ExtentNumber != extentNum)
                {
                    extentNum = pathComparer.Path.ExtentNumber;
                    pathComparerList = new List<IPathComparer>();
                    pathComparerListList.Add(pathComparerList);
                }
                pathComparerList.Add(pathComparer);
            }
            else
            {
                return null;
            }
        }
        // Investigate if there are indexes that can be used for all sort-specification-items,
        // and create a list with info about the appropriate indexes and required sort-order.
        List<IndexUseInfo> indexUseInfoList = new List<IndexUseInfo>();
        IndexUseInfo indexUseInfo = null;
        for (Int32 i = 0; i < pathComparerListList.Count; i++)
        {
            indexUseInfo = GetIndexUseInfo(resultTypeBind, pathComparerListList[i]);
            if (indexUseInfo != null)
            {
                indexUseInfoList.Add(indexUseInfo);
            }
            else
            {
                return null;
            }
        }
        return indexUseInfoList;
    }

    private static IndexUseInfo GetIndexUseInfo(CompositeTypeBinding resultTypeBind, List<IPathComparer> pathComparerList)
    {
        if (pathComparerList.Count == 0)
        {
            return null;
        }

        Int32 extentNumber = pathComparerList[0].Path.ExtentNumber;
        TypeBinding typeBind = (resultTypeBind.GetTypeBinding(extentNumber) as TypeBinding);

#if true
        unsafe
        {
            sccoredb.SCCOREDB_SORT_SPEC_ELEM[] sort_spec = new sccoredb.SCCOREDB_SORT_SPEC_ELEM[pathComparerList.Count + 1];
            for (int i = 0; i < pathComparerList.Count; i++)
            {
                sort_spec[i].column_index = (Int16)typeBind.GetPropertyBinding(pathComparerList[i].Path.FullName).GetDataIndex();
                sort_spec[i].sort = (Byte)pathComparerList[i].SortOrdering;
            }
            sort_spec[pathComparerList.Count].column_index = -1;

            fixed (sccoredb.SCCOREDB_SORT_SPEC_ELEM* fixed_sort_spec = sort_spec)
            {
                sccoredb.SC_INDEX_INFO kernel_index_info;
                uint r = sccoredb.sccoredb_get_index_info_by_sort(typeBind.DefHandle, fixed_sort_spec, &kernel_index_info);
                if (r == 0)
                {
                    return new IndexUseInfo(typeBind.TypeDef.TableDef.CreateIndexInfo(&kernel_index_info), SortOrder.Ascending);
                }
                if (r != Error.SCERRINDEXNOTFOUND) throw ErrorCode.ToException(r);
            }

            // Try find index with matching decending sort.

            for (int i = 0; i < (sort_spec.Length - 1); i++)
            {
                sort_spec[i].sort = sort_spec[i].sort == 0 ? (byte)1 : (byte)0;
            }
            sort_spec[pathComparerList.Count].column_index = -1;

            fixed (sccoredb.SCCOREDB_SORT_SPEC_ELEM* fixed_sort_spec = sort_spec)
            {
                sccoredb.SC_INDEX_INFO kernel_index_info;
                uint r = sccoredb.sccoredb_get_index_info_by_sort(typeBind.DefHandle, fixed_sort_spec, &kernel_index_info);
                if (r == 0)
                {
                    return new IndexUseInfo(typeBind.TypeDef.TableDef.CreateIndexInfo(&kernel_index_info), SortOrder.Descending);
                }
                if (r == Error.SCERRINDEXNOTFOUND) return null;
                throw ErrorCode.ToException(r);
            }
        }
#endif

#if false // TODO: Remove!
        TypeDef typeDef = typeBind.TypeDef;

        String key = CreateIndexDictionaryKey(typeDef, pathComparerList);
        if (key == null)
        {
            return null;
        }
        IndexUseInfo indexUseInfo = GetIndexBySortSpecification(
            CreateIndexDictionaryBySortSpecification(typeDef),
            key
            );
        return indexUseInfo; // Might be "null".
#endif
    }

#if false
    private static String CreateIndexDictionaryKey(TypeDef typeBind, List<IPathComparer> pathComparerList)
    {
        // Assume pathComparerList.Count > 0.

        String key = typeBind.Name;
        for (Int32 i = 0; i < pathComparerList.Count; i++)
        {
            key += "-" + pathComparerList[i].Path.FullName + ":" + pathComparerList[i].SortOrdering;
        }
        return key;
    }

    private static Dictionary<String, IndexUseInfo> CreateIndexDictionaryBySortSpecification(TypeDef typeDef)
    {
        Dictionary<String, IndexUseInfo> indexDictionaryBySortSpecification = new Dictionary<String, IndexUseInfo>();
        IndexInfo[] indexInfoArray = null;
        IndexUseInfo indexUseInfo = null;
        String key = null;

        indexInfoArray = typeDef.TableDef.GetAllIndexInfos();
        for (Int32 i = 0; i < indexInfoArray.Length; i++)
        {
            for (Int32 j = 1; j <= indexInfoArray[i].AttributeCount; j++)
            {
                // Create entry for using index ascending.
                indexUseInfo = new IndexUseInfo(indexInfoArray[i], SortOrder.Ascending);
                key = CreateIndexDictionaryKey(typeDef, indexInfoArray[i], j);
                if (!indexDictionaryBySortSpecification.ContainsKey(key))
                {
                    indexDictionaryBySortSpecification.Add(key, indexUseInfo);
                }
                // Create entry for iusing index descending.
                indexUseInfo = new IndexUseInfo(indexInfoArray[i], SortOrder.Descending);
                key = CreateReversedIndexDictionaryKey(typeDef, indexInfoArray[i], j);
                if (!indexDictionaryBySortSpecification.ContainsKey(key))
                {
                    indexDictionaryBySortSpecification.Add(key, indexUseInfo);
                }
            }
        }

        return indexDictionaryBySortSpecification;
    }

    private static IndexUseInfo GetIndexBySortSpecification(Dictionary<String, IndexUseInfo> indexDictionaryBySortSpecification, String key)
    {
        IndexUseInfo indexUseInfo = null;
        if (indexDictionaryBySortSpecification.TryGetValue(key, out indexUseInfo))
        {
            return indexUseInfo;
        }
        return null;
    }

    private static String CreateIndexDictionaryKey(TypeDef typeBind, IndexInfo indexInfo, Int32 usedArity)
    {
        if (usedArity < 1 || usedArity > indexInfo.AttributeCount)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect usedArity.");
        }
        String key = typeBind.Name;
        for (Int32 i = 0; i < usedArity; i++)
        {
            key += "-" + indexInfo.GetPathName(i) + ":" + indexInfo.GetSortOrdering(i).ToString();
        }
        return key;
    }
#endif

    private static SortOrder Reverse(SortOrder sortOrdering)
    {
        if (sortOrdering == SortOrder.Ascending)
            return SortOrder.Descending;
        return SortOrder.Ascending;
    }

    private static String CreateReversedIndexDictionaryKey(TypeDef typeBind, IndexInfo indexInfo, Int32 usedArity)
    {
        if (usedArity < 1 || usedArity > indexInfo.AttributeCount)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect usedArity.");
        }
        String key = typeBind.Name;
        for (Int32 i = 0; i < usedArity; i++)
        {
            key += "-" + indexInfo.GetPathName(i) + ":" + Reverse(indexInfo.GetSortOrdering(i)).ToString();
        }
        return key;
    }

    internal Boolean IsEmpty()
    {
        return (sortSpecItemList.Count == 0);
    }

    internal IQueryComparer CreateComparer()
    {
        if (sortSpecItemList.Count == 1)
        {
            return sortSpecItemList[0];
        }
        return new MultiComparer(sortSpecItemList);
    }
}
}

