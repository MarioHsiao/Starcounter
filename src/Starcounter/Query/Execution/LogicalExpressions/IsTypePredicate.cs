using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Starcounter.Binding;
using Starcounter.Query.Optimization;

namespace Starcounter.Query.Execution {
    internal class IsTypePredicate : IComparison {
        ComparisonOperator compOperator;
        IObjectExpression objExpr;
        ITypeExpression typeExpr;
        ITypeBinding typeBinding;

        internal IsTypePredicate(ComparisonOperator compOp, IObjectExpression expr1, ITypeExpression expr2)
        {
            if (compOp != ComparisonOperator.IS && compOp != ComparisonOperator.ISNOT)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compOp.");
            }
            if (expr1 == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr1.");
            }
            if (expr2 == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr2.");
            }
            compOperator = compOp;
            objExpr = expr1;
            typeExpr = expr2;
            typeBinding = null;
        }

        internal IsTypePredicate(ComparisonOperator compOp, IObjectExpression expr, ITypeBinding value)
        {
            if (compOp != ComparisonOperator.IS && compOp != ComparisonOperator.ISNOT)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compOp.");
            }
            if (expr == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr.");
            }
            if (value == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect value.");
            }
            compOperator = compOp;
            objExpr = expr;
            typeExpr = null;
            typeBinding = value;
        }

        public ComparisonOperator Operator {
            get {
                return compOperator;
            }
        }

        internal ITypeBinding GetTypeBinding() {
            return typeBinding == null ? typeExpr.EvaluateToType(null) : typeBinding;
        }

        public Boolean InvolvesCodeExecution() {
            return objExpr.InvolvesCodeExecution();
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

            // If there is a typeExpr then create a typeBinding from that.
            if (typeExpr != null)
                typeBinding = typeExpr.EvaluateToType(obj);

            IObjectView objValue = objExpr.EvaluateToObject(obj);
            if (typeBinding == null || objValue == null) 
                return TruthValue.UNKNOWN;
            ITypeBinding objTypeBind = objValue.TypeBinding;
            if (objTypeBind is TypeBinding && typeBinding is TypeBinding)
            {
                if (((TypeBinding)objTypeBind).SubTypeOf((TypeBinding)typeBinding))
                {
                    if (compOperator == ComparisonOperator.IS)
                        return TruthValue.TRUE;
                    else
                        return TruthValue.FALSE;
                }
                else
                {
                    if (compOperator == ComparisonOperator.IS)
                        return TruthValue.FALSE;
                    else
                        return TruthValue.TRUE;
                }
            }
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
            // Object is null or result is unknown
            ITypeBinding objType = objExpr.TypeBinding; // Object type cannot be null
            ITypeBinding typeType = typeBinding == null ? typeExpr.EvaluateToType(null) : typeBinding;
            if (objType is TypeBinding && typeType is TypeBinding)
                if (objType == typeType)
                    return IsTypeCompare.EQUAL;
                else if (((TypeBinding)objType).SubTypeOf((TypeBinding)typeType))
                    return IsTypeCompare.SUBTYPE;
                else if (((TypeBinding)typeType).SubTypeOf((TypeBinding)objType))
                    return IsTypeCompare.SUPERTYPE;
                else return IsTypeCompare.FALSE;
            if (objType is TypeBinding)
                return IsTypeCompare.UNKNOWNTYPE;
            if (typeType is TypeBinding)
                return IsTypeCompare.UNKNOWNOBJECT;
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
            if (typeExpr != null)
                return new IsTypePredicate(compOperator, objExpr.Instantiate(obj), typeExpr);
            //else
            return new IsTypePredicate(compOperator, objExpr.Instantiate(obj), typeBinding);
        }

        public void InstantiateExtentSet(ExtentSet extentSet) {
            objExpr.InstantiateExtentSet(extentSet);
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

        internal IsTypeCompare CompareTypeTo(IsTypePredicate otherPredicate) {
            TypeBinding thisType = this.typeExpr.EvaluateToType(null) as TypeBinding;
            TypeBinding otherType = otherPredicate.GetTypeBinding() as TypeBinding;
            if (thisType == null || otherType == null)
                return IsTypeCompare.FALSE;
            if (thisType.LowerName == otherType.LowerName)
                return IsTypeCompare.EQUAL;
            if (thisType.SubTypeOf(otherType))
                return IsTypeCompare.SUBTYPE;
            else if (otherType.SubTypeOf(thisType))
                return IsTypeCompare.SUPERTYPE;
            else return IsTypeCompare.FALSE;
        }

        public RangePoint CreateRangePoint(Int32 extentNumber, String strPath) {
            return null;
        }
        
        public ILogicalExpression Clone(VariableArray varArray) {
            if (typeExpr != null)
                return new IsTypePredicate(compOperator, objExpr.CloneToObject(varArray), typeExpr.CloneToType(varArray));
            // else
            return new IsTypePredicate(compOperator, objExpr.CloneToObject(varArray), typeBinding);
        }

        public ExtentSet GetOutsideJoinExtentSet()
        {
            return null;
        }

        public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
        {
            stringBuilder.AppendLine(tabs, "IsTypePredicate(");
            stringBuilder.AppendLine(tabs + 1, compOperator.ToString());
            objExpr.BuildString(stringBuilder, tabs + 1);
            if (typeBinding == null)
                typeExpr.BuildString(stringBuilder, tabs + 1);
            else
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
            if (this.typeBinding == null) {
                Debug.Assert(other.typeBinding == null);
                if (other.typeBinding != null)
                    return false;
            } else {
                Debug.Assert(this.typeBinding.Name == other.typeBinding.Name);
                if (this.typeBinding.Name != other.typeBinding.Name)
                    return false;
            }
            // Check references. This should be checked if there is cyclic reference.
            AssertEqualsVisited = true;
            bool areEquals = true;
            if (this.objExpr == null) {
                Debug.Assert(other.objExpr == null);
                areEquals = other.objExpr == null;
            } else
                areEquals = this.objExpr.AssertEquals(other.objExpr);
            if (areEquals)
                if (this.typeExpr == null) {
                    Debug.Assert(other.typeExpr == null);
                    areEquals = other.typeExpr == null;
                } else
                    areEquals = this.typeExpr.AssertEquals(other.typeExpr);
            AssertEqualsVisited = false;
            return areEquals;
        }
#endif
    }
}