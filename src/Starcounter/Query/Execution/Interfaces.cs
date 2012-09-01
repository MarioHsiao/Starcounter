
// + IQueryObject
// |
// |---+ IConditionTreeNode
// |   |
// |   +---+ ITypeExpression
// |   |   |
// |   |   |
// |   |   +---- ILiteral
// |   |   |
// |   |   +---- IVariable
// |   |   |
// |   |   +---+ IPath
// |   |   |   |
// |   |   |   +---+ IMember
// |   |   |       |
// |   |   |       +---- IProperty
// |   |   |       |
// |   |   |       +---- IMethod
// |   |   |
// |   |   +---+ IOperation
// |   |   |   |
// |   |   |   +---- INumericalOperation
// |   |   |
// |   |   +---+ INumericalExpression
// |   |   |   |
// |   |   +   +---+ IDecimalExpression
// |   |   |   |   |
// |   |   |   |   +---- IDecimalPathItem
// |   |   |   |
// |   |   +   +---+ IDoubleExpression
// |   |   |   |   |
// |   |   |   |   +---- IDoublePathItem
// |   |   |   |
// |   |   +   +---+ IIntegerExpression
// |   |   |   |   |
// |   |   |   |   +---- IIntegerPathItem
// |   |   |   |
// |   |   +   +---+ IUIntegerExpression
// |   |   |       |
// |   |   |       +---- IUIntegerPathItem
// |   |   |
// |   |   +---+ IBinaryExpression
// |   |   |   |
// |   |   |   +---- IBinaryPathItem
// |   |   |
// |   |   +---+ IBooleanExpression
// |   |   |   |
// |   |   |   +---- IBooleanPathItem
// |   |   |
// |   |   +---+ IDateTimeExpression
// |   |   |   |
// |   |   |   +---- IDateTimePathItem
// |   |   |
// |   |   +---+ IObjectExpression
// |   |   |   |
// |   |   |   +---- IObjectPathItem
// |   |   |
// |   |   +---+ IStringExpression
// |   |       |
// |   |       +---- IStringPathItem
// |   |
// |   +---+ ILogicalExpression
// |       |
// |       +---- IComparison
// |
// +---- ISetFunction
// |
// +---+ IQueryComparer
// |   |
// |   +---+ ISingleComparer
// |       |
// |       +---- IPathComparer
// |
// +---+ ISqlEnumerator
// |   |
// |   +--- IExecutionEnumerator
// |
// +---- NumericalRangePoint
// |
// +---- IDynamicRange
//
// - IStaticRange

using Sc.Server.Binding;
using Starcounter.Query.Optimization;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using Sc.Server.Internal;

namespace Starcounter.Query.Execution
{

/// <summary>
/// This interface indicates that its children
/// must produce compilable code.
/// </summary>
internal interface ICompilable
{
    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    void GenerateCompilableCode(CodeGenStringGenerator stringGen);
}

/// <summary>
/// Interface for all types of objects in a query execution, which includes:
/// literals, properties, paths, operations, conditions, comparers and enumerators.
/// </summary>
internal interface IQueryObject : ICompilable
{
    /// <summary>
    /// Builds a string presentation of the expression using the input string-builder.
    /// </summary>
    /// <param name="stringBuilder">String-builder to use.</param>
    /// <param name="tabs">Number of tab indentations for the presentation.</param>
    void BuildString(MyStringBuilder stringBuilder, Int32 tabs);
}

internal interface IConditionTreeNode : IQueryObject
{
    /// <summary>
    /// Updates the set of extents with all extents referenced in the current expression.
    /// </summary>
    /// <param name="extentSet">The set of extents to be updated.</param>
    void InstantiateExtentSet(ExtentSet extentSet);

    // Append this node to filter instructions and leaves.
    // Called statically so no need to worry about performance.
    UInt32 AppendToInstrAndLeavesList(List<CodeGenFilterNode> dataLeaves,
                                      CodeGenFilterInstrArray instrArray,
                                      Int32 currentExtent,
                                      StringBuilder filterText);
}

/// <summary>
/// Identifies the node type in condition tree.
/// </summary>
enum ConditionNodeType
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

/// <summary>
/// Interface for all types of expressions that have a return value of a value type or
/// an object reference, which includes: literals, properties, paths and operations.
/// </summary>
internal interface ITypeExpression : IConditionTreeNode
{
    /// <summary>
    /// The DbTypeCode of the value of the expression or the property.
    /// </summary>
    DbTypeCode DbTypeCode
    {
        get;
    }

