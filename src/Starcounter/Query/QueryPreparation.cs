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
#if HELLOTEST
            Starcounter.Query.RawParserAnalyzer.ParserAnalyzerHelloTest newAnalyzer = null;
#else
            Starcounter.Query.RawParserAnalyzer.MapParserTree newAnalyzer = null;
#endif
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
            try {
#if HELLOTEST
                newAnalyzer = new Starcounter.Query.RawParserAnalyzer.ParserAnalyzerHelloTest();
                newAnalyzer.ParseAndAnalyzeQuery(query);
#else
                newAnalyzer = new Starcounter.Query.RawParserAnalyzer.MapParserTree();
                Starcounter.Query.RawParserAnalyzer.ParserTreeWalker treeWalker = new Starcounter.Query.RawParserAnalyzer.ParserTreeWalker();
                treeWalker.ParseQueryAndWalkTree(query, newAnalyzer);
#endif
            } catch (Starcounter.Query.RawParserAnalyzer.SQLParserAssertException) {
                newAnalyzer = null;
            } catch (SqlException bisonAnalyzerException) {
                //Debug.Assert(prologException != null);
                throw bisonAnalyzerException;
                //throw prologException;
            }
            if (prologException != null)
                throw prologException;
#endif
#if BISON_ONLY
            if (newAnalyzer == null)
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Semantic analyzer is not implemented for this type of query parsed by Bison");
#endif
                // Check equality
#if DEBUG
            if (optArgsProlog != null && newAnalyzer != null) {
                Debug.Assert(newAnalyzer.JoinTree.AssertEquals(optArgsProlog.NodeTree), "Join trees are not the same!");
                Debug.Assert(newAnalyzer.WhereCondition.AssertEquals(optArgsProlog.ConditionDict), "Logical conditions are not the same!");
                if (newAnalyzer.FetchNumExpr == null)
                    Debug.Assert(optArgsProlog.FetchNumExpr == null, "Fetch limit expression is expected to be null.");
                else
                    Debug.Assert(newAnalyzer.FetchNumExpr.AssertEquals(optArgsProlog.FetchNumExpr), "Fetch limit expression is not the same!");
                if (newAnalyzer.FethcOffsetExpr == null)
                    Debug.Assert(optArgsProlog.FetchOffsetExpr == null, "Fetch offset expression is expected to be null.");
                else
                    Debug.Assert(newAnalyzer.FethcOffsetExpr.AssertEquals(optArgsProlog.FetchOffsetExpr), "Fetch limit expression is not the same!");
                if (newAnalyzer.FetchOffsetKeyExpr == null)
                    Debug.Assert(optArgsProlog.FetchOffsetKeyExpr == null, "Fetch offset key expression is expected to be null.");
                else
                    Debug.Assert(newAnalyzer.FetchOffsetKeyExpr.AssertEquals(optArgsProlog.FetchOffsetKeyExpr), "Fetch offset key expression is not the same");
                Debug.Assert(newAnalyzer.HintSpec.AssertEquals(optArgsProlog.HintSpec), "Hint expressions are not the same");
            }
#endif
            // Call to optimizer of Prolog result
            if (optArgsProlog != null)
                prologParsedQueryPlan = Optimizer.Optimize(optArgsProlog);

            // Call to optimizer of Bison result
            if (newAnalyzer != null)
                newAnalyzer.Optimize();

            // Check equality
#if DEBUG
            if (prologParsedQueryPlan != null && newAnalyzer != null) {
                String prologParsedQueryPlanStr = prologParsedQueryPlan.ToString();
                String bisonParsedQueryPlanStr = newAnalyzer.OptimizedPlan.ToString();
                Debug.Assert(bisonParsedQueryPlanStr == prologParsedQueryPlanStr, "Strings of executions plans should be equally");
                //Debug.Assert(newAnalyzer.CompareTo(prologParsedQueryPlan),"Query plans produces by Prolog-based and Bison-based optimizers should be the same.");
            }
#endif
            // Choose Bison based execution plan if available
            IExecutionEnumerator newEnum = newAnalyzer != null ? newAnalyzer.OptimizedPlan : prologParsedQueryPlan;

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
