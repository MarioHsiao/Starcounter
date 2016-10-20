// ***********************************************************************
// <copyright file="Casts.cs" company="Starcounter AB">
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
internal class ObjectCast : IObjectPathItem, IPath
{
    ITypeBinding typeBinding;
    IObjectExpression expression;

    internal ObjectCast(ITypeBinding typeBind, IObjectExpression expr)
    {
        if (typeBind == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeBind.");
        }
        if (expr == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect logExpr.");
        }
        typeBinding = typeBind;
        expression = expr;
    }

    /// <summary>
    /// The extent number of the extent to which this cast belongs.
    /// </summary>
    public Int32 ExtentNumber
    {
        get
        {
            if (expression is IPath)
            {
                return (expression as IPath).ExtentNumber;
            }
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
            if (expression is IPath)
            {
                return (expression as IPath).Name;
            }
            return "";
        }
    }

    /// <summary>
    /// Full name to uniquely identify the path.
    /// </summary>
    public String FullName
    {
        get
        {
            if (expression is IPath)
            {
                return "(" + (expression as IPath).FullName + " as " + typeBinding.Name + ")";
            }
            return "";
        }
    }

    public String ColumnName {
        get {
            if (expression is IPath)
                return (expression as IPath).ColumnName;
            else return null;
        }
    }

    /// <summary>
    /// The DbTypeCode of the casted path-item.
    /// </summary>
    public DbTypeCode DbTypeCode
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

    public Boolean InvolvesCodeExecution()
    {
        return expression.InvolvesCodeExecution();
    }

    /// <summary>
    /// The type binding of the casted path-item.
    /// </summary>
    public ITypeBinding TypeBinding
    {
        get
        {
            return typeBinding;
        }
    }

    /// <summary>
    /// Calculates the value of the cast operation when evaluated on an input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the cast operation.</param>
    /// <returns>The value of the cast operation evaluated on the input object.</returns>
    public IObjectView EvaluateToObject(IObjectView obj)
    {
        IObjectView value = expression.EvaluateToObject(obj);
        if (value != null && (value.TypeBinding is TypeBinding) && (value.TypeBinding as TypeBinding).SubTypeOf(typeBinding as TypeBinding))
        {
            return value;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Calculates the value of the cast operation when evaluated on an input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the cast operation.</param>
    /// <param name="startObj">The start object of the current path expression.</param>
    /// <returns>The value of the cast operation evaluated on the input object.</returns>
    public IObjectView EvaluateToObject(IObjectView obj, IObjectView startObj)
    {
        return EvaluateToObject(obj);
    }

    /// <summary>
    /// Examines if the value of the cast operation is null when evaluated on an input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the cast operation.</param>
    /// <returns>True, if the value of the cast operation when evaluated on the input object
    /// is null, otherwise false.</returns>
    public Boolean EvaluatesToNull(IObjectView obj)
    {
        return (EvaluateToObject(obj) == null);
    }

    /// <summary>
    /// Creates an more instantiated copy of this expression by evaluating it on a Row.
    /// </summary>
    /// <param name="obj">The Row on which to evaluate the expression.</param>
    /// <returns>A more instantiated expression.</returns>
    public IObjectExpression Instantiate_OLD(Row obj)
    {
        IObjectExpression instExpression = expression.Instantiate(obj);

        if (instExpression is ObjectLiteral)
        {
            IObjectView value = instExpression.EvaluateToObject(null);
            if (value != null && (value.TypeBinding is TypeBinding) && (value.TypeBinding as TypeBinding).SubTypeOf(typeBinding as TypeBinding))
            {
                return new ObjectLiteral(value);
            }
            else
            {
                return new ObjectLiteral(typeBinding);
            }
        }

        return new ObjectCast(typeBinding, instExpression);
    }

    public IObjectExpression Instantiate(Row obj)
    {
        return new ObjectCast(typeBinding, expression.Instantiate(obj));
    }

    public IValueExpression Clone(VariableArray varArray)
    {
        return CloneToObject(varArray);
    }

    public IObjectExpression CloneToObject(VariableArray varArray)
    {
        return new ObjectCast(typeBinding, expression.CloneToObject(varArray));
    }

    public void InstantiateExtentSet(ExtentSet extentSet)
    {
        expression.InstantiateExtentSet(extentSet);
    }

    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "ObjectCast(");
        expression.BuildString(stringBuilder, tabs + 1);
        stringBuilder.AppendLine(tabs + 1, typeBinding.Name);
        stringBuilder.AppendLine(tabs, ")");
    }

    // No implementation.
    public UInt32 AppendToInstrAndLeavesList(List<CodeGenFilterNode> dataLeaves, CodeGenFilterInstrArray instrArray, Int32 currentExtent, StringBuilder filterText)
    {
        throw new NotImplementedException("AppendToInstrAndLeavesList is not implemented for Casts");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        expression.GenerateCompilableCode(stringGen);
    }

#if DEBUG
    private bool AssertEqualsVisited = false;
    public bool AssertEquals(IValueExpression other) {
        ObjectCast otherNode = other as ObjectCast;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(ObjectCast other) {
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
        if (this.expression == null) {
            Debug.Assert(other.expression == null);
            areEquals = other.expression == null;
        } else
            areEquals = this.expression.AssertEquals(other.expression);
        AssertEqualsVisited = false;
        return areEquals;
    }
#endif
}
}
