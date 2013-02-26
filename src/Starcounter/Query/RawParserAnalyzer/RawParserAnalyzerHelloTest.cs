using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Starcounter;
using Starcounter.Query.Execution;
using Starcounter.Query.Optimization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Starcounter.Binding;

[assembly: InternalsVisibleTo("Starcounter.SqlParser.Tests")]
namespace Starcounter.Query.RawParserAnalyzer
{
    /// <summary>
    /// Contains methods to analyze raw parsed tree and generate necessary structures for current optimizer.
    /// </summary>
    internal partial class ParserAnalyzerHelloTest : IDisposable
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
        internal INumericalExpression FethcOffsetExpr { get; private set; }
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
                List* parsedTree = UnmanagedParserInterface.ParseQuery(query, &scerrorcode);
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

        /// <summary>
        /// Calls parser for a query. If parsing fails then exception is thrown.
        /// </summary>
        /// <param name="query">The query to parse.</param>
        /// <returns></returns>
        internal unsafe void ParseQuery(string query)
        {
            if (query == "") return;
            IsOpenParserThread = true; // Important to avoid destroying global variables in unmanaged parser.
            Query = query;
            int scerrorcode = 0;
            unsafe
            {
                // The result error code. If 0 then parsing was successful.
                // Calls unmanaged parser, which returns the parsed tree
                List* parsedTree = UnmanagedParserInterface.ParseQuery(query, &scerrorcode);
                try {
                    // Throw exception if error
                    RawParserError(scerrorcode);
                } finally {
                    UnmanagedParserInterface.CleanMemoryContext(); // Otherwise memory leaks
                    IsOpenParserThread = false; // Important to allow calling parser again
                }
            }
        }

        /// <summary>
        /// Calls parser for a quey. If error is unexpected then exception is thrown.
        /// </summary>
        /// <param name="query">The query to parser.</param>
        /// <param name="errorExpected">If error expected or not</param>
        /// <returns>Error code.</returns>
        internal unsafe int ParseQuery(string query, bool errorExpected) {
            IsOpenParserThread = true; // Important to avoid destroying global variables in unmanaged parser.
            Query = query;
            int scerrorcode = 0;
            unsafe {
                // The result error code. If 0 then parsing was successful.
                // Calls unmanaged parser, which returns the parsed tree
                List* parsedTree = UnmanagedParserInterface.ParseQuery(query, &scerrorcode);
                try {
                    if (!errorExpected)
                        // Throw exception if error
                        RawParserError(scerrorcode);
                    else
                        Console.WriteLine(GetErrorMessage(scerrorcode));
                } finally {
                    UnmanagedParserInterface.CleanMemoryContext(); // Otherwise memory leaks
                    IsOpenParserThread = false; // Important to allow calling parser again
                }
            }
            return scerrorcode;
        }

