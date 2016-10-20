// ***********************************************************************
// <copyright file="UIntegerPath.cs" company="Starcounter AB">
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
/// Class that holds information about a path of type unsigned integer.
/// </summary>
internal class UIntegerPath : Path, IUIntegerExpression, IPath
{
    IUIntegerPathItem member;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="extNum">The extent number to which this path belongs.</param>
    /// <param name="path">A currentLogExprList of object expressions (references) which constitutes
    /// a path.</param>
    /// <param name="member">The end member of this path.</param>
    internal UIntegerPath(Int32 extNum, List<IObjectPathItem> path, IUIntegerPathItem member)
    {
        if (path == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect path.");
        }
        if (member == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect member.");
        }
        extentNumber = extNum;
        pathList = path;
        this.member = member;
    }

    /// <summary>
    /// Name to be displayed for example as column header in a result grid.
    /// </summary>
    public String Name
    {
        get
        {
            if (member is IMember)
            {
                return (member as IMember).Name;
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect member (Name).");
        }
    }

    /// <summary>
    /// Full path name to uniquely identify this path.
    /// </summary>
    public String FullName
    {
        get
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (Int32 i = 0; i < pathList.Count; i++)
            {
                stringBuilder.Append(pathList[i].Name + ".");
            }
            stringBuilder.Append(Name);
            return stringBuilder.ToString();
        }
    }

    public String ColumnName {
        get {
            if (member is IMember)
                return (member as IMember).ColumnName;
            else return null;
        }
    }

    /// <summary>
    /// The DbTypeCode of (the end member of) this path.
    /// </summary>
    public override DbTypeCode DbTypeCode
    {
        get
        {
            return member.DbTypeCode;
        }
    }

    public QueryTypeCode QueryTypeCode
    {
        get
        {
            return QueryTypeCode.UInteger;
        }
    }

    public override bool CanCodeGen {
        get {
            if (member is CodeGenFilterNode)
                return (member as CodeGenFilterNode).CanCodeGen;
            else
                return false;
        }
    }

    public Boolean InvolvesCodeExecution()
    {
        Boolean codeExecution = member.InvolvesCodeExecution();
        Int32 i = 0;
        while (codeExecution == false && i < pathList.Count)
        {
            codeExecution = pathList[i].InvolvesCodeExecution();
            i++;
        }
        return codeExecution;
    }

    /// <summary>
    /// Appends data of this leaf to the provided filter key.
    /// </summary>
    /// <param name="key">Reference to the filter key to which data should be appended.</param>
    /// <param name="obj">Row for which evaluation should be performed.</param>
    public override void AppendToByteArray(FilterKeyBuilder key, IObjectView obj)
    {
        key.Append(EvaluateToUInteger(obj));
    }

    /// <summary>
    /// Calculates the value of this path when evaluated on an input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this path.</param>
    /// <returns>The value of this path when evaluated on the input object.</returns>
    public Nullable<UInt64> EvaluateToUInteger(IObjectView obj)
    {
        if (obj == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect obj.");
        }
        IObjectView startObj = obj;
        IObjectView cursorObj = obj;
        Int32 i = 0;
        while (i < pathList.Count)
        {
            if (cursorObj != null)
            {
                cursorObj = pathList[i].EvaluateToObject(cursorObj, startObj);
            }
            i++;
        }
        if (cursorObj != null)
        {
            return member.EvaluateToUInteger(cursorObj, startObj);
        }
        return null;
    }

    /// <summary>
    /// Calculates the value of this path as a nullable Decimal.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this path.</returns>
    public Nullable<Decimal> EvaluateToDecimal(IObjectView obj)
    {
        return EvaluateToUInteger(obj);
    }

    /// <summary>
    /// Calculates the value of this path as a nullable Double.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this path.</returns>
    public Nullable<Double> EvaluateToDouble(IObjectView obj)
    {
        return EvaluateToUInteger(obj);
    }

    /// <summary>
    /// Calculates the value of this path as a nullable Int64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this path.</returns>
    public Nullable<Int64> EvaluateToInteger(IObjectView obj)
    {
        Nullable<UInt64> value = EvaluateToUInteger(obj);
        if (value == null)
        {
            return null;
        }
        if (value.Value > Int64.MaxValue)
        {
            return null;
        }
        return (Int64)value.Value;
    }

    /// <summary>
    /// Calculates the value of this path as a ceiling (round up) nullable Int64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this path.</returns>
    public Nullable<Int64> EvaluateToIntegerCeiling(IObjectView obj)
    {
        Nullable<UInt64> value = EvaluateToUInteger(obj);
        if (value == null)
        {
            return null;
        }
        if (value.Value > Int64.MaxValue)
        {
            return null;
        }
        return (Int64)value.Value;
    }

