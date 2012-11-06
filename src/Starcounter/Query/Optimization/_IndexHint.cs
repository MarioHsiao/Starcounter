// ***********************************************************************
// <copyright file="_IndexHint.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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

    internal String IndexName { get { return indexName; } }

    internal Int32 ExtentNumber
    {
        get
        {
            return extentNumber;
        }
    }

#if DEBUG
    internal bool AssertEquals(IndexHint other) {
        Debug.Assert(other != null);
        if (other == null)
            return false;
        // Check basic types
        Debug.Assert(this.extentNumber == other.extentNumber);
        if (this.extentNumber != other.extentNumber)
            return false;
        Debug.Assert(this.indexName == other.indexName);
        if (this.indexName != other.indexName)
            return false;
        return true;
    }
#endif
}
}