        /// <summary>
        /// Entry point of analyzer.
        /// </summary>
        /// <param name="parsedTree">Parsed tree produced by the unmanaged bison-based parser.</param>
        internal unsafe void AnalyzeParseTree(List* parsedTree)
        {
            Debug.Assert(parsedTree != null, "Parsed tree should not be null");
            Debug.Assert(parsedTree->type == NodeTag.T_List, "Parsed tree should be of T_List, but was " + parsedTree->type.ToString());
            if (parsedTree->length > 1) // Parser can parse several statements in a string and thus produce list of trees. See test example.
                throw ErrorCode.ToException(Error.SCERRSQLINCORRECTSYNTAX, "The query should contain only one statement." +
                    LocationMessageForError((Node*)parsedTree->tail->data.ptr_value));
            Node* stmt = (Node*)parsedTree->head->data.ptr_value;
            switch (stmt->type)
            {
                case NodeTag.T_SelectStmt: TestAnalyzeSelectStmt((SelectStmt*)stmt);
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
            OptimizedPlan = Optimizer.Optimize(JoinTree, WhereCondition, FetchNumExpr, FethcOffsetExpr, FetchOffsetKeyExpr, HintSpec);
        }

        /// <summary>
        /// Checks if native parser was closed, i.e., memory was cleaned. If not then calls memory clean up.
        /// </summary>
        public void Dispose() {
            if (IsOpenParserThread) {
                UnmanagedParserInterface.CleanMemoryContext();
                IsOpenParserThread = false;
            }
        }

        internal unsafe void TestAnalyzeSelectStmt(SelectStmt* stmt) {
            // Read and assert the input tree
            Debug.Assert(JoinTree == null && WhereCondition == null && VarArray == null, "The variables for the result of analyzer should be reset.");
            // Let's go through FROM clause first
            List* fromClause = stmt->fromClause;
            // Assume only one relation in from clause
            SQLParserAssert(fromClause->length == 1, "Assuming one relation in from clause only");
            SQLParserAssert(((Node*)fromClause->head->data.ptr_value)->type == NodeTag.T_RangeVar, "Expected T_RangeVar, but got " + ((Node*)fromClause->head->data.ptr_value)->type.ToString());
            RangeVar* extent = (RangeVar*)fromClause->head->data.ptr_value;
            List* selectClause = stmt->targetList;
            SQLParserAssert(selectClause->length == 1, "Assuming projection of an alias");
            SQLParserAssert(((Node*)selectClause->head->data.ptr_value)->type == NodeTag.T_ResTarget, "Expected T_ResTarget, but got " + ((Node*)selectClause->head->data.ptr_value)->type.ToString());
            ResTarget* target = (ResTarget*)selectClause->head->data.ptr_value;
            SQLParserAssert(target->name == null);
            SQLParserAssert(target->val->type == NodeTag.T_List, "Expected T_List, but got " + target->val->type.ToString());
            SQLParserAssert(((List*)target->val)->length == 1, "Expected list with one element - alias access");
            SQLParserAssert(((Node*)((List*)target->val)->head->data.ptr_value)->type == NodeTag.T_ColumnRef, "Expected T_ColumnRef, but got " +
                ((Node*)((List*)target->val)->head->data.ptr_value)->type.ToString());
            ColumnRef* col = (ColumnRef*)((List*)target->val)->head->data.ptr_value;
            SQLParserAssert(col->name != null, "Assuming alias name");
            //SQLParserAssert(val->type == NodeTag.T_String, "Expected T_String, but got " + val->type.ToString());
            SQLParserAssert(extent->alias != null, "Assuming that alias is given after the extent name");
            SQLParserAssert(extent->alias->aliasname == col->name, "Assuming that aliases are equivalent");
            SQLParserAssert(stmt->sortClause == null, "Assuming no order by");
            SQLParserAssert(stmt->whereClause == null, "Assuming no where clause");
            SQLParserAssert(stmt->optionClause == null, "Assuming no option clause with optimizer hints");
            // Creating output structures
            RowTypeBinding typeBindings = new RowTypeBinding();
            Int32 extNum = 0;
            TypeBinding extType = GetTypeBindingFor(extent);
            typeBindings.AddTypeBinding(extType);
            // Add projection to typebinding
            IValueExpression propExpr = new ObjectThis(extNum, extType);
            typeBindings.AddPropertyMapping(extNum.ToString(), propExpr);
            VarArray = new VariableArray(0);
            if ((typeBindings.PropertyCount == 1) && (typeBindings.GetPropertyBinding(0).TypeCode == DbTypeCode.Object))
                VarArray.QueryFlags = VarArray.QueryFlags | QueryFlags.SingletonProjection;
            JoinTree = new ExtentNode(typeBindings, 0, VarArray, Query);
            WhereCondition = new ConditionDictionary();
            ILogicalExpression whereCond = new LogicalLiteral(TruthValue.TRUE);
            WhereCondition.AddCondition(whereCond);
            HintSpec = new HintSpecification();
        }

        // I should investigate the exception first, since it might be not related
        internal unsafe String GetFullName(RangeVar* extent) {
            Debug.Assert(extent->path != null);
            Debug.Assert(extent->relname == null);
            ListCell* curCell = extent->path->head;
            Debug.Assert(curCell != null);
            Debug.Assert(((Node*)curCell->data.ptr_value)->type == NodeTag.T_ColumnRef, "Expected T_ColumnRef, but got " +
                ((Node*)curCell->data.ptr_value)->type.ToString());
            String name = ((ColumnRef*)curCell->data.ptr_value)->name;
            curCell = curCell->next;
            while (curCell != null) {
                name += '.';
                name += ((ColumnRef*)curCell->data.ptr_value)->name;
                curCell = curCell->next;
            }
            return name;
        }

        internal unsafe TypeBinding GetTypeBindingFor(RangeVar* extent) {
            //Debug.Assert(extent->relname != null);
            String relName = GetFullName(extent);
            TypeBinding theType = null;
            try {
                theType = Bindings.GetTypeBindingInsensitive(relName);
            } catch (DbException ex) {
                throw ErrorCode.ToException(Error.SCERRSQLUNKNOWNNAME, ex, LocationMessageForError((Node*)extent, relName));
            }
            if (theType != null)
                return theType;
            //int res = TypeRepository.TryGetTypeBindingByShortName(shortname, out theType);
            //if (res == 1)
            //    return theType;
            throw ErrorCode.ToException(Error.SCERRSQLUNKNOWNNAME, LocationMessageForError((Node*)extent, relName));
        }

        internal bool CompareTo(IExecutionEnumerator otherOptimizedPlan) {
            String thisOptimizedPlanStr = Regex.Replace(this.OptimizedPlan.ToString(), "\\s", "");
            String otherOptimizedPlanStr = Regex.Replace(otherOptimizedPlan.ToString(), "\\s", "");
            return thisOptimizedPlanStr.Equals(otherOptimizedPlanStr);
            //return this.OptimizedPlan.ToString().Equals(otherOptimizedPlan.ToString());
        }

        /// <summary>
        /// Checks the error code if an error was returned by parser. If so the error 
        /// information is read from unmanaged parser and a Starcounter exception is
        /// thrown.
        /// </summary>
        /// <param name="scerrorcode">the code returned by the unmanaged parser. 
        /// 0 means no error.</param>
        internal unsafe void RawParserError(int scerrorcode) {
            Debug.Assert(IsOpenParserThread, "Raw parser error management requires an open parser.");
            if (scerrorcode > 0) {
                // Unmanaged parser returned an error, thus throwing an exception.
                unsafe {
                    ScError* scerror = UnmanagedParserInterface.GetScError();
                    // Throw Starcounter exception for parsing error
                    String message = new String(scerror->scerrmessage);
                    if (scerror->scerrposition >= 0)
                        message += " Position " + scerror->scerrposition + " in the query \"" + Query + "\"";
                    else
                        message += " in the query \"" + Query + "\"";
                    if (scerror->tocken != null)
                        message += "The error is near or at: " + scerror->tocken;
                    throw GetSqlException((uint)scerror->scerrorcode, message, scerror->scerrposition, scerror->tocken);
                }
            }
        }

        internal unsafe String GetErrorMessage(int scerrorcode) {
            if (scerrorcode == 0)
                return "No error";
            unsafe {
                ScError* scerror = UnmanagedParserInterface.GetScError();
                // Throw Starcounter exception for parsing error
                String message = new String(scerror->scerrmessage);
                if (scerror->scerrposition >= 0)
                    message += " Position " + scerror->scerrposition + " in the query \"" + Query + "\"";
                else
                    message += " in the query \"" + Query + "\"";
                if (scerror->tocken != null)
                    message += "The error is near or at: " + scerror->tocken;
                return message;
            }
        }

        /// <summary>
        /// Generates a string reporting position and token in the given query for an error.
        /// </summary>
        /// <param name="node">Node of unmanaged tree where error happened.</param>
        /// <returns>Part of error message about location of the error.</returns>
        internal unsafe String LocationMessageForError(Node* node) {
            return LocationMessageForError(node, node->type.ToString());
        }

        internal unsafe String LocationMessageForError(Node* node, String token) {
            return "Position " + UnmanagedParserInterface.Location(node) + " in the query \"" + Query + "\"" +
                ". The error is near or at: " + token;
        }

        // Proper error should be returned from here.
        internal unsafe void UnknownNode(Node* node) {
            throw new SQLParserAssertException();
            //throw GetSqlException(Error.SCERRSQLNOTIMPLEMENTED, "The statement or clause is not implemented. " + LocationMessageForError(node),
            //    UnmanagedParserInterface.Location(node), node->type.ToString());
        }

        /// <summary>
        /// Has to be called to assert if temporal assumption holds.
        /// If not Debug.Assert is called and an exception is thrown to catch in the parent code and do different condition.
        /// ONLY FOR DEVELOPMENT PURPOSE
        /// </summary>
        /// <param name="condition">The condition to check</param>
        internal void SQLParserAssert(bool condition) {
            Debug.Assert(condition);
            if (!condition)
                throw new SQLParserAssertException();
        }

        /// <summary>
        /// Has to be called to assert if temporal assumption holds.
        /// If not Debug.Assert is called and an exception is thrown to catch in the parent code and do different condition.
        /// ONLY FOR DEVELOPMENT PURPOSE
        /// </summary>
        /// <param name="condition">The condition to check</param>
        /// <param name="message">Adds message to Debug.Assert</param>
        internal void SQLParserAssert(bool condition, string message) {
            Debug.Assert(condition, message);
            if (!condition)
                throw new SQLParserAssertException();
        }

        internal static Exception GetSqlException(uint errorCode, string message, int location, string token) {
            List<string> tokens = new List<string>(1);
            tokens.Add(token);
            return ErrorCode.ToException(errorCode, message, (m, e) => new SqlException(m, tokens, location));
        }
    }

    /// <summary>
    /// Exception class used during development to trigger that this parser cannot be used.
    /// </summary>
    internal class SQLParserAssertException : Exception {
    }
}
