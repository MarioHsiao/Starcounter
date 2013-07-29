// ***********************************************************************
// <copyright file="ParseNode.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.Internal.Application.CodeGeneration {

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

        ///// <summary>
        ///// 
        ///// </summary>
        //private static readonly string[] EmptyParseTypes = new string[0];

        /// <summary>
        /// Gets the template if this is a leaf-node.
        /// </summary>
        /// <value>The template or null if the current node is not a leaf-node.</value>
        internal TemplateMetadata Template {
            get {
                if (TemplateIndex == -1)
                    return null;
                return AllTemplates[TemplateIndex];
            }
        }

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
        /// The index in template name string corresponding to the child candidates
        /// </summary>
        internal int MatchCharInTemplateRelative;

        /// <summary>
        /// Used by the AstTree generator
        /// </summary>
        internal bool TagAsVerified = false;

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

        /// <summary>
        /// This method returns a leaf node. As this function will return
        /// any leaf not, its use is limited to retrieve the name of the template
        /// in the part that is identical in all candidates.
        /// </summary>
        /// <returns>RequestProcessorMetaData.</returns>
        private TemplateMetadata GetAnyCandidateTemplate() {
            if (TemplateIndex != -1)
                return AllTemplates[TemplateIndex];
            return Candidates[0].GetAnyCandidateTemplate();
        }

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
                string templateName;
                string fragment = "";

                if (Parent != null && DetectedType == NodeType.CharMatchNode) {
                    start = Parent.MatchCharInTemplateAbsolute + 1;
                    count = MatchCharInTemplateRelative - 1;

                    if (count > 0) {
                        templateName = GetAnyCandidateTemplate().TemplateName;

                        try {
                            fragment = templateName.Substring(start, count);
                        } catch (Exception) {
                            fragment = "<ERROR>";
                        }
                    }

                    if (Match != 0)
                        fragment += (char)Match;
                } 
                return fragment;
            }
        }

        /// <summary>
        /// Will return a debug string with the tree in JSON. Used for debugging.
        /// </summary>
        /// <returns>The debug string</returns>
        public override string ToString() {
            var sb = new StringBuilder();
            BuildPrettyDebugString(sb, 0);
            return sb.ToString();
        }

        /// <summary>
        /// Used for debug purposes to pretty print a tree. Called by ToString().
        /// </summary>
        /// <param name="fullDebug">if set to <c>true</c> [full debug].</param>
        /// <param name="sb">A string builder to which the tree string is appended</param>
        /// <param name="indentation">The current indentation level</param>
        private void BuildPrettyDebugString(StringBuilder sb, int indentation) {
            sb.Append(' ', indentation);
            sb.Append("{ Match: \'");
            if (Match != 0)
                sb.Append((char)Match);
            sb.Append("'");
            //sb.Append("', DebugMatch: \"");
            //sb.Append(DebugMatchFragment);
            //sb.Append("\"");
            
            if (TemplateIndex != -1 && Candidates.Count == 0) {
                sb.Append(", ");
                sb.Append("Template: \"");
                sb.Append(AllTemplates[TemplateIndex].TemplateName);
                sb.Append('"');
            }

            sb.Append(", ");
            if (TemplateIndex != -1) {
                sb.Append("TemplateIndex: ");
                sb.Append(TemplateIndex);
                sb.Append(", ");
            }
                
            sb.Append("RelativeCharIndex: ");
            sb.Append(MatchCharInTemplateRelative);
            sb.Append(", AbsoluteCharIndex: ");
            sb.Append(MatchCharInTemplateAbsolute);
            
            if (Candidates.Count == 0) {
                sb.Append(' ');
            } else {
                sb.Append(", ");
                sb.AppendLine("Candidates:[ ");
                
                int x = 0;
                foreach (var child in Candidates) {
                    x++;
                    if (x > 1) {
                        sb.AppendLine(",");
                    }
                    child.BuildPrettyDebugString(sb, indentation + 4);
                }
                sb.Append(" ]");
            }
            
            sb.Append("}");
            if (Candidates.Count == 0) {
                if (TemplateIndex == -1)
                    throw new Exception("TODO: something wrong in ParseTreeGenerator.");
            }
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