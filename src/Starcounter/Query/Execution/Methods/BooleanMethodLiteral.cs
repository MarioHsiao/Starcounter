// ***********************************************************************
// <copyright file="BooleanMethodLiteral.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query.Sql;
using System;
using System.Collections.Generic;
using Starcounter.Binding;
using System.Diagnostics;


namespace Starcounter.Query.Execution
{
// At the moment only support of EqualsOrIsDerivedFrom-method is implemented.
/// <summary>
/// Class that holds information about a method literal with return type Boolean.
/// An method literal is a method where the extent number is replaced by a specific object.
/// </summary>
internal class BooleanMethodLiteral : Literal, ILiteral, IBooleanPathItem, IMethod
{
    IObjectView value;
    ITypeBinding typeBinding;
    IObjectExpression argumentExpr;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="value">The specific object to which this method belongs.</param>
    /// <param name="argument">The argument to this method.</param>
    internal BooleanMethodLiteral(IObjectView value, IObjectExpression argument)
    : base()
    {
        if (value == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect value.");
        }
        if (argument == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect argument.");
        }
        this.value = value;
        typeBinding = value.TypeBinding;
        argumentExpr = argument;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="typeBind">The type binding of this null-method-literal.</param>
    /// <param name="argument">The argument to this method.</param>
    internal BooleanMethodLiteral(ITypeBinding typeBind, IObjectExpression argument)
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
        value = null;
        typeBinding = typeBind;
        argumentExpr = argument;
    }

    /// <summary>
    /// The extent number of the extent to which this method-literal belongs.
    /// </summary>
    public Int32 ExtentNumber
    {
        get
        {
            return -1;
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
    public override DbTypeCode DbTypeCode
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

    /// <summary>
    /// Appends data of this leaf to the provided filter key.
    /// </summary>
    /// <param name="key">Reference to the filter key to which data should be appended.</param>
    /// <param name="obj">Row for which evaluation should be performed.</param>
    public override void AppendToByteArray(FilterKeyBuilder key, IObjectView obj)
    {
        key.Append(EvaluateToBoolean(obj));
    }

    /// <summary>
    /// Calculates the return value of this method when the arguments are evaluated on an input object.
    /// If the input object is not a Row then all member references in this expression (argExpression) should refer
    /// to the extent number (extentNumber) of this method and the input object should belong to the corresponding extent.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the argument of this method.</param>
    /// <returns>The return value of this method when the arguments are evaluated on the input object.</returns>
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
    /// <param name="obj">Not used.</param>
    /// <param name="startObj">The start object of the path expression.</param>
    /// <returns>The return value of this method when the arguments are evaluated on the start-object.</returns>
    public Nullable<Boolean> EvaluateToBoolean(IObjectView obj, IObjectView startObj)
    {
        if (startObj == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect startObj.");
        }
        if (value != null)
        {
            IObjectView argObj = argumentExpr.EvaluateToObject(startObj);
            return value.EqualsOrIsDerivedFrom(argObj);
        }
        else
        {
            return null;
        }
    }

    public String EvaluateToString() {
        return EvaluateToBoolean(null).ToString();
    }

    /// <summary>
    /// Examines if the return value of the method is null when evaluated on an input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this method.</param>
    /// <returns>True, if the return value of this method when evaluated on the input object
    /// is null, otherwise false.</returns>
    public override Boolean EvaluatesToNull(IObjectView obj)
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
        if (value != null)
        {
            return new BooleanMethodLiteral(value, instArgumentExpr);
        }
        else
        {
            return new BooleanMethodLiteral(typeBinding, instArgumentExpr);
        }
    }

    public IValueExpression Clone(VariableArray varArray)
    {
        return CloneToBoolean(varArray);
    }

    public IBooleanExpression CloneToBoolean(VariableArray varArray)
    {
        if (value != null)
        {
            return new BooleanMethodLiteral(value, argumentExpr.CloneToObject(varArray));
        }
        else
        {
            return new BooleanMethodLiteral(typeBinding, argumentExpr.CloneToObject(varArray));
        }
    }

    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "BooleanMethodLiteral(");
        if (value != null)
        {
            stringBuilder.AppendLine(tabs + 1, value.ToString());
        }
        else
        {
            stringBuilder.AppendLine(tabs + 1, Starcounter.Db.NullString);
        }
        stringBuilder.AppendLine(tabs + 1, "EqualsOrIsDerivedFrom");
        argumentExpr.BuildString(stringBuilder, tabs + 1);
        stringBuilder.AppendLine(tabs, ")");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, value.ToString());
    }

#if DEBUG
    private bool AssertEqualsVisited = false;
    public bool AssertEquals(IValueExpression other) {
        BooleanMethodLiteral otherNode = other as BooleanMethodLiteral;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(BooleanMethodLiteral other) {
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
        Debug.Assert(this.typeBinding == other.typeBinding);
        if (this.typeBinding != other.typeBinding)
            return false;
        // Check references. This should be checked if there is cyclic reference.
        AssertEqualsVisited = true;
        bool areEquals = true;
        if (this.value == null) {
            Debug.Assert(other.value == null);
            areEquals = other.value == null;
        } else
            areEquals = this.value.AssertEquals(other.value);
        if (areEquals)
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
