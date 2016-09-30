// ***********************************************************************
// <copyright file="ObjectThis.cs" company="Starcounter AB">
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
/// Class that holds information about the pseudo property "this",
/// which is a reference to the object itself.
/// </summary>
internal class ObjectThis : CodeGenFilterNode, IObjectExpression, IProperty
{
    ITypeBinding typeBinding;
    Int32 extentNumber;

    // Indicates if object comes from some previous extent.
    // By default treated as 'this object'.
    protected Boolean objFromPreviousExtent = false;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="extNum">The extent number to which this pseudo property belongs.</param>
    /// <param name="typeBind">The type binding of this pseudo property (object).</param>
    internal ObjectThis(Int32 extNum, ITypeBinding typeBind)
    {
        if (typeBind == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeBind.");
        }
        extentNumber = extNum;
        typeBinding = typeBind;
    }

    /// <summary>
    /// The extent number of the extent to which this pseudo property belongs.
    /// </summary>
    public Int32 ExtentNumber
    {
        get
        {
            return extentNumber;
        }
    }

    /// <summary>
    /// Name to be displayed for example as column header in a result grid.
    /// </summary>
    public String Name
    {
        get
        {
            return typeBinding.Name;
        }
    }

    /// <summary>
    /// Full path name to uniquely identify this pseudo property.
    /// </summary>
    public String FullName
    {
        get
        {
            return "this";
        }
    }

    public String ColumnName { get { return null; } }

    /// <summary>
    /// The DbTypeCode of this pseudo property (object).
    /// </summary>
    public override DbTypeCode DbTypeCode
    {
        get
        {
            return DbTypeCode.Object;
        }
    }

    public QueryTypeCode QueryTypeCode
    {
        get
        {
            return QueryTypeCode.Object;
        }
    }

    /// <summary>
    /// The type binding of this pseudo property (object).
    /// </summary>
    public ITypeBinding TypeBinding
    {
        get
        {
            return typeBinding;
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

    /// <summary>
    /// Calculates the value of this pseudo property when evaluated on an input object,
    /// which is the input object itself.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this pseudo property.</param>
    /// <returns>The value of the input object.</returns>
    public IObjectView EvaluateToObject(IObjectView obj)
    {
        if (obj == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect obj.");
        }
        if (obj is Row)
        {
            IObjectView partObj = (obj as Row).AccessObject(extentNumber);
            if (partObj == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "No elementary object at extent number: " + extentNumber);
            }
            if (partObj is NullObject)
                return null;
            return partObj;
        }
        return obj;
    }

    /// <summary>
    /// Examines if the value of the property is null when evaluated on an input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the property.</param>
    /// <returns>True, if the value of the property when evaluated on the input object
    /// is null, otherwise false.</returns>
    public override Boolean EvaluatesToNull(IObjectView obj)
    {
        return (EvaluateToObject(obj) == null);
    }

    /// <summary>
    /// Creates an more instantiated copy of this expression by evaluating it on a Row.
    /// Properties, with extent numbers for which there exist objects attached to the Row,
    /// are evaluated and instantiated to literals, other properties are not changed.
    /// </summary>
    /// <param name="obj">The Row on which to evaluate the expression.</param>
    /// <returns>A more instantiated expression.</returns>
    public IObjectExpression Instantiate(Row obj)
    {
        if (obj != null && extentNumber >= 0 && obj.AccessObject(extentNumber) != null)
        {
            return new ObjectLiteral(EvaluateToObject(obj));
        }
        return new ObjectThis(extentNumber, typeBinding);
    }

    public IValueExpression Clone(VariableArray varArray)
    {
        return CloneToObject(varArray);
    }

    public IObjectExpression CloneToObject(VariableArray varArray)
    {
        return new ObjectThis(extentNumber, typeBinding);
    }

    public override void InstantiateExtentSet(ExtentSet extentSet)
    {
        extentSet.AddExtentNumber(extentNumber);
    }

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
            objFromPreviousExtent = true;
            // Since its a data leaf (since coming from some previous extent),
            // adding it to the data leaves list.
            if (dataLeaves != null)
            {
                dataLeaves.Add(this);
            }
        }
        else // This is a regular property from the same extent.
        {
            objFromPreviousExtent = false;
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
        // Checking if its a property from some previous extent.
        if (objFromPreviousExtent)
        {
            // Returning code as for a data.
            return "LDV_REF_THIS";
        }

        throw new NotImplementedException("No CodeGen instruction for obtaining whole object reference.");

        // Returning code as for whole object reference.
        //return "LDA_SREF";
    }

    // Instruction code value.
    protected override UInt32 InstrCode()
    {
        // Checking if its an object from some previous extent.
        if (objFromPreviousExtent)
        {
            // Returning code as for a data.
            return CodeGenFilterInstrCodes.LDV_REF;
        }

        throw new NotImplementedException("No CodeGen instruction for obtaining whole object reference.");

        // Returning code as for whole object reference.
        //return CodeGenFilterInstrCodes.LDA_SREF;
    }

    /// <summary>
    /// Appends data of this leaf to the provided filter key.
    /// </summary>
    /// <param name="key">Reference to the filter key to which data should be appended.</param>
    /// <param name="obj">Row for which evaluation should be performed.</param>
    public override void AppendToByteArray(FilterKeyBuilder key, IObjectView obj)
    {
        // Checking if its an object from some previous extent
        // and if yes calculate its data (otherwise do nothing).
        if (objFromPreviousExtent)
        {
            key.Append(EvaluateToObject(obj));
        }
        else
        {
            throw new NotImplementedException("No CodeGen instruction for obtaining whole object reference.");
        }
    }

    // What stack changes caused by this instruction.
    protected override UInt32 StackChange()
    {
        return 1;
    }

    /// <summary>
    /// Builds a string presentation of this property using the input string-builder.
    /// </summary>
    /// <param name="stringBuilder">String-builder to use.</param>
    /// <param name="tabs">Number of tab indentations for the presentation.</param>
    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.Append(tabs, "ObjectThis(");
        stringBuilder.Append(extentNumber.ToString());
        stringBuilder.AppendLine(")");
    }

    /// <summary>
    /// Appends reference type to the node type list.
    /// </summary>
    /// <param name="nodeTypeList">List with condition nodes types.</param>
    public override void AddNodeTypeToList(List<ConditionNodeType> nodeTypeList)
    {
        nodeTypeList.Add(ConditionNodeType.ObjectThis);
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "GetThisObject();");
    }

#if DEBUG
    public bool AssertEquals(IValueExpression other) {
        ObjectThis otherNode = other as ObjectThis;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(ObjectThis other) {
        Debug.Assert(other != null);
        if (other == null)
            return false;
        // Check basic types
        Debug.Assert(this.typeBinding == other.typeBinding);
        if (this.typeBinding != other.typeBinding)
            return false;
        Debug.Assert(this.extentNumber == other.extentNumber);
        if (this.extentNumber != other.extentNumber)
            return false;
        Debug.Assert(this.objFromPreviousExtent == other.objFromPreviousExtent);
        if (this.objFromPreviousExtent != other.objFromPreviousExtent)
            return false;
        return true;
    }
#endif
}
}