    /// <summary>
    /// Examines if the value of the expression is null when evaluated on an input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the expression.</param>
    /// <returns>True, if the value of the expression when evaluated on the input object
    /// is null, otherwise false.</returns>
    Boolean EvaluatesToNull(IObjectView obj);

    /// <summary>
    /// Generic clone for ITypeExpression types.
    /// </summary>
    /// <param name="varArray">Variables array.</param>
    /// <returns>Clone of the expression.</returns>
    ITypeExpression Clone(VariableArray varArray);
}

/// <summary>
/// Interface for literals of all data types.
/// </summary>
internal interface ILiteral : ITypeExpression
{
}

/// <summary>
/// Interface for variables of all data types.
/// </summary>
internal interface IVariable : ITypeExpression
{
    Int32 Number
    {
        get;
    }

    // Special case when value of variable must be set to null.
    void SetNullValue();

    // Appending variable value to key (with the context object).
    void AppendToByteArray(ByteArrayBuilder key, IObjectView obj);

    // Setting values of different data types.
    void SetValue(Binary newValue);
    void SetValue(Byte[] newValue);
    void SetValue(Boolean newValue);
    void SetValue(DateTime newValue);
    void SetValue(Decimal newValue);
    void SetValue(Double newValue);
    void SetValue(Single newValue);
    void SetValue(Int64 newValue);
    void SetValue(Int32 newValue);
    void SetValue(Int16 newValue);
    void SetValue(SByte newValue);
    void SetValue(IObjectView newValue);
    void SetValue(String newValue);
    void SetValue(UInt64 newValue);
    void SetValue(UInt32 newValue);
    void SetValue(UInt16 newValue);
    void SetValue(Byte newValue);
    void SetValue(Object newValue);

    // Sets value to variable in another enumerator.
    void ProlongValue(IExecutionEnumerator destEnum);

    // Appends maximum and minimum value to the provided filter key.
    void AppendMaxToKey(ByteArrayBuilder key);
    void AppendMinToKey(ByteArrayBuilder key);

    // Initializes variable from byte buffer.
    unsafe void InitFromBuffer(ref Byte *buffer);
}

/// <summary>
/// Interface for path expressions of all data types.
/// </summary>
internal interface IPath : ITypeExpression
{
    /// <summary>
    /// The extent number of the extent to which this path belongs.
    /// </summary>
    Int32 ExtentNumber
    {
        get;
    }

    /// <summary>
    /// Name to be displayed for example as column header in a result grid.
    /// </summary>
    String Name
    {
        get;
    }

    /// <summary>
    /// The full name of the path, for example used to uniquely identify the path.
    /// </summary>
    String FullName
    {
        get;
    }
}

/// <summary>
/// Interface for members (properties and methods) of all data types.
/// </summary>
internal interface IMember : IPath
{
}

/// <summary>
/// Interface for properties of all data types.
/// </summary>
internal interface IProperty : IMember
{
}

/// <summary>
/// Interface for methods with return value of all data types.
/// </summary>
internal interface IMethod : IMember
{
}

/// <summary>
/// Interface for numerical expressions which are all expressions of a numerical type
/// (Decimal, Double, Int64, UInt64).
/// </summary>
internal interface INumericalExpression : ITypeExpression
{
    /// <summary>
    /// Calculates the value as a nullable Decimal of the expression when evaluated on an input object.
    /// All properties in the expression are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the expression.</param>
    /// <returns>The value of the expression when evaluated on the input object.</returns>
    Nullable<Decimal> EvaluateToDecimal(IObjectView obj);

    /// <summary>
    /// Calculates the value as a nullable Double of the expression when evaluated on an input object.
    /// All properties in the expression are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the expression.</param>
    /// <returns>The value of the expression when evaluated on the input object.</returns>
    Nullable<Double> EvaluateToDouble(IObjectView obj);

