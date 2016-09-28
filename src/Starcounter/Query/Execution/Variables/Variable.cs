// ***********************************************************************
// <copyright file="Variable.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Query.Optimization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Diagnostics;
using Starcounter.Binding;

namespace Starcounter.Query.Execution
{
/// <summary>
/// Abstract base class for variables.
/// </summary>
internal abstract class Variable : CodeGenFilterNode
{
    // Represents position of this variable in variables array.
    protected Int32 number;

    public Int32 Number
    {
        get
        {
            return number;
        }
    }

    /// <summary>
    /// Always true
    /// </summary>
    public override bool CanCodeGen {
        get {
                return true;
        }
    }

    public Boolean InvolvesCodeExecution()
    {
        return false;
    }

    public abstract void SetValue(Object newValue);

    public abstract void SetNullValue();

    public virtual void SetValue(Binary newValue)
    {
        throw ErrorCode.ToException(Error.SCERRBADARGUMENTS);
    }

    public virtual void SetValue(Byte[] newValue)
    {
        throw ErrorCode.ToException(Error.SCERRBADARGUMENTS);
    }

    public virtual void SetValue(Boolean newValue)
    {
        throw ErrorCode.ToException(Error.SCERRBADARGUMENTS);
    }

    public virtual void SetValue(DateTime newValue)
    {
        throw ErrorCode.ToException(Error.SCERRBADARGUMENTS);
    }

    public virtual void SetValue(Decimal newValue)
    {
        throw ErrorCode.ToException(Error.SCERRBADARGUMENTS);
    }

    public virtual void SetValue(Double newValue)
    {
        throw ErrorCode.ToException(Error.SCERRBADARGUMENTS);
    }

    public virtual void SetValue(Single newValue)
    {
        throw ErrorCode.ToException(Error.SCERRBADARGUMENTS);
    }

    public virtual void SetValue(Int64 newValue)
    {
        throw ErrorCode.ToException(Error.SCERRBADARGUMENTS);
    }

    public virtual void SetValue(Int32 newValue)
    {
        throw ErrorCode.ToException(Error.SCERRBADARGUMENTS);
    }

    public virtual void SetValue(Int16 newValue)
    {
        throw ErrorCode.ToException(Error.SCERRBADARGUMENTS);
    }

    public virtual void SetValue(SByte newValue)
    {
        throw ErrorCode.ToException(Error.SCERRBADARGUMENTS);
    }

    public virtual void SetValue(IObjectView newValue)
    {
        throw ErrorCode.ToException(Error.SCERRBADARGUMENTS);
    }

    public virtual void SetValue(String newValue)
    {
        throw ErrorCode.ToException(Error.SCERRBADARGUMENTS);
    }

    public virtual void SetValue(UInt64 newValue)
    {
        throw ErrorCode.ToException(Error.SCERRBADARGUMENTS);
    }

    public virtual void SetValue(UInt32 newValue)
    {
        throw ErrorCode.ToException(Error.SCERRBADARGUMENTS);
    }

    public virtual void SetValue(UInt16 newValue)
    {
        throw ErrorCode.ToException(Error.SCERRBADARGUMENTS);
    }

    public virtual void SetValue(Byte newValue)
    {
        throw ErrorCode.ToException(Error.SCERRBADARGUMENTS);
    }

    public virtual void SetValue(Type newValue) {
        throw ErrorCode.ToException(Error.SCERRBADARGUMENTS);
    }

    public virtual void SetValue(ITypeBinding newValue) {
        throw ErrorCode.ToException(Error.SCERRBADARGUMENTS);
    }

    /// <summary>
    /// Generic clone for ITypeExpression types.
    /// </summary>
    /// <param name="varArray">Variables array.</param>
    /// <returns>Clone of the expression.</returns>
    public virtual IValueExpression Clone(VariableArray varArray)
    {
        throw ErrorCode.ToException(Error.SCERRNOTSUPPORTED);
    }
    
    // String representation of this instruction.
    protected override String CodeAsString()
    {
        throw ErrorCode.ToException(Error.SCERRNOTSUPPORTED);
    }

    // What stack changes caused by this instruction.
    protected override UInt32 StackChange()
    {
        return 1;
    }

    // Append this node to filter instructions and leaves.
    // Called statically so no need to worry about performance.
    // We redefine function here because variable is a leaf.
    public override UInt32 AppendToInstrAndLeavesList(List<CodeGenFilterNode> dataLeaves,
                                                      CodeGenFilterInstrArray instrArray,
                                                      Int32 currentExtent,
                                                      StringBuilder filterText)
    {
        UInt32 newInstrCode = InstrCode();
        if (instrArray != null)
        {
            instrArray.Add(newInstrCode);
        }

        if (filterText != null)
        {
            filterText.Append(InstrCode() + ": " + CodeAsString() + "\n");
        }
        
        // Since its a data leaf - adding it to the data leaves list.
        if (dataLeaves != null)
        {
            dataLeaves.Add(this);
        }
        
        return StackChange();
    }

    /// <summary>
    /// Appends variable type to the node type list.
    /// </summary>
    /// <param name="nodeTypeList">List with condition nodes types.</param>
    public override void AddNodeTypeToList(List<ConditionNodeType> nodeTypeList)
    {
        nodeTypeList.Add(ConditionNodeType.Variable);
    }

#if DEBUG
    internal bool AssertEquals(Variable other) {
        Debug.Assert(other != null);
        if (other == null)
            return false;
        // Check basic types
        Debug.Assert(this.number == other.number);
        if (this.number != other.number)
            return false;
        return true;
    }
#endif
}
}
