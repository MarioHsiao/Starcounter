// ***********************************************************************
// <copyright file="Path.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Query.Optimization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Starcounter.Query.Execution
{
/// <summary>
/// Abstract base class for paths.
/// </summary>
internal abstract class Path : CodeGenFilterNode
{
    protected Int32 extentNumber;
    protected List<IObjectPathItem> pathList;

    /// <summary>
    /// The extent number corresponding to this path.
    /// </summary>
    public Int32 ExtentNumber
    {
        get
        {
            return extentNumber;
        }
    }

    public override void InstantiateExtentSet(ExtentSet extentSet)
    {
        extentSet.AddExtentNumber(extentNumber);
    }

    // Just need to overload without any implementation.
    public override Boolean EvaluatesToNull(IObjectView obj)
    {
        throw new NotImplementedException("EvaluatesToNull is not implemented for Path");
    }

    /// <summary>
    /// Generic clone for ITypeExpression types.
    /// </summary>
    /// <param name="varArray">Variables array.</param>
    /// <returns>Clone of the expression.</returns>
    public abstract ITypeExpression Clone(VariableArray varArray);

#if DEBUG
    private bool AssertEqualsVisited = false;
    internal bool AssertEquals(Path other) {
        Debug.Assert(other != null);
        if (other == null)
            return false;
        // Check if there are not cyclic references
        Debug.Assert(!this.AssertEqualsVisited);
        if (this.AssertEqualsVisited)
            return false;
        Debug.Assert(!other.AssertEqualsVisited);
        if (other.AssertEqualsVisited)
            return false;
        // Check basic types
        Debug.Assert(this.extentNumber == other.extentNumber);
        if (this.extentNumber != other.extentNumber)
            return false;
        // Check cardinalities of collections
        if (this.pathList == null) {
            Debug.Assert(other.pathList == null);
            if (other.pathList != null)
                return false;
        } else {
            Debug.Assert(this.pathList.Count == other.pathList.Count);
            if (this.pathList.Count != other.pathList.Count)
                return false;
        }
        // Check references. This should be checked if there is cyclic reference.
        AssertEqualsVisited = true;
        bool areEquals = true;
        // Check collections of objects
        if (this.pathList != null)
            for (int i = 0; i < this.pathList.Count && areEquals; i++)
                areEquals = this.pathList[i].AssertEquals(other.pathList[i]);
        AssertEqualsVisited = false;
        return areEquals;
    }
#endif
}
}
