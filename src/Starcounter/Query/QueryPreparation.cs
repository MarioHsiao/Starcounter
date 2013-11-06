using System;
using System.ComponentModel;
using System.Diagnostics;
using Starcounter.Binding;
using Starcounter.Query.Execution;
using Starcounter.Query.Optimization;
using Starcounter.Query.Sql;
using Starcounter.Query.SQL;

namespace Starcounter.Query {
    internal static class QueryPreparation {
        /// <summary>
        /// Prepares query by first parsing it and then optimizing. The preparation can be done in Prolog or Bison.
        /// By default the preparation is done in both and if debug the results are compares before and after optimization.
        /// LIKE case requires special treatment.
        /// </summary>
        /// <param name="query">Input query string to prepare</param>
        /// <returns>The result enumerated with the execution plan.</returns>
        internal static IExecutionEnumerator PrepareQuery<T>(String query) {
            OptimizerInput optArgsProlog = null;
            IExecutionEnumerator prologParsedQueryPlan = null;
            Exception prologException = null;
            // Call to Prolog parser and type checker
#if !BISON_ONLY
            // Call Prolog and get answer
            se.sics.prologbeans.QueryAnswer answer = null;
            try {
                answer = PrologManager.CallProlog(QueryModule.DatabaseId, query);
            } catch (SqlException e) {
                prologException = e;
            }
            // Transfer answer terms into pre-optimized structures
            if (prologException == null)
                optArgsProlog = PrologManager.ProcessPrologAnswer(answer, query);
#endif
            // Call to Bison parser and type checker
#if !PROLOG_ONLY
            uint err = SqlProcessor.SqlProcessor.CallSqlProcessor(query);
            Debug.Assert(err == 0);
            if (prologException != null)
                throw prologException;
#endif //!PROLOG_ONLY
#if BISON_ONLY
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Semantic analyzer is not implemented for this type of query parsed by Bison");
#endif // BISON_ONLY
            // Call to optimizer of Prolog result
            if (optArgsProlog != null)
                prologParsedQueryPlan = Optimizer.Optimize(optArgsProlog);

            // Check equality
            // Choose Bison based execution plan if available
            IExecutionEnumerator newEnum = prologParsedQueryPlan;

            // Checking if its LikeExecEnumerator.
            if (newEnum is LikeExecEnumerator) {
                (newEnum as LikeExecEnumerator).CreateLikeCombinations<T>();
            }
            MatchEnumeratorResultAndExpectedType<T>(newEnum);
            return newEnum;
        }

        internal static void MatchEnumeratorResultAndExpectedType<T>(IExecutionEnumerator execEnum) {
            // Check if Generic type corresponds to the query result type
            // dynamic type corresponds to Object
            Type expectedType = typeof(T);
            if (expectedType == typeof(Object))
                return;
            Type queryResultType;
            Boolean isValueType = false;
            if (execEnum.TypeBinding == null) {
                queryResultType = Type.GetType("System." + ((Starcounter.Binding.DbTypeCode)execEnum.ProjectionTypeCode).ToString());
                isValueType = true;
            } else {
                queryResultType = Type.GetType(execEnum.TypeBinding.Name);
                if (queryResultType == null)
                    if (execEnum.TypeBinding.Name == "Starcounter.Row")
                        queryResultType = typeof(Row);
                    else
                        foreach (var a in AppDomain.CurrentDomain.GetAssemblies()) {
                            queryResultType = a.GetType(execEnum.TypeBinding.Name);
                            if (queryResultType != null)
                                break;
                        }
            }
            Debug.Assert(queryResultType != null);
            if (!MatchResultTypeAndExpectedType(queryResultType, isValueType, expectedType))
                throw ErrorCode.ToException(Error.SCERRQUERYRESULTTYPEMISMATCH, "The result type " +
                    expectedType.FullName + " cannot be assigned from the query result, which is of type " +
                    queryResultType.FullName + ".");
        }

        public static Boolean MatchResultTypeAndExpectedType(Type sourceType, Boolean isSourceValueType, Type targetType) {
            if (sourceType.IsValueType) {
                Debug.Assert(isSourceValueType);
                return Check(sourceType, targetType);
            } else
                return targetType.IsAssignableFrom(sourceType);
        }

        public static bool Check(Type fromType, Type toType) {
            Type converterType = typeof(TypeConverterChecker<,>).MakeGenericType(fromType, toType);
            object instance = Activator.CreateInstance(converterType);
            return (bool)converterType.GetProperty("CanConvert").GetGetMethod().Invoke(instance, null);
        }

        public class TypeConverterChecker<TFrom, TTo> {
            public bool CanConvert { get; private set; }

            public TypeConverterChecker() {
                TFrom from = default(TFrom);
                if (from == null)
                    if (typeof(TFrom).Equals(typeof(String)))
                        from = (TFrom)(dynamic)"";
                    else
                        from = (TFrom)Activator.CreateInstance(typeof(TFrom));
                try {
                    TTo to = (dynamic)from;
                    CanConvert = true;
                } catch {
                    CanConvert = false;
                }
            }
        }
    }
}