    /// <summary>
    /// Calculates the value as a nullable Int64 of the expression when evaluated on an input object.
    /// All properties in the expression are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the expression.</param>
    /// <returns>The value of the expression when evaluated on the input object.</returns>
    Nullable<Int64> EvaluateToInteger(IObjectView obj);

    /// <summary>
    /// Calculates the value as a ceiling (round up) nullable Int64 of the expression when evaluated on an input object.
    /// All properties in the expression are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the expression.</param>
    /// <returns>The value of the expression when evaluated on the input object.</returns>
    Nullable<Int64> EvaluateToIntegerCeiling(IObjectView obj);

    /// <summary>
    /// Calculates the value as a floor (round down) nullable Int64 of the expression when evaluated on an input object.
    /// All properties in the expression are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the expression.</param>
    /// <returns>The value of the expression when evaluated on the input object.</returns>
    Nullable<Int64> EvaluateToIntegerFloor(IObjectView obj);

    /// <summary>
    /// Calculates the value as a nullable UInt64 of the expression when evaluated on an input object.
    /// All properties in the expression are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the expression.</param>
    /// <returns>The value of the expression when evaluated on the input object.</returns>
    Nullable<UInt64> EvaluateToUInteger(IObjectView obj);

    /// <summary>
    /// Calculates the value as a ceiling (round up) nullable UInt64 of the expression when evaluated on an input object.
    /// All properties in the expression are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the expression.</param>
    /// <returns>The value of the expression when evaluated on the input object.</returns>
    Nullable<UInt64> EvaluateToUIntegerCeiling(IObjectView obj);

    /// <summary>
    /// Calculates the value as a floor (round down) nullable UInt64 of the expression when evaluated on an input object.
    /// All properties in the expression are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the expression.</param>
    /// <returns>The value of the expression when evaluated on the input object.</returns>
    Nullable<UInt64> EvaluateToUIntegerFloor(IObjectView obj);

    /// <summary>
    /// Creates an more instantiated copy of the expression by evaluating it on a result-object.
    /// Properties, with extent numbers for which there exist objects attached to the result-object,
    /// are evaluated and instantiated to literals, other properties are not changed.
    /// </summary>
    /// <param name="obj">The result-object on which to evaluate the expression.</param>
    /// <returns>A more instantiated expression.</returns>
    INumericalExpression Instantiate(CompositeObject obj);

    INumericalExpression CloneToNumerical(VariableArray varArray);
}

/// <summary>
/// Interface for Decimal expressions which are all expressions of type Decimal.
/// </summary>
internal interface IDecimalExpression : INumericalExpression
{
    IDecimalExpression CloneToDecimal(VariableArray varArray);
}

/// <summary>
/// Interface for Double expressions which are all expressions of type Double.
/// </summary>
internal interface IDoubleExpression : INumericalExpression
{
    IDoubleExpression CloneToDouble(VariableArray varArray);
}

/// <summary>
/// Interface for integer expressions which are all expressions of type integer (Int64).
/// </summary>
internal interface IIntegerExpression : INumericalExpression
{
    IIntegerExpression CloneToInteger(VariableArray varArray);
}

/// <summary>
/// Interface for unsigned integer expressions which are all expressions type unsigned integer (UInt64).
/// </summary>
internal interface IUIntegerExpression : INumericalExpression
{
    IUIntegerExpression CloneToUInteger(VariableArray varArray);
}

/// <summary>
/// Interface for Binary expressions which are all expressions of type Binary.
/// </summary>
internal interface IBinaryExpression : ITypeExpression
{
    /// <summary>
    /// Calculates the value of the expression when evaluated on an input object.
    /// All properties in the expression are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the expression.</param>
    /// <returns>The value of the expression when evaluated on the input object.</returns>
    Nullable<Binary> EvaluateToBinary(IObjectView obj);

    /// <summary>
    /// Creates an more instantiated copy of the expression by evaluating it on a result-object.
    /// Properties, with extent numbers for which there exist objects attached to the result-object,
    /// are evaluated and instantiated to literals, other properties are not changed.
    /// </summary>
    /// <param name="obj">The result-object on which to evaluate the expression.</param>
    /// <returns>A more instantiated expression.</returns>
    IBinaryExpression Instantiate(CompositeObject obj);

