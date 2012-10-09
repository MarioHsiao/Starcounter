
using Starcounter.Query.Optimization;
using System;
using System.Collections.Generic;
using System.Text;

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
}
}
