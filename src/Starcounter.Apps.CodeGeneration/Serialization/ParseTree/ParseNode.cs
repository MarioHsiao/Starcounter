// ***********************************************************************
// <copyright file="ParseNode.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <value>The type of the detected.</value>
        /// <exception cref="System.Exception"></exception>
        internal NodeType DetectedType {
            get {
//                return NodeType.CharMatchNode;
                if (IsParseNode)
                    return NodeType.TestAllCandidatesNode;
                else if (IsParseTypeNode)
                    return NodeType.RequestProcessorNode;
                else if (IsCharMatchNode)
                    return NodeType.CharMatchNode;
                else if (Candidates.Count == 0)
                    return NodeType.Heureka;
                else
                    return NodeType.RequestProcessorNode;
                throw new Exception();
            }
        }
        /// <summary>
        /// The empty parse types
        /// </summary>
        static readonly string[] EmptyParseTypes = new string[0];

        /// <summary>
        /// Gets the handler.
        /// </summary>
        /// <value>The handler.</value>
        internal RequestProcessorMetaData Handler {
            get {
                if (HandlerIndex == -1)
                    return null;
                return AllHandlers[HandlerIndex];
            }
        }

        /// <summary>
        /// If this node is a  '@' node (i.e. a parent parse node), this method will return
        /// a list of types that should be parsed.
        /// </summary>
        /// <value>The parse types.</value>
        internal IEnumerable<string> ParseTypes {
            get {
                if (IsParseNode) {
                    var types = new List<string>();
                    foreach (var kid in Candidates) {
                        types.Add(kid.ParseTypeName);
                    }
                    return types;
                }
                return EmptyParseTypes;
            }
        }

        /// <summary>
        /// If this node is a parse type node it will returns the name
        /// of the type to parse, null otherwise
        /// </summary>
        /// <exception cref="System.NotImplementedException">Implement more types</exception>
        internal string ParseTypeName {
            get {
                string ret = null;
                if (IsParseTypeNode) {
                    switch(Match){
                        // TODO: Use constants instead of literals.
                        case (byte)'s':
                            ret = "string";
                            break;
                        case (byte)'i':
                            ret = "int";
                            break;
                        case (byte)'l':
                            ret = "long";
                            break;
                        case (byte)'m':
                            ret = "decimal";
                            break;
                        case (byte)'d':
                            ret = "double";
                            break;
                        case (byte)'b':
                            ret = "bool";
                            break;
                        case (byte)'t':
                            ret = "DateTime";
                            break;
                        default:
                            throw new NotImplementedException("Implement more types");
                    }
                }
                return ret;
            }
        }

        /// <summary>
        /// A parse node ('@' node) is followed by a parse type node (i.e. 'i', 's', etc).
        /// For example, in the template "GET /players/{?}" and the handler lambda (string name) =&gt; {...},
        /// the prepared template will evaluate to "GET /players/@s". In this example, this node
        /// would be corresponsing to the character 'i'.
        /// </summary>
        /// <value><c>true</c> if this instance is parse type node; otherwise, <c>false</c>.</value>
        internal bool IsParseTypeNode {
            get {
                return (Parent != null && Parent.Match=='@' );
            }
        }

        /// <summary>
        /// A parse node ('@' node) represents variable to be parsed using a specific type.
        /// For example, in the template "GET /players/{?}" and the handler lambda (string name) =&gt; {...},
        /// the prepared template will evaluate to "GET /players/@s". In this example, this node
        /// would be corresponsing to the character '@'.
        /// </summary>
        /// <value><c>true</c> if this instance is parse node; otherwise, <c>false</c>.</value>
        internal bool IsParseNode {
            get {
                return (Match == '@');
            }
        }

        /// <summary>
        /// All the handlers in the top level processor. This is the list used
        /// by the HandlerIndex property.
        /// </summary>
        /// <value>All handlers.</value>
        internal List<RequestProcessorMetaData> AllHandlers { get; set; }

        /// <summary>
        /// A index pointer to an individual handler. For non-leaf nodes,
        /// this variable is set to -1 to indicate that there is no single
        /// handler.
        /// </summary>
        internal int HandlerIndex = -1;

        /// <summary>
        /// The first character in the segment being matched. For the root
        /// level bransches, this value is zero. For sub level bransches
        /// (those after encountering a URI template variable), this points
        /// of the first character after the variable.
        /// </summary>
        internal int RootCharOffsetInUriTemplate = 0;

        /// <summary>
        /// The index in the verb+uri template string corresponding to the
        /// child candidates
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

        /// <summary>
        /// This method returns a leaf node. As this function will return
        /// any leaf not, its use is limited to retrieve the VerbAndUri text
        /// in the part that is identical in all candidates.
        /// </summary>
        /// <returns>RequestProcessorMetaData.</returns>
        private RequestProcessorMetaData GetAnyCandidateHandler() {
            if (HandlerIndex != -1)
                return AllHandlers[HandlerIndex];
            return Candidates[0].GetAnyCandidateHandler();
        }

        /// <summary>
        /// If true, this node (and also all of its siblings) represents a bransch of
        /// differentiating characters at a specific location in a sequence of the template.
        /// </summary>
        /// <value><c>true</c> if this instance is char match node; otherwise, <c>false</c>.</value>
        internal bool IsCharMatchNode {
            get {
                return (Match != 0 && !IsParseNode && !Parent.IsParseNode);
            }
        }
    }

    /// <summary>
    /// Each type can be categorized by calling the NodeType property.
    /// </summary>
    public enum NodeType {
        /// <summary>
        /// This node is a bransh of a differing character.
        /// </summary>
        CharMatchNode,

        /// <summary>
        /// This node represents a parent of a set of possible parsing results.
        /// </summary>
        TestAllCandidatesNode,

        /// <summary>
        /// This node represents a fragment following a parsing result or this node is the root
        /// processor node
        /// </summary>
        RequestProcessorNode,

        /// <summary>
        /// This node represents a single handler and no further processing is required
        /// </summary>
        Heureka
    }
}