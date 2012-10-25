﻿// ***********************************************************************
// <copyright file="_IndexHint.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

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

    internal String IndexName { get { return indexName.ToUpper(); } }

    internal Int32 ExtentNumber
    {
        get
        {
            return extentNumber;
        }
    }
}
}