    IBinaryExpression CloneToBinary(VariableArray varArray);
}

/// <summary>
/// Interface for Boolean expressions which are all expressions of type Boolean.
/// </summary>
internal interface IBooleanExpression : ITypeExpression
{
    /// <summary>
    /// Calculates the value of the expression when evaluated on an input object.
    /// All properties in the expression are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the expression.</param>
    /// <returns>The value of the expression when evaluated on the input object.</returns>
    Nullable<Boolean> EvaluateToBoolean(IObjectView obj);

    /// <summary>
    /// Creates an more instantiated copy of the expression by evaluating it on a result-object.
    /// Properties, with extent numbers for which there exist objects attached to the result-object,
    /// are evaluated and instantiated to literals, other properties are not changed.
    /// </summary>
    /// <param name="obj">The result-object on which to evaluate the expression.</param>
    /// <returns>A more instantiated expression.</returns>
    IBooleanExpression Instantiate(CompositeObject obj);

    IBooleanExpression CloneToBoolean(VariableArray varArray);
}

/// <summary>
/// Interface for DateTime expressions which are all expressions of type DateTime.
/// </summary>
internal interface IDateTimeExpression : ITypeExpression
{
    /// <summary>
    /// Calculates the value of the expression when evaluated on an input object.
    /// All properties in the expression are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the expression.</param>
    /// <returns>The value of the expression when evaluated on the input object.</returns>
    Nullable<DateTime> EvaluateToDateTime(IObjectView obj);

    /// <summary>
    /// Creates an more instantiated copy of the expression by evaluating it on a result-object.
    /// Properties, with extent numbers for which there exist objects attached to the result-object,
    /// are evaluated and instantiated to literals, other properties are not changed.
    /// </summary>
    /// <param name="obj">The result-object on which to evaluate the expression.</param>
    /// <returns>A more instantiated expression.</returns>
    IDateTimeExpression Instantiate(CompositeObject obj);

    IDateTimeExpression CloneToDateTime(VariableArray varArray);
}

/// <summary>
/// Interface for object expressions which are all expressions of type Object (reference).
/// </summary>
internal interface IObjectExpression : ITypeExpression
{
    ITypeBinding TypeBinding
    {
        get;
    }

    /// <summary>
    /// Calculates the value of the expression when evaluated on an input object.
    /// All properties in the expression are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the expression.</param>
    /// <returns>The value of the expression when evaluated on the input object.</returns>
    IObjectView EvaluateToObject(IObjectView obj);

    /// <summary>
    /// Creates an more instantiated copy of the expression by evaluating it on a result-object.
    /// Properties, with extent numbers for which there exist objects attached to the result-object,
    /// are evaluated and instantiated to literals, other properties are not changed.
    /// </summary>
    /// <param name="obj">The result-object on which to evaluate the expression.</param>
    /// <returns>A more instantiated expression.</returns>
    IObjectExpression Instantiate(CompositeObject obj);

    IObjectExpression CloneToObject(VariableArray varArray);
}

/// <summary>
/// Interface for String expressions which are all expressions of type String.
/// </summary>
internal interface IStringExpression : ITypeExpression
{
    /// <summary>
    /// Calculates the value of the expression when evaluated on an input object.
    /// All properties in the expression are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the expression.</param>
    /// <returns>The value of the expression when evaluated on the input object.</returns>
    String EvaluateToString(IObjectView obj);

    /// <summary>
    /// Creates an more instantiated copy of the expression by evaluating it on a result-object.
    /// Properties, with extent numbers for which there exist objects attached to the result-object,
    /// are evaluated and instantiated to literals, other properties are not changed.
    /// </summary>
    /// <param name="obj">The result-object on which to evaluate the expression.</param>
    /// <returns>A more instantiated expression.</returns>
    IStringExpression Instantiate(CompositeObject obj);

