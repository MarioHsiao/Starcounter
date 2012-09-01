
using Sc.Server.Binding;
using Sc.Server.Internal;
using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;

namespace Starcounter.Query.Optimization
{
internal class IndexHint : IHint
{
    Int32 extentNumber;
    String indexName;

    internal IndexHint(Int32 extentNumber, String indexName)
    {
        if (indexName == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect indexName.");
        }
        this.extentNumber = extentNumber;
        this.indexName = indexName;
    }

    internal IndexInfo GetIndexInfo()
    {
        return IndexRepository.GetIndexByName(indexName);
    }

    internal Int32 ExtentNumber
    {
        get
        {
            return extentNumber;
        }
    }
}
}

