
using Starcounter.Binding;
using Starcounter.Query.Execution;
using Sc.Server.Binding;
using Sc.Server.Internal;
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
        String key = CreateIndexDictionaryKey(resultTypeBind, pathComparerList);
        if (key == null)
        {
            return null;
        }
        IndexUseInfo indexUseInfo = IndexRepository.GetIndexBySortSpecification(key);
        return indexUseInfo; // Might be "null".
    }

    private static String CreateIndexDictionaryKey(CompositeTypeBinding resultTypeBind, List<IPathComparer> pathComparerList)
    {
        if (pathComparerList.Count == 0)
        {
            return null;
        }
        Int32 extentNumber = pathComparerList[0].Path.ExtentNumber;
        ITypeBinding typeBind = resultTypeBind.GetTypeBinding(extentNumber);
        String key = typeBind.Name;
        for (Int32 i = 0; i < pathComparerList.Count; i++)
        {
            key += "-" + pathComparerList[i].Path.FullName + ":" + pathComparerList[i].SortOrdering;
        }
        return key;
    }

    internal static String CreateIndexDictionaryKey(TypeDef typeBind, IndexInfo indexInfo, Int32 usedArity)
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

    private static SortOrder Reverse(SortOrder sortOrdering)
    {
        if (sortOrdering == SortOrder.Ascending)
            return SortOrder.Descending;
        return SortOrder.Ascending;
    }

    internal static String CreateReversedIndexDictionaryKey(TypeDef typeBind, IndexInfo indexInfo, Int32 usedArity)
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

