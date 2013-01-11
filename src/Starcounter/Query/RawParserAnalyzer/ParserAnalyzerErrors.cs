using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Starcounter;

namespace Starcounter.Query.RawParserAnalyzer
{
    internal partial class ParserAnalyzer
    {
        /// <summary>
        /// Checks the error code if an error was returned by parser. If so the error 
        /// information is read from unmanaged parser and a Starcounter exception is
        /// thrown.
        /// </summary>
        /// <param name="scerrorcode">the code returned by the unmanaged parser. 
        /// 0 means no error.</param>
        internal unsafe void RawParserError(int scerrorcode)
        {
            Debug.Assert(IsOpenParserThread, "Raw parser error management requires an open parser.");
            if (scerrorcode > 0)
            {
                // Unmanaged parser returned an error, thus throwing an exception.
                unsafe
                {
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
        internal unsafe String LocationMessageForError(Node* node)
        {
            return LocationMessageForError(node, UnmanagedParserInterface.StrVal(node));
        }

        internal unsafe String LocationMessageForError(Node* node, String token)
        {
            return "Position " + UnmanagedParserInterface.Location(node) + " in the query \"" + Query + "\"" +
                "The error is near or at: " + token;
        }

        // Proper error should be returned from here.
        internal unsafe void UnknownNode(Node* node)
        {
            throw GetSqlException(Error.SCERRSQLNOTIMPLEMENTED, "The statement or clause is not implemented"+LocationMessageForError(node), 
                UnmanagedParserInterface.Location(node), UnmanagedParserInterface.StrVal(node));
        }

        /// <summary>
        /// Has to be called to assert if temporal assumption holds.
        /// If not Debug.Assert is called and an exception is thrown to catch in the parent code and do different condition.
        /// ONLY FOR DEVELOPMENT PURPOSE
        /// </summary>
        /// <param name="condition">The condition to check</param>
        internal void SQLParserAssert(bool condition)
        {
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
        internal void SQLParserAssert(bool condition, string message)
        {
            Debug.Assert(condition, message);
            if (!condition)
                throw new SQLParserAssertException();
        }

        internal static Exception GetSqlException(uint errorCode, string message, int location, string token) {
            List<string> tokens = new List<string>(1);
            tokens.Add(token);
            SqlException ex = new SqlException(message, tokens, location);
            return ErrorCode.ToException(errorCode, "", (m, e) => ex);
        }
    }

    /// <summary>
    /// Exception class used during development to trigger that this parser cannot be used.
    /// </summary>
    internal class SQLParserAssertException : Exception
    {
    }
}
