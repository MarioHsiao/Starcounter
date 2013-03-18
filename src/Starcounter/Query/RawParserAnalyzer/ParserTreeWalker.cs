using System;
using System.Diagnostics;

namespace Starcounter.Query.RawParserAnalyzer {
    /// <summary>
    /// Entry point to call parser and then to traverse the AST.
    /// </summary>
    internal partial class ParserTreeWalker : IDisposable {

        /// <summary>
        /// Keeps knowledge if an open parser exists in this thread. It is important to have maximum one open parser per thread.
        /// </summary>
        [ThreadStatic]
        private static bool IsOpenParserThread = false;

        internal String Query { get; private set; }

        /// <summary>
        /// Calls unmanaged bison-based parser and then managed analyzer for the query.
        /// Generates all necessary structures for original optimizer and fills the values to 
        /// corresponding class properties.
        /// </summary>
        /// <param name="query">Query to process</param>
        /// <param name="consumer">Interface object to be called during tree walk</param>
        internal unsafe void ParseQueryAndWalkTree(string query, IParserTreeAnalyzer consumer) {
            //OnEmptyQueryError(query);
            IsOpenParserThread = true; // Important to avoid destroying global variables in unmanaged parser.
            Query = query;
            // Call parser
            int scerrorcode = 0;
            unsafe {
                // The result error code. If 0 then parsing was successful.
                // Calls unmanaged parser, which returns the parsed tree
                List* parsedTree = UnmanagedParserInterface.ParseQuery(query, &scerrorcode);
                try {
                    // Throw exception if error
                    RawParserError(scerrorcode);
                    // Call analyzer, which can throw exception for errors
                    WalkParsedTree(parsedTree, consumer);
                } finally {
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
        internal unsafe void ParseQuery(string query) {
            //OnEmptyQueryError(query);
            IsOpenParserThread = true; // Important to avoid destroying global variables in unmanaged parser.
            Query = query;
            int scerrorcode = 0;
            unsafe {
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
            //OnEmptyQueryError(query);
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
        /// <param name="consumer">Interface object to be called during tree walk</param>
        internal unsafe void WalkParsedTree(List* parsedTree, IParserTreeAnalyzer consumer) {
            Debug.Assert(parsedTree != null, "Parsed tree should not be null");
            Debug.Assert(parsedTree->type == NodeTag.T_List, "Parsed tree should be of T_List, but was " + parsedTree->type.ToString());
            Debug.Assert(parsedTree->length == 1, "The query should contain only one statement.");
            Node* stmt = (Node*)parsedTree->head->data.ptr_value;
            switch (stmt->type) {
                case NodeTag.T_SelectStmt: WalkSelectStmt((SelectStmt*)stmt, consumer);
                    break;
                default: UnknownNode(stmt);
                    break;
            }
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
    }
}
