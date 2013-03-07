// ***********************************************************************
// <copyright file="ParseNode.PrettyPrint.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
namespace Starcounter.Internal.Application.CodeGeneration.Serialization {

    /// <summary>
    /// The result of analysing sibling verb and URI templates. Used to generate source
    /// code to quickly and efficient find the correct code to execute when
    /// a http call (or other verb+uri call) comes in to the server.
    /// </summary>
    /// <remarks>Example:
    /// There are three handlers that looks like follows:
    /// 
    /// "GET /mydemo/foo"
    ///              ^
    ///              12
    /// "GET /mydemo/barx"
    ///              ^  ^
    ///              12 15
    /// "GET /mydemo/bary"
    ///              ^  ^
    ///              12 15
    ///              
    /// In the example above, the ^ character shows the fork character positions
    /// 
    /// { Index:12, // All handlers have the same verb and uri from indicies 0 through 11
    ///   Candidates:[
    ///         { Match:'f'}, // The 'f' and 'b' characters differentiate handlers 1 and 2/3
    ///         { Match:'b', Index:15, Candidates:[
    ///                 { Match:'x'}, // The 'x' and 'y' character differentiate handlers 2 and 3
    ///                 { Match:'y'}
    ///             ]}
    ///         ]
    /// }</remarks>
    public partial class ParseNode {


        /// <summary>
        /// Extracts the part of the URI template (and leading verb) that
        /// are being matched in this node. Actually, it is only the last character
        /// that is being matched as the other characters much be identical for all
        /// child candidates of this node. However, for easy of debugging, we show
        /// the complete fragment string in this property.
        /// </summary>
        /// <value>The debug match fragment.</value>
        /// <remarks>Used to pretty print a trailing string rather than the individual
        /// character used for matching this node in the parent nodes candidate
        /// list. Does not affect the procesing logic.</remarks>
        internal string DebugMatchFragment {
            get {
                int count;
                int start;
                string str;

                if (Parent == null) {
                    return null;
                }
          
                str = null;
                if (HandlerIndex == -1) {
                    start = Parent.MatchCharInTemplateAbsolute + 1;
                    count = MatchCharInTemplateRelative - 1;

                    if (Parent.Parent == null) {
                        start--;
                        count++;
                    }

                    if (count < 1)
                        return null;

                    str = GetAnyCandidateHandler().PreparedVerbAndUri;
                    str = str.Substring(start, count);
                    if (Match != 0)
                        str += (char)Match;
                } else if (Match != 0) {
                    str = "" + (char)Match;
                }
                return str;
            }
        }

