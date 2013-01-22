//using System;
//using Starcounter.Binding;

//namespace Starcounter.Query.Execution {
//    internal class IsTypePredicate : IComparison {
//        ComparisonOperator CompOperator;
//        IObjectExpression Expr1;
//        ITypeBinding Value2;

//        internal IsTypePredicate(ComparisonOperator compOp, IObjectExpression expr1, ITypeBinding value2) {
//            if (compOp != ComparisonOperator.IS && compOp != ComparisonOperator.ISNOT) {
//                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compOp.");
//            }
//            if (expr1 == null) {
//                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr1.");
//            }
//            if (value2 == null) {
//                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr2.");
//            }
//            CompOperator = compOp;
//            Expr1 = expr1;
//            Value2 = value2;
//        }

//        public ComparisonOperator Operator {
//            get {
//                return CompOperator;
//            }
//        }

//        public Boolean InvolvesCodeExecution() {
//            return Expr1.InvolvesCodeExecution();
//        }

//        /// <summary>
//        /// Calculates the truth value of this operation when evaluated on an input object.
//        /// All properties in this operation are evaluated on the input object.
//        /// </summary>
//        /// <param name="obj">The object on which to evaluate this operation.</param>
//        /// <returns>The truth value of this operation when evaluated on the input object.</returns>
//        public TruthValue Evaluate(IObjectView obj) {
//            IObjectView value1 = Expr1.EvaluateToObject(obj);
//            //IObjectView value2 = Expr2.EvaluateToType(obj);
//            switch (CompOperator) {
//                case ComparisonOperator.IS:
//                    if (value1 == null && value2 == null) {
//                        return TruthValue.TRUE;
//                    }
//                    if (value1 == null || value2 == null) {
//                        return TruthValue.FALSE;
//                    }
//                    if (value1.Equals(value2)) {
//                        return TruthValue.TRUE;
//                    }
//                    return TruthValue.FALSE;
//                case ComparisonOperator.ISNOT:
//                    if (value1 == null && value2 == null) {
//                        return TruthValue.FALSE;
//                    }
//                    if (value1 == null || value2 == null) {
//                        return TruthValue.TRUE;
//                    }
//                    if (value1.Equals(value2)) {
//                        return TruthValue.FALSE;
//                    }
//                    return TruthValue.TRUE;
//                default:
//                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compOperator: " + compOperator);
//            }
//        }
//    }
//}