    IStringExpression CloneToString(VariableArray varArray);
}

/// <summary>
/// Interface for path items of datatype Binary.
/// </summary>
internal interface IBinaryPathItem : IBinaryExpression
{
    /// <summary>
    /// Calculates the value of the path-item when evaluated on a current object and the start object
    /// of the path to which the current object belongs.
    /// </summary>
    /// <param name="obj">The current object on which to evaluate the expression.</param>
    /// <param name="startObj">The start object of the current path on which to evaluate the expression.</param>
    /// <returns>The value of the expression when evaluated on the current object and the start object.</returns>
    Nullable<Binary> EvaluateToBinary(IObjectView obj, IObjectView startObj);
}

/// <summary>
/// Interface for path items of datatype Boolean.
/// </summary>
internal interface IBooleanPathItem : IBooleanExpression
{
    /// <summary>
    /// Calculates the value of the path-item when evaluated on a current object and the start object
    /// of the path to which the current object belongs.
    /// </summary>
    /// <param name="obj">The current object on which to evaluate the expression.</param>
    /// <param name="startObj">The start object of the current path on which to evaluate the expression.</param>
    /// <returns>The value of the expression when evaluated on the current object and the start object.</returns>
    Nullable<Boolean> EvaluateToBoolean(IObjectView obj, IObjectView startObj);
}

/// <summary>
/// Interface for path items of datatype DateTime.
/// </summary>
internal interface IDateTimePathItem : IDateTimeExpression
{
    /// <summary>
    /// Calculates the value of the path-item when evaluated on a current object and the start object
    /// of the path to which the current object belongs.
    /// </summary>
    /// <param name="obj">The current object on which to evaluate the expression.</param>
    /// <param name="startObj">The start object of the current path on which to evaluate the expression.</param>
    /// <returns>The value of the expression when evaluated on the current object and the start object.</returns>
    Nullable<DateTime> EvaluateToDateTime(IObjectView obj, IObjectView startObj);
}

/// <summary>
/// Interface for path items of datatype Decimal.
/// </summary>
internal interface IDecimalPathItem : IDecimalExpression
{
    /// <summary>
    /// Calculates the value of the path-item when evaluated on a current object and the start object
    /// of the path to which the current object belongs.
    /// </summary>
    /// <param name="obj">The current object on which to evaluate the expression.</param>
    /// <param name="startObj">The start object of the current path on which to evaluate the expression.</param>
    /// <returns>The value of the expression when evaluated on the current object and the start object.</returns>
    Nullable<Decimal> EvaluateToDecimal(IObjectView obj, IObjectView startObj);
}

/// <summary>
/// Interface for path items of datatype Double.
/// </summary>
internal interface IDoublePathItem : IDoubleExpression
{
    /// <summary>
    /// Calculates the value of the path-item when evaluated on a current object and the start object
    /// of the path to which the current object belongs.
    /// </summary>
    /// <param name="obj">The current object on which to evaluate the expression.</param>
    /// <param name="startObj">The start object of the current path on which to evaluate the expression.</param>
    /// <returns>The value of the expression when evaluated on the current object and the start object.</returns>
    Nullable<Double> EvaluateToDouble(IObjectView obj, IObjectView startObj);
}

/// <summary>
/// Interface for path items of datatype integer.
/// </summary>
internal interface IIntegerPathItem : IIntegerExpression
{
    /// <summary>
    /// Calculates the value of the path-item when evaluated on a current object and the start object
    /// of the path to which the current object belongs.
    /// </summary>
    /// <param name="obj">The current object on which to evaluate the expression.</param>
    /// <param name="startObj">The start object of the current path on which to evaluate the expression.</param>
    /// <returns>The value of the expression when evaluated on the current object and the start object.</returns>
    Nullable<Int64> EvaluateToInteger(IObjectView obj, IObjectView startObj);
}

/// <summary>
/// Interface for path items of datatype Object (reference).
/// </summary>
internal interface IObjectPathItem : IObjectExpression
{
    /// <summary>
    /// Calculates the value of the path-item when evaluated on a current object and the start object
    /// of the path to which the current object belongs.
    /// </summary>
    /// <param name="obj">The current object on which to evaluate the expression.</param>
    /// <param name="startObj">The start object of the current path on which to evaluate the expression.</param>
    /// <returns>The value of the expression when evaluated on the current object and the start object.</returns>
    IObjectView EvaluateToObject(IObjectView obj, IObjectView startObj);

