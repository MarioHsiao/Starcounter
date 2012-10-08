
using Sc.Server.Internal;
using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;

namespace Starcounter.Query.Optimization
{
internal class JoinOrderHint : IHint
{
    List<Int32> extentNumList;

    internal JoinOrderHint(List<Int32> extentNumList)
    {
        if (extentNumList == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extentNumList.");
        }
        // Empty extentNumList (Count = 0) represents "JOIN ORDER FIXED".
        this.extentNumList = extentNumList;
    }

    internal List<Int32> ExtentNumList
    {
        get
        {
            return extentNumList;
        }
    }
}
}