    /// <summary>
    /// Calculates the value of this path as a floor (round down) nullable Int64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this path.</returns>
    public Nullable<Int64> EvaluateToIntegerFloor(IObjectView obj)
    {
        Nullable<UInt64> value = EvaluateToUInteger(obj);
        if (value == null)
        {
            return null;
        }
        if (value.Value > Int64.MaxValue)
        {
            return Int64.MaxValue;
        }
        return (Int64)value.Value;
    }

    /// <summary>
    /// Calculates the value of this path as a ceiling (round up) nullable UInt64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this path.</returns>
    public Nullable<UInt64> EvaluateToUIntegerCeiling(IObjectView obj)
    {
        return EvaluateToUInteger(obj);
    }

    /// <summary>
    /// Calculates the value of this path as a floor (round down) nullable UInt64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this path.</returns>
    public Nullable<UInt64> EvaluateToUIntegerFloor(IObjectView obj)
    {
        return EvaluateToUInteger(obj);
    }

    /// <summary>
    /// Examines if the value of the path is null when evaluated on an input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the path.</param>
    /// <returns>True, if the value of the path when evaluated on the input object
    /// is null, otherwise false.</returns>
    public override Boolean EvaluatesToNull(IObjectView obj)
    {
        return (EvaluateToUInteger(obj) == null);
    }

    /// <summary>
    /// Creates an more instantiated copy of this expression by evaluating it on a Row.
    /// Members, with extent numbers for which there exist objects attached to the Row,
    /// are evaluated and instantiated to literals, other members are not changed.
    /// </summary>
    /// <param name="obj">The Row on which to evaluate the expression.</param>
    /// <returns>A more instantiated expression.</returns>
    public INumericalExpression Instantiate_OLD(Row obj)
    {
        List<IObjectPathItem> instPathList = new List<IObjectPathItem>();
        Int32 i = 0;
        while (i < pathList.Count)
        {
            IObjectPathItem instPathItem = pathList[i].Instantiate(obj) as IObjectPathItem;
            instPathList.Add(instPathItem);
            i++;
        }
        IUIntegerPathItem instMember = member.Instantiate(obj) as IUIntegerPathItem;
        if (instMember is UIntegerLiteral)
        {
            return new UIntegerLiteral(instMember.EvaluateToUInteger(null));
        }
        return new UIntegerPath(extentNumber, instPathList, instMember);
    }

    public INumericalExpression Instantiate(Row obj)
    {
        List<IObjectPathItem> instPathList = new List<IObjectPathItem>();
        Int32 i = 0;
        while (i < pathList.Count)
        {
            IObjectPathItem instPathItem = pathList[i].Instantiate(obj) as IObjectPathItem;
            instPathList.Add(instPathItem);
            i++;
        }
        return new UIntegerPath(extentNumber, instPathList, member.Instantiate(obj) as IUIntegerPathItem);
    }

    public override IValueExpression Clone(VariableArray varArray)
    {
        return CloneToUInteger(varArray);
    }

    public IUIntegerExpression CloneToUInteger(VariableArray varArray)
    {
        List<IObjectPathItem> path = new List<IObjectPathItem>();
        for (Int32 i = 0; i < pathList.Count; i++)
        {
            path.Add(pathList[i].CloneToObject(varArray) as IObjectPathItem);
        }
        return new UIntegerPath(extentNumber, path, member.CloneToUInteger(varArray) as IUIntegerPathItem);
    }

    public INumericalExpression CloneToNumerical(VariableArray varArray)
    {
        List<IObjectPathItem> path = new List<IObjectPathItem>();
        for (Int32 i = 0; i < pathList.Count; i++)
        {
            path.Add(pathList[i].CloneToObject(varArray) as IObjectPathItem);
        }
        return new UIntegerPath(extentNumber, path, member.CloneToUInteger(varArray) as IUIntegerPathItem);
    }

    /// <summary>
    /// Builds a string presentation of this path using the input string-builder.
    /// </summary>
    /// <param name="stringBuilder">String-builder to use.</param>
    /// <param name="tabs">Number of tab indentations for the presentation.</param>
    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "UIntegerPath(");
        for (Int32 i = 0; i < pathList.Count; i++)
        {
            pathList[i].BuildString(stringBuilder, tabs + 1);
        }
        member.BuildString(stringBuilder, tabs + 1);
        stringBuilder.AppendLine(tabs, ")");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        member.GenerateCompilableCode(stringGen);
    }

#if DEBUG
    private bool AssertEqualsVisited = false;
    public bool AssertEquals(IValueExpression other) {
        UIntegerPath otherNode = other as UIntegerPath;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(UIntegerPath other) {
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
        // Check parent
        if (!base.AssertEquals(other))
            return false;
        // Check references. This should be checked if there is cyclic reference.
        AssertEqualsVisited = true;
        bool areEquals = true;
        if (this.member == null) {
            Debug.Assert(other.member == null);
            areEquals = other.member == null;
        } else
            areEquals = this.member.AssertEquals(other.member);
        AssertEqualsVisited = false;
        return areEquals;
    }
#endif
}
}