    /// <summary>
    /// Name of the object path item. Used to create a unique full path name.
    /// </summary>
    String Name
    {
        get;
    }
}

/// <summary>
/// Interface for path items of datatype String.
/// </summary>
internal interface IStringPathItem : IStringExpression
{
    /// <summary>
    /// Calculates the value of the path-item when evaluated on a current object and the start object
    /// of the path to which the current object belongs.
    /// </summary>
    /// <param name="obj">The current object on which to evaluate the expression.</param>
    /// <param name="startObj">The start object of the current path on which to evaluate the expression.</param>
    /// <returns>The value of the expression when evaluated on the current object and the start object.</returns>
    String EvaluateToString(IObjectView obj, IObjectView startObj);
}

/// <summary>
/// Interface for path items of datatype unsigned integer.
/// </summary>
internal interface IUIntegerPathItem : IUIntegerExpression
{
    /// <summary>
    /// Calculates the value of the path-item when evaluated on a current object and the start object
    /// of the path to which the current object belongs.
    /// </summary>
    /// <param name="obj">The current object on which to evaluate the expression.</param>
    /// <param name="startObj">The start object of the current path on which to evaluate the expression.</param>
    /// <returns>The value of the expression when evaluated on the current object and the start object.</returns>
    Nullable<UInt64> EvaluateToUInteger(IObjectView obj, IObjectView startObj);
}

/// <summary>
/// Interface for operations of all data types.
/// </summary>
internal interface IOperation : ITypeExpression
{
}

/// <summary>
/// Interface for operations of all numerical data types.
/// </summary>
internal interface INumericalOperation : IOperation
{
}

/// <summary>
/// Interface for logical expressions (conditions) which are expressions that have a return
/// value of type TruthValue.
/// </summary>
internal interface ILogicalExpression : IConditionTreeNode
{
    /// <summary>
    /// Calculates the truth value (TRUE, FALSE or UNKNOWN) of the condition when
    /// evaluated on an input object. All properties in the expression are evaluated on
    /// the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the condition.</param>
    /// <returns>The truth value of the condition when evaluated on the input object.</returns>
    TruthValue Evaluate(IObjectView obj);

    /// <summary>
    /// Calculates the Boolean value (true or false) of the condition when applied
    /// to an input object. All properties in the expression are evaluated on the input
    /// object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the condition.</param>
    /// <returns>The Boolean value of the condition when evaluated on the input object.</returns>
    Boolean Filtrate(IObjectView obj);

    /// <summary>
    /// Creates an more instantiated copy of the expression by evaluating it on a result-object.
    /// Properties, with extent numbers for which there exist objects attached to the result-object,
    /// are evaluated and instantiated to literals, other properties are not changed.
    /// </summary>
    /// <param name="obj">The result-object on which to evaluate the expression.</param>
    /// <returns>A more instantiated expression.</returns>
    ILogicalExpression Instantiate(CompositeObject obj);

    /// <summary>
    /// Interface for cloning logical expressions.
    /// </summary>
    /// <param name="varArray">Variable array.</param>
    /// <returns>Cloned logical expression.</returns>
    ILogicalExpression Clone(VariableArray varArray);
}

/// <summary>
/// Interface for comparisons of all data types.
/// </summary>
internal interface IComparison : ILogicalExpression
{
    ComparisonOperator Operator
    {
        get;
    }

    /// <summary>
    /// Gets a path that eventually (if there is a corresponding index) can be used for
    /// an index scan for the extent with the input extent number, if there is such a path.
    /// </summary>
    /// <param name="extentNumber">An extent number.</param>
    /// <returns>A path, if an appropriate one is found, otherwise null.</returns>
    IPath GetIndexPath(Int32 extentNumber);

    RangePoint CreateRangePoint(Int32 extentNumber, String strPath);
}

internal interface ISetFunction : IQueryObject
{
    /// <summary>
    /// The DbTypeCode of the result of the set-function.
    /// </summary>
    DbTypeCode DbTypeCode
    {
        get;
    }

    void UpdateResult(IObjectView obj);

    void ResetResult();

    ILiteral GetResult();

