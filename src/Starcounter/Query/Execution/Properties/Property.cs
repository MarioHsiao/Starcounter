// ***********************************************************************
// <copyright file="Property.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Query.Optimization;
using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Binding;
using System.Diagnostics;

namespace Starcounter.Query.Execution
{
/// <summary>
/// Abstract base class for properties.
/// </summary>
internal abstract class Property : CodeGenFilterNode, IProperty
{
    // Specific property properties.
    protected Int32 extentNumber;
    protected ITypeBinding typeBinding;
    protected IPropertyBinding propBinding;
    protected Int32 propIndex;
    
    // Indicates if property comes from the other extent.
    // By default treated as a normal property.
    protected Boolean propFromPreviousExtent = false;

    /// <summary>
    /// The extent number of the extent to which this property belongs.
    /// </summary>
    public Int32 ExtentNumber
    {
        get
        {
            return extentNumber;
        }
    }

    public override bool CanCodeGen {
        get {
            if (propBinding is PropertyBinding)
                return (propBinding as PropertyBinding).GetDataIndex() > -1;
            else return false;
        }
    }

    public Boolean InvolvesCodeExecution()
    {
        return (propIndex == -1);
    }

    /// <summary>
    /// Generic clone for ITypeExpression types.
    /// </summary>
    /// <param name="varArray">Variables array.</param>
    /// <returns>Clone of the expression.</returns>
    public abstract IValueExpression Clone(VariableArray varArray);

    // Append this node to filter instructions and leaves.
    // Called statically so no need to worry about performance.
    public override UInt32 AppendToInstrAndLeavesList(List<CodeGenFilterNode> dataLeaves,
                                                      CodeGenFilterInstrArray instrArray,
                                                      Int32 currentExtent,
                                                      StringBuilder filterText)
    {
        // If property comes from some previous extent treating it as a variable.
        if (currentExtent != extentNumber)
        {
            propFromPreviousExtent = true;
            // Since its a data leaf (since coming from some previous extent),
            // adding it to the data leaves list.
            if (dataLeaves != null)
            {
                dataLeaves.Add(this);
            }
        }
        else // This is a regular property from the same extent.
        {
            propFromPreviousExtent = false;
        }

        UInt32 newInstrCode = InstrCode();
        if (instrArray != null)
        {
            instrArray.Add(newInstrCode);
        }

        // Appending property string representation.
        if (filterText != null)
        {
            filterText.Append(InstrCode() + ": " + CodeAsString() + "\n");
        }

        return StackChange();
    }

    // String representation of this instruction.
    protected override String CodeAsString()
    {
        throw new NotImplementedException("CodeAsString is not implemented for Property");
    }

    // What stack changes caused by this instruction.
    protected override UInt32 StackChange()
    {
        return 1;
    }

    /// <summary>
    /// Name to be displayed for example as column header in a result grid.
    /// </summary>
    public virtual String Name
    {
        get
        {
            return propBinding.Name;
        }
    }

    /// <summary>
    /// Full path name to uniquely identify this property.
    /// </summary>
    public virtual String FullName
    {
        get
        {
            return propBinding.Name;
        }
    }

    public virtual String ColumnName {
        get {
            if (typeBinding is TypeBinding && propBinding != null)
                return (typeBinding as TypeBinding).TypeDef.PropertyDefs[propBinding.Index].ColumnName;
            else return null;
        }
    }

    /// <summary>
    /// The DbTypeCode of this property.
    /// </summary>
    public override DbTypeCode DbTypeCode
    {
        get
        {
            return propBinding.TypeCode;
        }
    }
    
    /// <summary>
    /// The index of this property within the type.
    /// </summary>
    public Int32 PropertyIndex
    {
        get
        {
            return propIndex;
        }
    }

    /// <summary>
    /// Gets the data index of this property
    /// </summary>
    internal Int32 DataIndex
    {
        get
        {
            PropertyBinding pb = propBinding as PropertyBinding;
            if (pb != null)
            {
                return pb.GetDataIndex();
            }
            return -1;
        }
    }

    public override void InstantiateExtentSet(ExtentSet extentSet)
    {
        extentSet.AddExtentNumber(extentNumber);
    }

    /// <summary>
    /// Appends property type to the node type list.
    /// </summary>
    /// <param name="nodeTypeList">List with condition nodes types.</param>
    public override void AddNodeTypeToList(List<ConditionNodeType> nodeTypeList)
    {
        nodeTypeList.Add(ConditionNodeType.Property);
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    virtual public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "GetProperty();");
    }

#if DEBUG
    public bool AssertEquals(IValueExpression other) {
        Property otherNode = other as Property;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(Property other) {
        Debug.Assert(other != null);
        if (other == null)
            return false;
        // Check basic types
        Debug.Assert(this.propIndex == other.propIndex);
        if (this.propIndex != other.propIndex)
            return false;
        Debug.Assert(this.extentNumber == other.extentNumber);
        if (this.extentNumber != other.extentNumber)
            return false;
        Debug.Assert(this.typeBinding == other.typeBinding);
        if (this.typeBinding != other.typeBinding)
            return false;
        Debug.Assert(this.propBinding == other.propBinding);
        if (this.propBinding != other.propBinding)
            return false;
        Debug.Assert(this.propFromPreviousExtent == other.propFromPreviousExtent);
        if (this.propFromPreviousExtent != other.propFromPreviousExtent)
            return false;
        return true;
    }
#endif
}
}