        /// <summary>
        /// Will return a debug string with the tree in JSON. Used for debugging.
        /// </summary>
        /// <returns>The debug string</returns>
        public override string ToString() {
            var sb = new StringBuilder();
            var comment = BuildPrettyPrintDebugString(false, sb, 0);
            if (comment != null) {
                sb.Append(comment);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Creates a pretty print DOM tree in the form of
        /// a JSON tree
        /// </summary>
        /// <param name="fullDebug">Set to true to print the used data, false to only show the debug data</param>
        /// <returns>A multiline string</returns>
        public string ToString(bool fullDebug) {
            var sb = new StringBuilder();
            var comment = BuildPrettyPrintDebugString(fullDebug, sb, 0);
            if (comment != null) {
                sb.Append(comment);
            }
            return sb.ToString();
        }
        /// <summary>
        /// Used for debug purposes to pretty print a tree. Called by ToString().
        /// </summary>
        /// <param name="fullDebug">if set to <c>true</c> [full debug].</param>
        /// <param name="sb">A string builder to which the tree string is appended</param>
        /// <param name="indentation">The current indentation level</param>
        /// <returns>System.String.</returns>
        private string BuildPrettyPrintDebugString(bool fullDebug, StringBuilder sb, int indentation) {
            sb.Append(' ', indentation);
            //          sb.AppendLine("{");
            sb.Append("{ ");
            string comment = null;
            var par = false;
            var nt = DetectedType;
            //            if (nt == NodeType.CharMatchNode) {
            if (fullDebug) {
                par = true;
                sb.Append("Match:\'");
                if (Match != 0) 
                    sb.Append(Encoding.UTF8.GetString(new byte[] { Match }));
                sb.Append("', ");
                sb.Append("DebugMatch:\"");
                sb.Append(DebugMatchFragment);
                sb.Append("\"");
            }
            else {
                //                    sb.Append("Case:'");
                //                    if (Match != 0)
                //                        sb.Append((char)Match);
                //                    sb.Append("'");
                if (Match != 0) {
                    if (par)
                        sb.Append(", ");
                    par = true;
                    sb.Append("Match:\"");
                    sb.Append(DebugMatchFragment);
                    sb.Append('"');
                }
            }
            //            }
            //            else {
            //                par = true;
            //                sb.Append("Type:\"");
            //                sb.Append(DebugTypeString);
            //                sb.Append("\"");
            //            }
            if (HandlerIndex != -1 && Candidates.Count == 0) {
                if (par)
                    sb.Append(", ");
                sb.Append("Handler:\"");
                sb.Append(AllHandlers[HandlerIndex].PreparedVerbAndUri);
                sb.Append('"');
                par = true;
            }
            if (IsParseTypeNode) {
                if (par)
                    sb.Append(", ");
                sb.Append("Parse:'" + (char)Match + "'");
                par = true;
            }

            if (fullDebug) {
                if (par)
                    sb.Append(", ");
                par = true;
                if (HandlerIndex != -1) {
                    sb.Append("HandlerIndex:");
                    sb.Append(HandlerIndex);
                }
                if (par)
                    sb.Append(", ");
                par = true;
                sb.Append("RelativeCharIndex:");
                sb.Append(MatchCharInTemplateRelative);
                sb.Append(", AbsoluteCharIndex:");
                sb.Append(MatchCharInTemplateAbsolute);
            }
            if (par)
                sb.Append(", ");
            par = true;
            sb.Append("Detected:\"");
            sb.Append(DebugDetected);
            sb.Append('"');
            if (Candidates.Count == 0) {
                sb.Append(' ');
            }
            else {
                //                if (par && !IsParseParent) {
                //                    sb.Append(", ");
                //                    sb.Append("Switch(" + MatchCharInTemplateRelative + ")");
                //                }
                if (par)
                    sb.Append(", ");
                sb.Append("Candidates:");
                //                sb.Append(' ', indentation);
                sb.AppendLine("[");
                int x = 0;
                foreach (var child in Candidates) {
                    x++;
                    if (x > 1) {
                        sb.Append(",");
                        //                        if (comment != null) {
                        //                            sb.Append(comment);
                        //                        }
                        sb.AppendLine("");
                    }
                    comment = child.BuildPrettyPrintDebugString(fullDebug, sb, indentation + 4);
                }
                sb.Append("]");
            }
            indentation -= 4;
            sb.Append("}");
            if (Candidates.Count == 0) {
                if (HandlerIndex == -1)
                    return " // ERROR";
                return " // " + AllHandlers[HandlerIndex].PreparedVerbAndUri;
            }
            return comment;
        }


        /// <summary>
        /// Gets the debug detected.
        /// </summary>
        /// <value>The debug detected.</value>
        /// <exception cref="System.Exception"></exception>
        internal string DebugDetected {
            get {
                switch (DetectedType) {
                    case NodeType.TestAllCandidatesNode:
                        return "TestAll";
                    case NodeType.CharMatchNode:
                        return "Case";
                    case NodeType.Heureka:
                        return "Eureka";
                    case NodeType.RequestProcessorNode:
                        return "RP";
                    default:
                        throw new Exception();
                }
            }
        }
    }
}