    ISetFunction Clone(VariableArray varArray);
}

/// <summary>
/// Interface for expressions which are comparers of result objects.
/// </summary>
internal interface IQueryComparer : IComparer<CompositeObject>, IQueryObject
{
    IQueryComparer Clone(VariableArray varArray);
}

/// <summary>
/// Interface for expressions which are single comparers of result objects.
/// Single comparer means that one single expression is used for the comparison.
/// </summary>
internal interface ISingleComparer : IQueryComparer
{
    /// <summary>
    /// The DbTypeCode of the expression used for the comparison.
    /// </summary>
    DbTypeCode ComparerTypeCode
    {
        get;
    }

    /// <summary>
    /// The expression used for the comparison.
    /// </summary>
    ITypeExpression Expression
    {
        get;
    }

    /// <summary>
    /// The sort ordering (ascending or descending) of the comparer.
    /// </summary>
    SortOrder SortOrdering
    {
        get;
    }

    /// <summary>
    /// Calculates the value of the comparison expression when evaluated on an
    /// input object. All properties in the expression are evaluated on the input
    /// object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the comparison expression.</param>
    /// <returns>The value of the comparison expression when evaluated on the input
    /// object.</returns>
    ILiteral Evaluate(CompositeObject obj);

    /// <summary>
    /// Compares the value specified by the input literal with the value of the
    /// comparison expression evaluated on the input object.
    /// </summary>
    /// <param name="value">The value to be compared.</param>
    /// <param name="obj">The object to be compared.</param>
    /// <returns>The value -1 if the input value is less than the comparison
    /// evaluation of the input object w.r.t. the current sort ordering.
    /// The value 0 if the comparison values are equal.
    /// The value 1 if the input value is greater than the comparison evaluation
    /// of the input object w.r.t. the current sort ordering.
    /// </returns>
    Int32 Compare(ILiteral value, CompositeObject obj);

    ISingleComparer CloneToSingleComparer(VariableArray varArray);
}

/// <summary>
/// Interface for single comparers where the expresion used for the comparison is
/// a path.
/// </summary>
internal interface IPathComparer : ISingleComparer
{
    /// <summary>
    /// The path used for the comparison.
    /// </summary>
    IPath Path
    {
        get;
    }
}

internal interface IExecutionEnumerator : IQueryObject, ISqlEnumerator
{
    /// <summary>
    /// Sets a null value to an SQL variable.
    /// </summary>
    /// <param name="number">The order number of the variable starting at 0.</param>
    void SetVariableToNull(Int32 index);

    /// <summary>
    /// Sets a value to an SQL variable.
    /// </summary>
    /// <param name="number">The order number of the variable starting at 0.</param>
    /// <param name="value">The new value of the variable.</param>
    void SetVariable(Int32 index, Binary newValue);
    void SetVariable(Int32 index, Byte[] newValue);
    void SetVariable(Int32 index, Boolean newValue);
    void SetVariable(Int32 index, DateTime newValue);
    void SetVariable(Int32 index, Decimal newValue);
    void SetVariable(Int32 index, Double newValue);
    void SetVariable(Int32 index, Single newValue);
    void SetVariable(Int32 index, Int64 newValue);
    void SetVariable(Int32 index, Int32 newValue);
    void SetVariable(Int32 index, Int16 newValue);
    void SetVariable(Int32 index, SByte newValue);
    void SetVariable(Int32 index, IObjectView newValue);
    void SetVariable(Int32 index, String newValue);
    void SetVariable(Int32 index, UInt64 newValue);
    void SetVariable(Int32 index, UInt32 newValue);
    void SetVariable(Int32 index, UInt16 newValue);
    void SetVariable(Int32 index, Byte newValue);
    void SetVariable(Int32 index, Object newValue);

    /// <summary>
    /// Sets values to all SQL variables in the SQL query.
    /// </summary>
    /// <param name="values">The new values of the variables in order of appearance.</param>
    void SetVariables(Object[] newValues);

    /// <summary>
    /// Gets the number of variables in the SQL query.
    /// </summary>
    Int32 VariableCount
    {
        get;
    }

    ///// <summary>
    ///// Used to obtain the next resulting object.
    ///// </summary>
    ///// <returns></returns>
    //Boolean MoveNext();

    //// General enumerator reset.
    //void Reset();

    //Int32 Counter
    //{
    //    get;
    //}

    /// <summary>
    /// Uniquely identifies query it belongs to (within one VP).
    /// </summary>
    UInt64 UniqueQueryID
    {
        get;
        set;
    }

