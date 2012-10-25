// ***********************************************************************
// <copyright file="RangePoint.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Starcounter.Query.Execution
{
/// <summary>
/// Abstract base class for range points.
/// </summary>
internal abstract class RangePoint
{
    protected ComparisonOperator compOp;

    public ComparisonOperator Operator
    {
        get
        {
            return compOp;
        }
        set
        {
            compOp = value;
        }
    }
}
}
