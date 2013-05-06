// ***********************************************************************
// <copyright file="ParseNode.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.CodeGeneration.Serialization {

    /// <summary>
    /// The result of analysing template names. Used to generate source code to quickly 
    /// and efficient find the correct code to execute for  serialization/deserialization 
    /// of typed json.
    /// </summary>
    internal class ParseNode {
        /// <summary>
        /// The parse tree uses Children and Parent to allow traversing up (Parent) or down (Candidates)
        /// the tree. Each element in the Candidates list will have its parent set to this object.
        /// </summary>
        internal ParseNode Parent;

        /// <summary>
        /// The candidates to match the character in index on. If there is only one
        /// candidate, this candidate is is the leaf node in the tree.
        /// </summary>
        internal List<ParseNode> Candidates = new List<ParseNode>();

        /// <summary>
        /// The UTF8 character (or character fragment) matching this node.
        /// </summary>
        internal byte Match;

        /// <summary>
        /// Categorizes the node in order to make the generation of the abstract syntax tree (AST)
        /// easier.
        /// </summary>
        internal NodeType DetectedType {
            get {
                if (Candidates.Count == 0) {
                    return NodeType.Heureka;
                } else {
                    return NodeType.CharMatchNode;
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private static readonly string[] EmptyParseTypes = new string[0];

        ///// <summary>
        ///// Gets the handler.
        ///// </summary>
        ///// <value>The handler.</value>
        //internal TemplateMetadata Handler {
        //    get {
        //        if (HandlerIndex == -1)
        //            return null;
        //        return AllHandlers[HandlerIndex];
        //    }
        //}

        /// <summary>
        /// All the templates in the top level processor. This is the list used
        /// by the TemplateIndex property.
        /// </summary>
        /// <value>All handlers.</value>
        internal List<TemplateMetadata> AllTemplates { get; set; }

        /// <summary>
        /// A index pointer to an individual template. For non-leaf nodes,
        /// this variable is set to -1 to indicate that there is no single
        /// template.
        /// </summary>
        internal int TemplateIndex = -1;

        /// <summary>
        /// The first character in the segment being matched. For the root level bransches, 
        /// this value is zero. For sub level bransches (those after encountering a URI 
        /// template variable), this points of the first character after the variable.
        /// </summary>
        internal int RootCharOffsetInUriTemplate = 0;

        /// <summary>
        /// The index in the verb+uri template string corresponding to the child candidates
        /// </summary>
        internal int MatchCharInTemplateRelative;

        /// <summary>
        /// The index relative to the first character of the verb and uri template
        /// </summary>
        /// <value>The match char in template absolute.</value>
        internal int MatchCharInTemplateAbsolute {
            get {
                if (Parent != null)
                    return Parent.MatchCharInTemplateAbsolute + MatchCharInTemplateRelative;
                return MatchCharInTemplateRelative;
            }
            set {
                if (Parent == null)
                    MatchCharInTemplateRelative = value;
                else
                    MatchCharInTemplateRelative = value - Parent.MatchCharInTemplateAbsolute;
            }
        }

        ///// <summary>
        ///// This method returns a leaf node. As this function will return
        ///// any leaf not, its use is limited to retrieve the VerbAndUri text
        ///// in the part that is identical in all candidates.
        ///// </summary>
        ///// <returns>RequestProcessorMetaData.</returns>
        //private RequestProcessorMetaData GetAnyCandidateHandler() {
        //    if (HandlerIndex != -1)
        //        return AllHandlers[HandlerIndex];
        //    return Candidates[0].GetAnyCandidateHandler();
        //}

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
                return "TODO: " + Match;

                //int count;
                //int start;
                //string str;

                //if (Parent == null) {
                //    return null;
                //}

                //str = null;
                //if (HandlerIndex == -1) {
                //    start = Parent.MatchCharInTemplateAbsolute + 1;
                //    count = MatchCharInTemplateRelative - 1;

                //    if (Parent.Parent == null) {
                //        start--;
                //        count++;
                //    }

                //    if (count < 1)
                //        return null;

                //    str = GetAnyCandidateHandler().PreparedVerbAndUri;
                //    str = str.Substring(start, count);
                //    if (Match != 0)
                //        str += (char)Match;
                //} else if (Match != 0) {
                //    str = "" + (char)Match;
                //}
                //return str;
            }
        }

        /// <summary>
        /// Will return a debug string with the tree in JSON. Used for debugging.
        /// </summary>
        /// <returns>The debug string</returns>
        public override string ToString() {
            return "TODO!";
            //var sb = new StringBuilder();
            //var comment = BuildPrettyPrintDebugString(false, sb, 0);
            //if (comment != null) {
            //    sb.Append(comment);
            //}
            //return sb.ToString();
        }

        /// <summary>
        /// Creates a pretty print DOM tree in the form of
        /// a JSON tree
        /// </summary>
        /// <param name="fullDebug">Set to true to print the used data, false to only show the debug data</param>
        /// <returns>A multiline string</returns>
        public string ToString(bool fullDebug) {
            return "TODO!";
            //var sb = new StringBuilder();
            //var comment = BuildPrettyPrintDebugString(fullDebug, sb, 0);
            //if (comment != null) {
            //    sb.Append(comment);
            //}
            //return sb.ToString();
        }
        /// <summary>
        /// Used for debug purposes to pretty print a tree. Called by ToString().
        /// </summary>
        /// <param name="fullDebug">if set to <c>true</c> [full debug].</param>
        /// <param name="sb">A string builder to which the tree string is appended</param>
        /// <param name="indentation">The current indentation level</param>
        /// <returns>System.String.</returns>
        private string BuildPrettyPrintDebugString(bool fullDebug, StringBuilder sb, int indentation) {
            return "TODO!";
            //sb.Append(' ', indentation);
            ////          sb.AppendLine("{");
            //sb.Append("{ ");
            //string comment = null;
            //var par = false;
            //var nt = DetectedType;
            ////            if (nt == NodeType.CharMatchNode) {
            //if (fullDebug) {
            //    par = true;
            //    sb.Append("Match:\'");
            //    if (Match != 0)
            //        sb.Append(Encoding.UTF8.GetString(new byte[] { Match }));
            //    sb.Append("', ");
            //    sb.Append("DebugMatch:\"");
            //    sb.Append(DebugMatchFragment);
            //    sb.Append("\"");
            //} else {
            //    //                    sb.Append("Case:'");
            //    //                    if (Match != 0)
            //    //                        sb.Append((char)Match);
            //    //                    sb.Append("'");
            //    if (Match != 0) {
            //        if (par)
            //            sb.Append(", ");
            //        par = true;
            //        sb.Append("Match:\"");
            //        sb.Append(DebugMatchFragment);
            //        sb.Append('"');
            //    }
            //}
            ////            }
            ////            else {
            ////                par = true;
            ////                sb.Append("Type:\"");
            ////                sb.Append(DebugTypeString);
            ////                sb.Append("\"");
            ////            }
            //if (HandlerIndex != -1 && Candidates.Count == 0) {
            //    if (par)
            //        sb.Append(", ");
            //    sb.Append("Handler:\"");
            //    sb.Append(AllHandlers[HandlerIndex].PreparedVerbAndUri);
            //    sb.Append('"');
            //    par = true;
            //}
            //if (IsParseTypeNode) {
            //    if (par)
            //        sb.Append(", ");
            //    sb.Append("Parse:'" + (char)Match + "'");
            //    par = true;
            //}

            //if (fullDebug) {
            //    if (par)
            //        sb.Append(", ");
            //    par = true;
            //    if (HandlerIndex != -1) {
            //        sb.Append("HandlerIndex:");
            //        sb.Append(HandlerIndex);
            //    }
            //    if (par)
            //        sb.Append(", ");
            //    par = true;
            //    sb.Append("RelativeCharIndex:");
            //    sb.Append(MatchCharInTemplateRelative);
            //    sb.Append(", AbsoluteCharIndex:");
            //    sb.Append(MatchCharInTemplateAbsolute);
            //}
            //if (par)
            //    sb.Append(", ");
            //par = true;
            //sb.Append("Detected:\"");
            //sb.Append(DebugDetected);
            //sb.Append('"');
            //if (Candidates.Count == 0) {
            //    sb.Append(' ');
            //} else {
            //    //                if (par && !IsParseParent) {
            //    //                    sb.Append(", ");
            //    //                    sb.Append("Switch(" + MatchCharInTemplateRelative + ")");
            //    //                }
            //    if (par)
            //        sb.Append(", ");
            //    sb.Append("Candidates:");
            //    //                sb.Append(' ', indentation);
            //    sb.AppendLine("[");
            //    int x = 0;
            //    foreach (var child in Candidates) {
            //        x++;
            //        if (x > 1) {
            //            sb.Append(",");
            //            //                        if (comment != null) {
            //            //                            sb.Append(comment);
            //            //                        }
            //            sb.AppendLine("");
            //        }
            //        comment = child.BuildPrettyPrintDebugString(fullDebug, sb, indentation + 4);
            //    }
            //    sb.Append("]");
            //}
            //indentation -= 4;
            //sb.Append("}");
            //if (Candidates.Count == 0) {
            //    if (HandlerIndex == -1)
            //        return " // ERROR";
            //    return " // " + AllHandlers[HandlerIndex].PreparedVerbAndUri;
            //}
            //return comment;
        }
    }

    /// <summary>
    /// Each type can be categorized by calling the NodeType property.
    /// </summary>
    internal enum NodeType {
        /// <summary>
        /// This node is a bransh of a differing character.
        /// </summary>
        CharMatchNode,

        /// <summary>
        /// This node represents a single handler and no further processing is required
        /// </summary>
        Heureka
    }
}