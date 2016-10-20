// ***********************************************************************
// <copyright file="BooleanMethod.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query.Optimization;
using Starcounter.Query.Sql;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using Starcounter.Binding;
using System.Diagnostics;

namespace Starcounter.Query.Execution
{
// At the moment only support of EqualsOrIsDerivedFrom-method is implemented.
/// <summary>
/// Class that holds information about a method with return type Boolean.
/// </summary>
internal class BooleanMethod : IBooleanPathItem, IMethod
{
    Int32 extentNumber;
    ITypeBinding typeBinding;
    IObjectExpression argumentExpr;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="extNum">The extent number to which this method belongs.
    /// If it does not belong to any extent number, which is the case for path expressions,
    /// then the number should be -1.</param>
    /// <param name="typeBind">The type rowTypeBind of the object to which this method belongs.</param>
    /// <param name="argument">The type rowTypeBind of the return object.</param>
    internal BooleanMethod(Int32 extNum, ITypeBinding typeBind, IObjectExpression argument)
    : base()
    {
        if (typeBind == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeBind.");
        }
        if (argument == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect argument.");
        }
        extentNumber = extNum;
        typeBinding = typeBind;
        argumentExpr = argument;
    }

    /// <summary>
    /// The extent number of the extent to which this method belongs.
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
            return ""; // TODO: Support method names for example in an IMethodBinding interface.
        }
    }

    /// <summary>
    /// Full name to uniquely identify the path.
    /// </summary>
    public String FullName
    {
        get
        {
            return ""; // TODO: Support method names for example in an IMethodBinding interface.
        }
    }

    public String ColumnName {
        get { return ""; }
    }

    /// <summary>
    /// The DbTypeCode of the return value of this method.
    /// </summary>
    public DbTypeCode DbTypeCode
    {
        get
        {
            return DbTypeCode.Boolean;
        }
    }

    public QueryTypeCode QueryTypeCode
    {
        get
        {
            return QueryTypeCode.Boolean;
        }
    }

    public Boolean InvolvesCodeExecution()
    {
        return true;
    }

    /// <summary>
    /// Calculates the return value of this method when evaluated on an input object.
    /// If the input object is not a Row then all member references in this expression (argExpression) should refer
    /// to the extent number (extentNumber) of this method and the input object should belong to the corresponding extent.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this method.</param>
    /// <returns>The return value of this method when evaluated on the input object.</returns>
    public Nullable<Boolean> EvaluateToBoolean(IObjectView obj)
    {
        return EvaluateToBoolean(obj, obj);
    }

    /// <summary>
    /// Calculates the return value of this method when evaluated on an input object and a start-object of the path to which
    /// the input object belongs.
    /// If the input object is not a Row then all member references in this expression (argExpression) should refer
    /// to the extent number (extentNumber) of this method and the input object should belong to the corresponding extent.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this method.</param>
    /// <param name="startObj">The start object of the path expression.</param>
    /// <returns>The return value of this method when evaluated on the input object and the start-object.</returns>
    public Nullable<Boolean> EvaluateToBoolean(IObjectView obj, IObjectView startObj)
    {
        if (obj == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect obj.");
        }
        if (startObj == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect startObj.");
        }
        if (obj is Row)
        {
            // Type control removed since type hierarchy and interfaces were not handled.
            // if ((obj.TypeBinding as RowTypeBinding).GetTypeBinding(extentNumber) == typeBinding)
            // {
            IObjectView partObj = (obj as Row).AccessObject(extentNumber);
            if (partObj == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "No elementary object at extent number: " + extentNumber);
            }
            IObjectView argObj = argumentExpr.EvaluateToObject(startObj);
            return partObj.EqualsOrIsDerivedFrom(argObj);
            // }
            // else
            //     throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Object of incorrect type: " + obj.TypeBinding);
        }
        else
        {
            // Type control removed since type hierarchy and interfaces were not handled.
            // if (obj.TypeBinding == typeBinding)
            // {
            IObjectView argObj = argumentExpr.EvaluateToObject(startObj);
            return obj.EqualsOrIsDerivedFrom(argObj);
            // }
            // else
            //     throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Object of incorrect type: " + obj.TypeBinding);
        }
    }

    /// <summary>
    /// Examines if the return value of the method is null when evaluated on an input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this method.</param>
    /// <returns>True, if the return value of this method when evaluated on the input object
    /// is null, otherwise false.</returns>
    public Boolean EvaluatesToNull(IObjectView obj)
    {
        return (EvaluateToBoolean(obj) == null);
    }

    /// <summary>
    /// Creates an more instantiated copy of this expression by evaluating it on a Row.
    /// Members, with extent numbers for which there exist objects attached to the Row,
    /// are evaluated and instantiated to literals, other members are not changed.
    /// </summary>
    /// <param name="obj">The Row on which to evaluate the expression.</param>
    /// <returns>A more instantiated expression.</returns>
    public IBooleanExpression Instantiate(Row obj)
    {
        IObjectExpression instArgumentExpr = argumentExpr.Instantiate(obj);
        if (obj != null && extentNumber >= 0 && obj.AccessObject(extentNumber) != null)
        {
            return new BooleanMethodLiteral(obj.AccessObject(extentNumber), instArgumentExpr);
        }
        else
        {
            return new BooleanMethod(extentNumber, typeBinding, instArgumentExpr);
        }
    }

    public IValueExpression Clone(VariableArray varArray)
    {
        return CloneToBoolean(varArray);
    }

    public IBooleanExpression CloneToBoolean(VariableArray varArray)
    {
        return new BooleanMethod(extentNumber, typeBinding, argumentExpr.CloneToObject(varArray));
    }

    public void InstantiateExtentSet(ExtentSet extentSet)
    {
        argumentExpr.InstantiateExtentSet(extentSet);
    }

    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "BooleanMethod(");
        stringBuilder.AppendLine(tabs + 1, extentNumber.ToString());
        stringBuilder.AppendLine(tabs + 1, "EqualsOrIsDerivedFrom(");
        argumentExpr.BuildString(stringBuilder, tabs + 2);
        stringBuilder.AppendLine(tabs + 1, ")");
        stringBuilder.AppendLine(tabs, ")");
    }

    // No implementation.
    public UInt32 AppendToInstrAndLeavesList(List<CodeGenFilterNode> dataLeaves, CodeGenFilterInstrArray instrArray, Int32 currentExtent, StringBuilder filterText)
    {
        throw new NotImplementedException("AppendToInstrAndLeavesList is not implemented for BooleanMethod");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "ObjectGenericMethod");
    }

#if DEBUG
    private bool AssertEqualsVisited = false;
    public bool AssertEquals(IValueExpression other) {
        BooleanMethod otherNode = other as BooleanMethod;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(BooleanMethod other) {
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
        Debug.Assert(this.typeBinding == other.typeBinding);
        if (this.typeBinding != other.typeBinding)
            return false;
        // Check references. This should be checked if there is cyclic reference.
        AssertEqualsVisited = true;
        bool areEquals = true;
        if (this.argumentExpr == null) {
            Debug.Assert(other.argumentExpr == null);
            areEquals = other.argumentExpr == null;
        } else
            areEquals = this.argumentExpr.AssertEquals(other.argumentExpr);
        AssertEqualsVisited = false;
        return areEquals;
    }
#endif
}
}
