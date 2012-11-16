﻿// ***********************************************************************
// <copyright file="BooleanDynamicRange.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query.Sql;
using System;
using System.Collections.Generic;

namespace Starcounter.Query.Execution
{
internal class BooleanDynamicRange : DynamicRange, IDynamicRange
{
    // Pre-cached range values.
    BooleanRangeValue lower, upper;
    List<BooleanRangePoint> rangePointList;

    internal BooleanDynamicRange()
    {
        rangePointList = new List<BooleanRangePoint>();
        logExpr = null;
        lower = new BooleanRangeValue();
        upper = new BooleanRangeValue();
    }

    private BooleanDynamicRange(List<BooleanRangePoint> rangePointList)
    {
        this.rangePointList = rangePointList;
        logExpr = null;
        lower = new BooleanRangeValue();
        upper = new BooleanRangeValue();
    }

    internal void AddRangePoint(BooleanRangePoint rangePoint)
    {
        switch (rangePoint.Operator)
        {
            // Replace "x = a" with "x >= a" and "x <= a".
            case ComparisonOperator.Equal:
                rangePointList.Add(new BooleanRangePoint(ComparisonOperator.GreaterThanOrEqual, rangePoint.Expression));
                rangePointList.Add(new BooleanRangePoint(ComparisonOperator.LessThanOrEqual, rangePoint.Expression));
                break;

            // Replace "x IS NULL" with "x <= NULL".
            case ComparisonOperator.IS:
                rangePointList.Add(new BooleanRangePoint(ComparisonOperator.LessThanOrEqual, rangePoint.Expression));
                break;

            // Replace "x IS NOT NULL" with "x > NULL".
            case ComparisonOperator.ISNOT:
                rangePointList.Add(new BooleanRangePoint(ComparisonOperator.GreaterThan, rangePoint.Expression));
                break;

            // Add "x > NULL" to exclude null values.
            case ComparisonOperator.LessThan:
            case ComparisonOperator.LessThanOrEqual:
                rangePointList.Add(rangePoint);
                rangePointList.Add(new BooleanRangePoint(ComparisonOperator.GreaterThan, new BooleanLiteral(null)));
                break;

            case ComparisonOperator.GreaterThan:
            case ComparisonOperator.GreaterThanOrEqual:
                rangePointList.Add(rangePoint);
                break;

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect Operator.");
        }
    }

    public void CreateRangePointList(List<ILogicalExpression> conditionList, Int32 extentNumber, String strPath)
    {
        RangePoint rangePoint = null;
        for (Int32 i = 0; i < conditionList.Count; i++)
        {
            if (!(conditionList[i] is IComparison))
            {
                break;
            }
            rangePoint = (conditionList[i] as IComparison).CreateRangePoint(extentNumber, strPath);
            if (rangePoint == null)
            {
                break;
            }
            if (!(rangePoint is BooleanRangePoint))
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect rangePoint.");
            }
            AddRangePoint(rangePoint as BooleanRangePoint);
            if (logExpr == null)
            {
                logExpr = conditionList[i];
            }
            else
            {
                logExpr = new LogicalOperation(LogicalOperator.AND, logExpr, conditionList[i]);
            }
            conditionList.RemoveAt(i);
            i--;
        }
    }

    // Creating fill range.
    public void CreateFillRange(
        SortOrder sortOrder,
        ByteArrayBuilder firstKey,
        ByteArrayBuilder secondKey,
        ComparisonOperator lastFirstOperator,
        ComparisonOperator lastSecondOperator)
    {
        // Reseting lower and upper range values.
        lower.ResetValueToMin(lastFirstOperator);
        upper.ResetValueToMax(lastSecondOperator);

        if (sortOrder == SortOrder.Ascending)
        {
            // Appending to the range keys.
            firstKey.Append(lower.GetValue);
            secondKey.Append(upper.GetValue);
        }
        else
        {
            // Appending to the range keys.
            firstKey.Append(upper.GetValue);
            secondKey.Append(lower.GetValue);
        }
    }

    // Appends to a key builder + returns boolean indicating the equality range.
    public Boolean Evaluate(
        Row contextObj,
        SortOrder sortOrder,
        ByteArrayBuilder firstKey,
        ByteArrayBuilder secondKey,
        ref ComparisonOperator firstOp,
        ref ComparisonOperator secondOp)
    {
        // The parameter contextObj can be null but then the expressions in the rangePointList should not include
        // any calls to properties, paths, methods etc.
        lower.ResetValueToMin(ComparisonOperator.GreaterThanOrEqual);
        upper.ResetValueToMax(ComparisonOperator.LessThanOrEqual);

        BooleanRangeValue rangeValue = null;

        // Going through all range points.
        for (Int32 i = 0; i < rangePointList.Count; i++)
        {
            rangeValue = rangePointList[i].Evaluate1(contextObj);
            switch (rangeValue.Operator)
            {
                case ComparisonOperator.LessThanOrEqual:
                case ComparisonOperator.LessThan:
                    {
                        if (rangeValue.CompareTo(upper) < 0)
                            upper = rangeValue;

                        break;
                    }

                case ComparisonOperator.GreaterThanOrEqual:
                case ComparisonOperator.GreaterThan:
                    {
                        if (rangeValue.CompareTo(lower) > 0)
                            lower = rangeValue;

                        break;
                    }
                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect rangePointList.");
            }
        }

        // Check if we have not an equality range.
        if (lower.GetValue != upper.GetValue)
        {
            if (sortOrder == SortOrder.Ascending)
            {
                firstOp = lower.Operator;
                secondOp = upper.Operator;

                // Not an equality range so we need to copy everything in both range keys.
                firstKey.CopyToAnotherByteArray(secondKey);

                firstKey.Append(lower.GetValue);
                secondKey.Append(upper.GetValue);
            }
            else
            {
                // Flipping operators.
                firstOp = upper.Operator;
                secondOp = lower.Operator;

                // Not an equality range so we need to copy everything in both range keys.
                firstKey.CopyToAnotherByteArray(secondKey);

                // Flipping keys.
                firstKey.Append(upper.GetValue);
                secondKey.Append(lower.GetValue);
            }

            return false;
        }

        // Appending only to the lower key, since we have equality range.
        firstKey.Append(lower.GetValue);
        return true;
    }

    public IDynamicRange Clone(VariableArray varArray)
    {
        List<BooleanRangePoint> rangePointListClone = new List<BooleanRangePoint>();
        for (Int32 i = 0; i < rangePointList.Count; i++)
        {
            rangePointListClone.Add(rangePointList[i].Clone(varArray));
        }
        return new BooleanDynamicRange(rangePointListClone);
    }


    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "BooleanDynamicRange(");
        for (Int32 i = 0; i < rangePointList.Count; i++)
        {
            rangePointList[i].BuildString(stringBuilder, tabs + 1);
        }
        stringBuilder.AppendLine(tabs, ")");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "Boolean range: " + lower.GetValue + " - " + upper.GetValue);
    }
}
}