    /// <summary>
    /// Indicates if code generation is possible for this enumerator.
    /// </summary>
    Boolean HasCodeGeneration
    {
        get;
        set;
    }

    // Gets the unique name of this execution enumerator.
    String GetUniqueName(UInt64 seqNumber);

    // Returns current composite object.
    CompositeObject CurrentCompositeObject
    {
        get;
    }

    // Returns reference to variable array.
    VariableArray VarArray
    {
        get;
    }

    /// <summary>
    /// Is used for outer joins. Works like MoveNext() but with the following exception.
    /// If either the parameter force is true
    /// or no next object is found and no object has been returned (counter = 0)
    /// then a NullObject is created and MoveNextSpecial returns true.
    /// </summary>
    /// <param name="force">Specifies if the creation of a NullObject should be forced or not.</param>
    /// <returns></returns>
    Boolean MoveNextSpecial(Boolean force);

    // Used for joins when result object is supplied from the outer scan loop for filtering on inner level.
    void Reset(CompositeObject contextObj);

    CompositeTypeBinding CompositeTypeBinding
    {
        get;
    }

    Int32 Depth
    {
        get;
    }

    /// <summary>
    /// Creates a clone of the execution enumerator.
    /// </summary>
    /// <param name="compTypeBindClone">A cloned composite-type-binding as input.</param>
    /// <param name="varArray">An array of variables to be instantiated.</param>
    /// <returns></returns>
    IExecutionEnumerator Clone(CompositeTypeBinding compTypeBindClone, VariableArray varArray);
    IExecutionEnumerator CloneCached();

    // For attaching enumerator to cache.
    void AttachToCache(LinkedList<IExecutionEnumerator> fromCache);

    // Initializes all query variables from given buffer.
    unsafe void InitVariablesFromBuffer(Byte * queryParamsBuf);

    // Does the continuous object ETIs and IDs fill up into the dedicated buffer.
    unsafe UInt32 FillupFoundObjectIDs(Byte * results, UInt32 resultsMaxBytes, UInt32 * resultsNum, UInt32 * flags);

    // Populates needed query flags, such as if it includes sorting, fetch statement, projection, etc.
    unsafe void PopulateQueryFlags(UInt32 * flags);

    // Populates query information such as fetch literal/variable info and recreation key info.
    unsafe UInt32 GetInfo(
        Byte infoType,
        UInt64 param,
        Byte * results,
        UInt32 maxBytes,
        UInt32 * outLenBytes);

    // Saves the enumerator being in the current state.
    unsafe Int32 SaveEnumerator(Byte * keysData, Int32 globalOffset, Boolean saveDynamicDataOnly);

    // Flags describing whether the query includes literal, aggregation etc.
    QueryFlags QueryFlags
    {
        get;
    }

    // Query string.
    String QueryString
    {
        get;
    }

    // The id of the transaction to which this ExecutionEnumerator belongs.
    UInt64 TransactionId { set; }

    // Set flag indicating getting first result only.
    void SetFirstOnlyFlag();
}

/// <summary>
/// Interface for dynamic ranges.
/// </summary>
internal interface IDynamicRange : IQueryObject
{
    // Returns true if the range is an equality range.
    Boolean Evaluate(CompositeObject contextObj, SortOrder sortOrder, ByteArrayBuilder firstKey, ByteArrayBuilder secondKey,
                     ref ComparisonOperator firstOp, ref ComparisonOperator secondOp);

    // Filling the range with minimum/maximum values according to the last operators.
    void CreateFillRange(SortOrder sortOrder, ByteArrayBuilder firstKey, ByteArrayBuilder secondKey,
                         ComparisonOperator lastFirstOperator, ComparisonOperator lastSecondOperator);

    void CreateRangePointList(List<ILogicalExpression> conditionList, Int32 extentNumber, String strPath);

    IDynamicRange Clone(VariableArray varArray);
}

/// <summary>
/// Interface for static ranges.
/// </summary>
internal interface IStaticRange
{
    DbTypeCode DbTypeCode
    {
        get;
    }

    ComparisonOperator LowerOperator
    {
        get;
    }

    ComparisonOperator UpperOperator
    {
        get;
    }

    Boolean IsEqualityRange();
}
}
