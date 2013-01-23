// ***********************************************************************
// <copyright file="PropertyMapping.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Binding;
using System.Diagnostics;

namespace Starcounter.Query.Execution
{
internal class PropertyMapping : IPropertyBinding
{
    ITypeBinding typeBinding;

    String refName; // Unique name within the type.
    Int32 propIndex;
    IValueExpression expression;

    internal PropertyMapping(String name, Int32 index, IValueExpression expr)
    : base()
    {
        if (name == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect name.");
        }
        if (expr == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect logExpr.");
        }
        refName = name;
        propIndex = index;
        expression = expr;
        if (expr is IObjectExpression)
        {
            typeBinding = (expr as IObjectExpression).TypeBinding;
        }
        else
        {
            typeBinding = null;
        }
    }

    internal IValueExpression Expression
    {
        get
        {
            return expression;
        }
    }

    public Int32 Index
    {
        get
        {
            return propIndex;
        }
    }

    public String Name
    {
        get
        {
            return refName;
        }
    }

    /// <summary>
    /// Name to be displayed for example as column header in a result grid.
    /// </summary>
    public String DisplayName
    {
        get
        {
            Char[] charArr = refName.ToCharArray();
            if (charArr.Length > 0 && Char.IsDigit(charArr[0]) && expression is IPath)
            {
                return (expression as IPath).Name;
            }
            return refName;
        }
    }

    public ITypeBinding TypeBinding
    {
        get
        {
            return typeBinding;
        }
    }

    public DbTypeCode TypeCode
    {
        get
        {
            return expression.DbTypeCode;
        }
    }

    internal void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.Append(tabs, refName);
        stringBuilder.AppendLine(" = ");
        expression.BuildString(stringBuilder, tabs + 1);
    }

#if DEBUG
    private bool AssertEqualsVisited = false;
    public bool AssertEquals(IPropertyBinding other) {
        PropertyMapping otherNode = other as PropertyMapping;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(PropertyMapping other) {
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
        Debug.Assert(this.refName == other.refName);
        if (this.refName != other.refName)
            return false;
        Debug.Assert(this.propIndex == other.propIndex);
        if (this.propIndex != other.propIndex)
            return false;
        Debug.Assert(this.typeBinding == other.typeBinding);
        if (this.typeBinding != other.typeBinding)
            return false;
        // Check references. This should be checked if there is cyclic reference.
        AssertEqualsVisited = true;
        bool areEquals = this.expression.AssertEquals(other.expression);
        AssertEqualsVisited = false;
        return areEquals;
    }
#endif
}
}
