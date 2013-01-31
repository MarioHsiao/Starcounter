using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Starcounter.Binding;
using Starcounter.Query.Optimization;

namespace Starcounter.Query.Execution {
    internal class IsTypePredicate : IComparison {
        ComparisonOperator compOperator;
        IObjectExpression Expr1;
        ITypeBinding typeBinding;

        internal IsTypePredicate(ComparisonOperator compOp, IObjectExpression expr1, ITypeBinding value2) {
            if (compOp != ComparisonOperator.IS && compOp != ComparisonOperator.ISNOT) {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compOp.");
            }
            if (expr1 == null) {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr1.");
            }
            if (value2 == null) {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr2.");
            }
            compOperator = compOp;
            Expr1 = expr1;
            typeBinding = value2;
        }

        public ComparisonOperator Operator {
            get {
                return compOperator;
            }
        }

        public Boolean InvolvesCodeExecution() {
            return Expr1.InvolvesCodeExecution();
        }

        /// <summary>
        /// Calculates the truth value of this operation when evaluated on an input object.
        /// All properties in this operation are evaluated on the input object.
        /// </summary>
        /// <param name="obj">The object on which to evaluate this operation.</param>
        /// <returns>The truth value of this operation when evaluated on the input object.</returns>
        public TruthValue Evaluate(IObjectView obj) {
            Debug.Assert(compOperator == ComparisonOperator.IS || compOperator == ComparisonOperator.ISNOT,
                "Comparison operator of IsTypePredicate should be either IS or ISNOT.");

            IObjectView value1 = Expr1.EvaluateToObject(obj);
            if (typeBinding == null || value1 == null) return TruthValue.UNKNOWN;
            ITypeBinding tval1 = value1.TypeBinding;
            if (tval1 is TypeBinding && typeBinding is TypeBinding)
                if (((TypeBinding)tval1).SubTypeOf((TypeBinding)typeBinding))
                    if (compOperator == ComparisonOperator.IS)
                        return TruthValue.TRUE;
                    else return TruthValue.FALSE;
                else if (compOperator == ComparisonOperator.IS)
                    return TruthValue.FALSE;
                else return TruthValue.TRUE;

            return TruthValue.UNKNOWN; // Same as in cast
        }
    
        /// <summary>
        /// Calculates the Boolean value of this operation when evaluated on an input object.
        /// All properties in this operation are evaluated on the input object.
        /// </summary>
        /// <param name="obj">The object on which to evaluate this operation.</param>
        /// <returns>The Boolean value of this operation when evaluated on the input object.</returns>
        public Boolean Filtrate(IObjectView obj) {
            return Evaluate(obj) == TruthValue.TRUE;
        }

        /// <summary>
        /// Compares object expression and type expression if possible.
        /// It tries to evaluate object expression, if not it uses typebinding from the object expression.
        /// If type binding of object is used, then the result might be not valid at running time.
        /// This method can be called only during optimization time, before constructing enumerator.
        /// </summary>
        /// <returns>Result of comparison</returns>
        public IsTypeCompare EvaluateAtCompile() {
            IObjectView obj = Expr1.EvaluateToObject(null);
            if (obj != null) {
                TruthValue res = Evaluate(obj);
                if (res == TruthValue.TRUE)
                    return IsTypeCompare.TRUE;
                if (res == TruthValue.FALSE)
                    return IsTypeCompare.FALSE;
            }
            // Object is null or result is unknown
            ITypeBinding objType = obj == null ? Expr1.TypeBinding : obj.TypeBinding; // Object type cannot be null
            if (objType == typeBinding)
                return IsTypeCompare.EQUAL;
            if (objType is TypeBinding && typeBinding is TypeBinding)
                if (((TypeBinding)objType).SubTypeOf((TypeBinding)typeBinding))
                    return IsTypeCompare.SUBTYPE;
                else if (((TypeBinding)typeBinding).SubTypeOf((TypeBinding)objType))
                    return IsTypeCompare.SUPERTYPE;
                else return IsTypeCompare.FALSE;
            return IsTypeCompare.UNKNOWN;
        }

        /// <summary>
        /// Creates an more instantiated copy of this expression by evaluating it on a Row.
        /// Properties, with extent numbers for which there exist objects attached to the Row,
        /// are evaluated and instantiated to literals, other properties are not changed.
        /// </summary>
        /// <param name="obj">The Row on which to evaluate the expression.</param>
        /// <returns>A more instantiated expression.</returns>
        public ILogicalExpression Instantiate(Row obj) {
            return new IsTypePredicate(compOperator, Expr1.Instantiate(obj), typeBinding);
        }

        public void InstantiateExtentSet(ExtentSet extentSet) {
            Expr1.InstantiateExtentSet(extentSet);
        }

        /// <summary>
        /// Gets a path to the given extent.
        /// The path is used for an index scan for the extent with the input extent number, 
        /// if there is such a path and if there is a corresponding index.
        /// </summary>
        /// <param name="extentNum">Input extent number.</param>
        /// <returns>A path, if an appropriate path is found, otherwise null.</returns>
        public IPath GetPathTo(Int32 extentNum) {
            return null;
        }

        public RangePoint CreateRangePoint(Int32 extentNumber, String strPath) {
            throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED);
        }
        
        public ILogicalExpression Clone(VariableArray varArray) {
            return new IsTypePredicate(compOperator, Expr1.CloneToObject(varArray), typeBinding);
        }

        public void BuildString(MyStringBuilder stringBuilder, Int32 tabs) {
            stringBuilder.AppendLine(tabs, "IsTypePredicate(");
            stringBuilder.AppendLine(tabs + 1, compOperator.ToString());
            Expr1.BuildString(stringBuilder, tabs + 1);
            stringBuilder.AppendLine(tabs + 1, typeBinding.Name);
            stringBuilder.AppendLine(tabs, ")");
        }
        
        /// <summary>
        /// Generates compilable code representation of this data structure.
        /// </summary>
        public void GenerateCompilableCode(CodeGenStringGenerator stringGen) {
            throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED);
        }

        // Append this node to filter instructions and leaves.
        public UInt32 AppendToInstrAndLeavesList(List<CodeGenFilterNode> dataLeaves,
                                                          CodeGenFilterInstrArray instrArray,
                                                          Int32 currentExtent,
                                                          StringBuilder filterText) {
            throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED);
        }

#if DEBUG
        private bool AssertEqualsVisited = false;
        public bool AssertEquals(ILogicalExpression other) {
            IsTypePredicate otherNode = other as IsTypePredicate;
            Debug.Assert(otherNode != null);
            return this.AssertEquals(otherNode);
        }
        internal bool AssertEquals(IsTypePredicate other) {
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
            Debug.Assert(this.compOperator == other.compOperator);
            if (this.compOperator != other.compOperator)
                return false;
            Debug.Assert(this.typeBinding.Name == other.typeBinding.Name);
            if (this.typeBinding.Name != other.typeBinding.Name)
                return false;
            // Check references. This should be checked if there is cyclic reference.
            AssertEqualsVisited = true;
            bool areEquals = true;
            if (this.Expr1 == null) {
                Debug.Assert(other.Expr1 == null);
                areEquals = other.Expr1 == null;
            } else
                areEquals = this.Expr1.AssertEquals(other.Expr1);
            AssertEqualsVisited = false;
            return areEquals;
        }
#endif
    }
}