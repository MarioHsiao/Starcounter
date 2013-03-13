using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Query.RawParserAnalyzer {
    internal partial class ParserTreeWalker {
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
                // Throw Starcounter exception for parsing error
                throw GetSqlException(scerrorcode);
            }
        }

        /// <summary>
        /// Creates exception with error location and token by using Starcounter factory.
        /// </summary>
        /// <param name="errorCode">Starcounter error code</param>
        /// <param name="message">The detailed error message</param>
        /// <param name="location">Start of the error token in the query</param>
        /// <param name="token">The error token</param>
        /// <returns></returns>
        internal Exception GetSqlException(int scErrorCode) {
            unsafe {
                ScError* scerror = UnmanagedParserInterface.GetScError();
                String message = new String(scerror->scerrmessage);
                uint errorCode = (uint)scerror->scerrorcode;
                Debug.Assert(scErrorCode == errorCode);
                int position = scerror->scerrposition;
                String token = scerror->token;
                if (message == "syntax error" && token != null)
                    message = "Unexpected token.";
                if (message == "syntax error" && token == null)
                    message = "Unexpected end of query.";
                message += " The error near or at position " + position;
                if (token != null)
                    message += " near or at token: " + token;
                else
                    message += ".";
                return ErrorCode.ToException(errorCode, message, (m, e) => new SqlException(errorCode, m, position, token, Query));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scerrorcode"></param>
        /// <returns></returns>
        internal unsafe String GetErrorMessage(int scerrorcode) {
            if (scerrorcode == 0)
                return "No error";
            else
                return GetSqlException(scerrorcode).ToString();
        }

        /// <summary>
        /// Throws exception for the given node, since it was unexpected to get it in the parsed tree.
        /// </summary>
        /// <param name="node">The unknown node</param>
        internal unsafe void UnknownNode(Node* node) {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Parsed tree contains unexpected or unsupported node "+node->type.ToString().Substring(2));
        }

        /// <summary>
        /// Has to be called to assert if temporal assumption holds.
        /// If not Debug.Assert is called and an exception is thrown to catch in the parent code and do different condition.
        /// ONLY FOR DEVELOPMENT PURPOSE
        /// </summary>
        /// <param name="condition">The condition to check</param>
        /// <param name="message">Adds message to Debug.Assert</param>
        internal void SQLParserAssert(bool condition, string message) {
            //Debug.Assert(condition, message);
            if (!condition)
                throw new SQLParserAssertException();
        }

        internal static void OnEmptyQueryError(String query) {
            if (query == "")
                throw ErrorCode.ToException(Error.SCERRSQLINCORRECTSYNTAX, "Query string should not be empty",
                    (m, e) => new SqlException(Error.SCERRSQLINCORRECTSYNTAX, m, 1, query));
        }
    }
}
