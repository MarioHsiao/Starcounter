// ***********************************************************************
// <copyright file="Literal.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Query.Optimization;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.Query.Execution
{
/// <summary>
/// Abstract base class for literals.
/// </summary>
internal abstract class Literal : CodeGenFilterNode
{
    public Boolean InvolvesCodeExecution()
    {
        return false;
    }

    public override bool CanCodeGen {
        get { return true; }
    }

    // String representation of this instruction.
    protected override String CodeAsString()
    {
        throw new NotImplementedException("CodeAsString is not implemented for Literal");
    }

    // What stack changes caused by this instruction.
    protected override UInt32 StackChange()
    {
        return 1;
    }

    // Append this node to filter instructions and leaves.
    // Called statically so no need to worry about performance.
    // We redefine function here because literal is a leaf.
    public override UInt32 AppendToInstrAndLeavesList(List<CodeGenFilterNode> dataLeaves,
                                                      CodeGenFilterInstrArray instrArray,
                                                      Int32 currentExtent,
                                                      StringBuilder filterText)
    {
        if (filterText != null)
        {
            filterText.Append(InstrCode() + ": " + CodeAsString() + "\n");
        }

        UInt32 newInstrCode = InstrCode();
        if (instrArray != null)
        {
            instrArray.Add(newInstrCode);
        }
        
        // Since its a data leaf - adding it to the data leaves list.
        if (dataLeaves != null)
        {
            dataLeaves.Add(this);
        }
        return StackChange();
    }

    // Just need to overload without any implementation.
    public override void InstantiateExtentSet(ExtentSet extentSet)
    {
        // Used, that's why should simply be empty.
        // throw new NotImplementedException("InstantiateExtentSet is not implemented for Literal");
    }
    
    // Since literal's data is statically defined and doesn't change over time
    // we can pre-compute data buffer and append it to the key builder directly.
    public override void AppendToByteArray(FilterKeyBuilder key, IObjectView obj)
    {
        if (byteData == null)
        {
            // Literal's byte array should always be pre-computed
            // when literal is created, so throwing exception in this case.
            throw new NullReferenceException("Byte array has not been pre-computed for this literal.");
        }
        key.AppendPrecomputedBuffer(byteData);
    }

    /// <summary>
    /// Appends literal type to the node type list.
    /// </summary>
    /// <param name="nodeTypeList">List with condition nodes types.</param>
    public override void AddNodeTypeToList(List<ConditionNodeType> nodeTypeList)
    {
        nodeTypeList.Add(ConditionNodeType.Literal);
    }
}
}
