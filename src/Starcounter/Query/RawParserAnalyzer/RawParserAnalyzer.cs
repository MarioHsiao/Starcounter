using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Starcounter;
using Starcounter.Query.Execution;
using Starcounter.Query.Optimization;

namespace Sc.Query.RawParserAnalyzer
{
    /// <summary>
    /// Contains methods to analyze raw parsed tree and generate necessary structures for current optimizer.
    /// </summary>
    internal partial class RawParserAnalyzer
    {
        /// <summary>
        /// Keeps knowledge if an open parser exists in this thread. It is important to have maximum one open parser per thread.
        /// </summary>
        [ThreadStatic]
        private static bool IsOpenParserThread = false;

        internal String Query { get; private set; }

        /// <summary>
        /// Contains joins of relations. It is constructed from all clauses of select statement.
        /// It consists of relations mentioned in FROM clause and path expressions - in all clauses.
        /// </summary>
        internal IOptimizationNode JoinTree { get; private set; }

        /// <summary>
        /// Contains logical condition of the query. Cannot be null for optimizer. At least TRUE.
        /// </summary>
        internal ConditionDictionary WhereCondition { get; private set; }

        internal INumericalExpression FetchNumExpr { get; private set; }
        internal IBinaryExpression FetchOffsetKeyExpr { get; private set; }
        internal HintSpecification HintSpec { get; private set; }
        internal VariableArray VarArray { get; private set; }

        internal IExecutionEnumerator OptimizedPlan { get; private set; }

        /// <summary>
        /// Calls unmanaged bison-based parser and then managed analyzer for the query.
        /// Generates all necessary structures for original optimizer and fills the values to 
        /// corresponding class properties.
        /// </summary>
        /// <param name="query">Query to process</param>
        internal unsafe void ParseAndAnalyzeQuery(string query)
        {
            IsOpenParserThread = true; // Important to avoid destroying global variables in unmanaged parser.
            Query = query;
            // Reset variables with structures for optimizer
            JoinTree = null;
            WhereCondition = null;
            FetchNumExpr = null;
            FetchOffsetKeyExpr = null;
            HintSpec = null;
            VarArray = null;
            // Call parser
            int scerrorcode = 0;
            unsafe
            {
                // The result error code. If 0 then parsing was successful.
                // Calls unmanaged parser, which returns the parsed tree
                Sc.Query.RawParserAnalyzer.List* parsedTree = UnmanagedParserInterface.ParseQuery(query, &scerrorcode);
                try
                {
                    // Throw exception if error
                    RawParserError(scerrorcode);
                    // Call analyzer, which can throw exception for errors
                    AnalyzeParseTree(parsedTree);
                }
                finally
                {
                    UnmanagedParserInterface.CleanMemoryContext(); // Otherwise memory leaks
                    IsOpenParserThread = false; // Important to allow calling parser again
                }
            }
        }

        internal unsafe void ParseQuery(string query)
        {
            IsOpenParserThread = true; // Important to avoid destroying global variables in unmanaged parser.
            int scerrorcode = 0;
            unsafe
            {
                // The result error code. If 0 then parsing was successful.
                // Calls unmanaged parser, which returns the parsed tree
                Sc.Query.RawParserAnalyzer.List* parsedTree = UnmanagedParserInterface.ParseQuery(query, &scerrorcode);
                UnmanagedParserInterface.CleanMemoryContext(); // Otherwise memory leaks
            }
            IsOpenParserThread = false; // Important to allow calling parser again
        }

        /// <summary>
        /// Entry point of analyzer.
        /// </summary>
        /// <param name="parsedTree">Parsed tree produced by the unmanaged bison-based parser.</param>
        internal unsafe void AnalyzeParseTree(Sc.Query.RawParserAnalyzer.List* parsedTree)
        {
            Debug.Assert(parsedTree != null, "Parsed tree should not be null");
            Debug.Assert(parsedTree->type == NodeTag.T_List, "Parsed tree should be of T_List, but was " + parsedTree->type.ToString());
            if (parsedTree->length > 1) // Parser can parse several statements in a string and thus produce list of trees. See test example.
                throw ErrorCode.ToException(Error.SCERRSQLINCORRECTSYNTAX, "The query should contain only one statement." +
                    LocationMessageForError((Node*)parsedTree->tail->data.ptr_value));
            Node* stmt = (Node*)parsedTree->head->data.ptr_value;
            switch (stmt->type)
            {
                case NodeTag.T_SelectStmt: AnalyzeSelectStmt((SelectStmt*)stmt);
                    break;
                default: UnknownNode(stmt);
                    break;
            }
        }

        /// <summary>
        /// Calls original optimizer on results of analyzer.
        /// </summary>
        internal void Optimize()
        {
            Debug.Assert(JoinTree != null && WhereCondition != null && HintSpec != null, "Query should parsed and analyzed before optimization");
            OptimizedPlan = Optimizer.Optimize(JoinTree, WhereCondition, FetchNumExpr, FetchOffsetKeyExpr, HintSpec);
        }
    }
}
