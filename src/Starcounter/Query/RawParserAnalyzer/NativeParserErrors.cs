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
                unsafe {
                    String message = GetErrorMessage(scerrorcode);
                    ScError* scerror = UnmanagedParserInterface.GetScError();
                    // Throw Starcounter exception for parsing error
                    throw GetSqlException((uint)scerror->scerrorcode, message, scerror->scerrposition, scerror->tocken);
                }
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
        internal static Exception GetSqlException(uint errorCode, string message, int location, string token) {
            List<string> tokens = new List<string>(1);
            tokens.Add(token);
            return ErrorCode.ToException(errorCode, message, (m, e) => new SqlException(m, tokens, location));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scerrorcode"></param>
        /// <returns></returns>
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
        /// Throws exception for the given node, since it was unexpected to get it in the parsed tree.
        /// </summary>
        /// <param name="node">The unknown node</param>
        internal unsafe void UnknownNode(Node* node) {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Parsed tree contains unexpected node "+node->type.ToString().Substring(2));
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
    }
}
