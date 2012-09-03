
using Starcounter;
using Sc.Server.Binding;
using Sc.Server.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Binding;

namespace Starcounter.Query.Execution
{
internal class PropertyMapping : IPropertyBinding
{
    ITypeBinding typeBinding;

    String refName; // Unique name within the type.
    Int32 propIndex;
    ITypeExpression expression;

    internal PropertyMapping(String name, Int32 index, ITypeExpression expr)
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

    internal ITypeExpression Expression
    {
        get
        {
            return expression;
        }
    }

    public Int32 AccessCost
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public Int32 Index
    {
        get
        {
            return propIndex;
        }
    }

    public IPropertyBinding InversePropertyBinding
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public Int32 MutateCost
    {
        get
        {
            throw new NotImplementedException();
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
}
}
