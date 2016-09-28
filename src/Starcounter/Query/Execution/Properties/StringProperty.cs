// ***********************************************************************
// <copyright file="StringProperty.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Query.Optimization;
using System;
using Starcounter.Binding;


namespace Starcounter.Query.Execution
{
/// <summary>
/// Class that holds information about a property of type String.
/// </summary>
internal class StringProperty : Property, IStringPathItem
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="extNum">The extent number to which this property belongs.
    /// If it does not belong to any extent number, which is the case for path expressions,
    /// then the number should be -1.</param>
    /// <param name="typeBind">The type binding of the object to which this property belongs.</param>
    /// <param name="propBind">The property binding of this property.</param>
    internal StringProperty(Int32 extNum, ITypeBinding typeBind, IPropertyBinding propBind)
    {
        if (typeBind == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeBind.");
        }
        if (propBind == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect propBind.");
        }
        if (propBind.TypeCode != DbTypeCode.String)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Property of incorrect type.");
        }
        extentNumber = extNum;
        typeBinding = typeBind;
        propBinding = propBind;
        propIndex = propBind.Index;
    }

    /// <summary>
    /// 
    /// </summary>
    public QueryTypeCode QueryTypeCode
    {
        get
        {
            return QueryTypeCode.String;
        }
    }

    // String representation of this instruction.
    protected override String CodeAsString()
    {
        // Checking if its a property from some previous extent.
        if (propFromPreviousExtent)
        {
            // Returning code as for a data.
            return "LDV_STR";
        }
        // Returning code as for a property.
        return "LDA_STR " + DataIndex;
    }

    // Instruction code value.
    protected override UInt32 InstrCode()
    {
        // Checking if its a property from some previous extent.
        if (propFromPreviousExtent)
        {
            // Returning code as for a data.
            return CodeGenFilterInstrCodes.LDV_STR;
        }
        // Returning code as for a property.
        return CodeGenFilterInstrCodes.LDA_STR | ((UInt32) DataIndex << 8);
    }

    /// <summary>
    /// Appends data of this leaf to the provided filter key.
    /// </summary>
    /// <param name="key">Reference to the filter key to which data should be appended.</param>
    /// <param name="obj">Row for which evaluation should be performed.</param>
    public override void AppendToByteArray(FilterKeyBuilder key, IObjectView obj)
    {
        // Checking if its a property from some previous extent
        // and if yes calculate its data (otherwise do nothing).
        if (propFromPreviousExtent)
        {
            key.Append(EvaluateToString(obj), false);
        }
    }
        
    /// <summary>
    /// Calculates the value of this property when evaluated on an input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this property.</param>
    /// <returns>The value of this property when evaluated on the input object.</returns>
    public String EvaluateToString(IObjectView obj)
    {
        if (obj == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect obj.");
        }
        if (obj is Row)
        {
            // Control that the type ((obj.TypeBinding as RowTypeBinding).GetTypeBinding(extentNumber)) of the input object
            // is equal to or a subtype (TypeBinding.SubTypeOf(TypeBinding)) of the type (typeBinding) to which this property belongs
            // is not implemented due to that interfaces cannot be handled and computational cost.
            IObjectView partObj = (obj as Row).AccessObject(extentNumber);
            if (partObj == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "No elementary object at extent number: " + extentNumber);
            }
            return partObj.GetString(propIndex);
        }
        // Control that the type (obj.TypeBinding) of the input object
        // is equal to or a subtype (TypeBinding.SubTypeOf(TypeBinding)) of the type (typeBinding) to which this property belongs
        // is not implemented due to that interfaces cannot be handled and computational cost.
        return obj.GetString(propIndex);
    }

    /// <summary>
    /// Calculates the value of the path-item when evaluated on an input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the expression.</param>
    /// <param name="startObj">The start object of the current path expression.</param>
    /// <returns>The value of the expression when evaluated on the input object.</returns>
    public String EvaluateToString(IObjectView obj, IObjectView startObj)
    {
        return EvaluateToString(obj);
    }

    /// <summary>
    /// Examines if the value of the property is null when evaluated on an input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the property.</param>
    /// <returns>True, if the value of the property when evaluated on the input object
    /// is null, otherwise false.</returns>
    public override Boolean EvaluatesToNull(IObjectView obj)
    {
        return (EvaluateToString(obj) == null);
    }

    /// <summary>
    /// Creates an more instantiated copy of this expression by evaluating it on a Row.
    /// Properties, with extent numbers for which there exist objects attached to the Row,
    /// are evaluated and instantiated to literals, other properties are not changed.
    /// </summary>
    /// <param name="obj">The Row on which to evaluate the expression.</param>
    /// <returns>A more instantiated expression.</returns>
    public IStringExpression Instantiate(Row obj)
    {
        if (obj != null && extentNumber >= 0 && obj.AccessObject(extentNumber) != null)
        {
            return new StringLiteral(EvaluateToString(obj));
        }
        return new StringProperty(extentNumber, typeBinding, propBinding);
    }

    public override IValueExpression Clone(VariableArray varArray)
    {
        return CloneToString(varArray);
    }
        
    public IStringExpression CloneToString(VariableArray varArray)
    {
        return new StringProperty(extentNumber, typeBinding, propBinding);
    }

    /// <summary>
    /// Builds a string presentation of this property using the input string-builder.
    /// </summary>
    /// <param name="stringBuilder">String-builder to use.</param>
    /// <param name="tabs">Number of tab indentations for the presentation.</param>
    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.Append(tabs, "StringProperty(");
        stringBuilder.Append(extentNumber.ToString());
        stringBuilder.Append(", ");
        stringBuilder.Append(propBinding.Name);
        stringBuilder.AppendLine(")");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public override void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "GetStringProperty();");
    }
}
}
