using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Starcounter.Binding;
using Starcounter.Query.Execution;
using Starcounter.Query.Optimization;
using Starcounter.Query.Sql;

namespace Starcounter.Query.RawParserAnalyzer
{
    internal partial class ParserAnalyzerHelloTest
    {
        /// <summary>
        /// Analyze select statement and creates structures for optimizer.
        /// It replaces part of OptimizeAndCreateEnumerator, CreateNodeTree and
        /// others from _Creator.cs
        /// </summary>
        /// <param name="stmt"></param>
        internal unsafe void AnalyzeSelectStmt(SelectStmt* stmt) {
            // Read and assert the input tree
            Debug.Assert(JoinTree == null && WhereCondition == null && VarArray == null, "The variables for the result of analyzer should be reset.");

            // Process FROM (fromClause)
            SQLParserAssert(stmt->fromClause == null, "From clause is not yet supported");

            //Process SELECT clause (targetList)
            List* selectClause = stmt->targetList;
            Debug.Assert(selectClause->length > 0, "Empty targetList of select statement");

        //internal List* distinctClause; /* NULL, list of DISTINCT ON exprs, or
        //                         * lcons(NIL,NIL) for all (SELECT DISTINCT) */
        //internal Node* intoClause;		/* target for SELECT INTO / CREATE TABLE AS */
        //internal Node* whereClause;	/* WHERE qualification */
        //internal List* groupClause;	/* GROUP BY clauses */
        //internal Node* havingClause;	/* HAVING conditional-expression */
        //internal List* windowClause;	/* WINDOW window_name AS (...), ... */
        //internal Node* withClause;		/* WITH clause */

        /*
         * In a "leaf" node representing a VALUES list, the above fields are all
         * null, and instead this field is set.  Note that the elements of the
         * sublists are just expressions, without ResTarget decoration. Also note
         * that a list element can be DEFAULT (represented as a SetToDefault
         * node), regardless of the context of the VALUES list. It's up to parse
         * analysis to reject that where not valid.
         */
        //internal List* valuesLists;	/* untransformed list of expression lists */

        /*
         * These fields are used in both "leaf" SelectStmts and upper-level
         * SelectStmts.
         */
        //internal List* sortClause;		/* sort clause (a list of SortBy's) */
        //internal Node* limitOffset;	/* # of result tuples to skip */
        ////internal Node* limitCount;		/* # of result tuples to return */
        //internal List* lockingClause;	/* FOR UPDATE (list of LockingClause's) */
        //internal List* optionClause;   /* option clause defining hints*/

        /*
         * These fields are used only in upper-level SelectStmts.
         */
        //internal SetOperation op;			/* type of set op */
        //internal bool all;			/* ALL specified? */
        //internal SelectStmt* larg;	/* left child */
        //internal SelectStmt* rarg;	/* right child */
            // Assert non-implemented scenarios

            // Process
        }

    }
}
