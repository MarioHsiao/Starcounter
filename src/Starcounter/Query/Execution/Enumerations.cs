// ***********************************************************************
// <copyright file="Enumerations.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Query.Execution
{
    /// <summary>
    /// Comparison operators.
    /// </summary>
    internal enum ComparisonOperator
    {
        // The order between the elements (LessThan, GreaterThanOrEqual, LessThanOrEqual, GreaterThan) are important
        // for methods IndexRangeValue.CompareTo and <Type>RangeValue.CompareTo (in namespace Starcounter.Query.Execution).
        LessThan = 1, GreaterThanOrEqual = 2, LessThanOrEqual = 3, GreaterThan = 4,
        Equal, NotEqual, IS, ISNOT, LIKEstatic, LIKEdynamic
    }

    /// <summary>
    /// Logical operators, where XOR is "exclusive or".
    /// </summary>
    internal enum LogicalOperator
    {
        AND, OR, NOT, IS, XOR
    }

    /// <summary>
    /// Numerical operators where Addition (X+Y), Subtraction (X-Y), Multiplication (X*Y) and
    /// Division (X/Y) are binary, while Plus (+X) and Minus (-X) are unary.
    /// </summary>
    internal enum NumericalOperator
    {
        Addition, Subtraction, Multiplication, Division, Plus, Minus
    }

    internal enum SetFunctionType
    {
        AVG, COUNT, MAX, MIN, SUM
    }

    /// <summary>
    /// Sort orderings.
    /// </summary>
    internal enum SortOrder : int
    {
        Ascending = 0,
        Descending = 1
    }

    /// <summary>
    /// String operators.
    /// Operator AppendMaxChar is used to handle upper end-point in "STARTS WITH" intervals.
    /// </summary>
    internal enum StringOperator
    {
        Concatenation, AppendMaxChar
    }

    /// <summary>
    /// Truth values for SQL which is three valued (TRUE, FALSE and UNKNOWN).
    /// </summary>
    internal enum TruthValue
    {
        UNKNOWN, FALSE, TRUE
    }

    internal enum IsTypeCompare {
        EQUAL, // Object has the same type as the type (TRUE)
        SUPERTYPE, // Object is supertype to the type (can be FALSE or TRUE)
        SUBTYPE, // Object is subtype of the type (TRUE)
        TRUE, // It is known to be true, e.g., object is evaluated and IS evaluated to TRUE
        FALSE, // Tt is known to be false, e.g., object is evaluated and IS evaluated to FALSE, or extent type binding is neither super or sub type to the type binding.
        UNKNOWNTYPE, // Type is unknown (i.e., comes through variable), while object or object type is known
        UNKNOWNOBJECT, // Object type is unknown (i.e., comes through variable), while type is known
        UNKNOWN // Neither type nor Object type are unknown
    }

    /// <summary>
    /// Join types used in the query execution.
    /// Cross joins are a special case of inner joins.
    /// </summary>
    internal enum JoinType
    {
        Undecided, Inner, LeftOuter, RightOuter
    }
    /// <summary>
    /// Format for key-values used for index ranges.
    /// </summary>
    internal enum KeyValueFormat
    {
        Fixed, Variable
    }

    // TODO: Remove?
    internal enum QueryTypeCode
    {
        Binary, Boolean, DateTime, Decimal, Double, Integer, Numerical, Object, String, UInteger
    }

    internal enum VariableTypeCode
    {
        Binary, Boolean, DateTime, Numerical, Object, String
    }

    [FlagsAttribute]
    internal enum QueryFlags
    {
        None = 0, IncludesLiteral = 1, IncludesAggregation = 2, IncludesLIKEvariable = 4, IncludesSorting = 8, 
        IncludesFetchLiteral = 16, IncludesFetchVariable = 32, IncludesOffsetKeyLiteral = 64, IncludesOffsetKeyVariable = 128, 
        SingletonProjection = 256
    }

    /// <summary>
    /// Identifies the node type in condition tree.
    /// </summary>
    internal enum ConditionNodeType
    {
        Property,
        Variable,
        Literal,
        ObjectThis,
        CompOpEqual,
        CompOpNotEqual,
        CompOpGreater,
        CompOpGreaterOrEqual,
        CompOpLess,
        CompOpLessOrEqual,
        Unsupported
    };

    internal enum EnumeratorNodeType : byte {
        FullTableScan,
        IndexScan,
        ReferenceLookup,
        ObjectIdentityLookup,
        Join,
        Aggregate,
        Sorting,
        LikeExec
    }
}
