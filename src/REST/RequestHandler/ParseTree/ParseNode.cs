﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Starcounter.Internal.Uri {

    /// <summary>
    /// The result of analysing sibling verb and URI templates. Used to generate source
    /// code to quickly and efficient find the correct code to execute when
    /// a http call (or other verb+uri call) comes in to the server.
    /// </summary>
    /// <remarks>
    /// 
    /// Example:
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
    /// }
    /// </remarks>
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
        static readonly string[] EmptyParseTypes = new string[0];

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
        internal IEnumerable<string> ParseTypes {
            get {
                if (IsParseNode) {
                    var types = new List<string>();
                    foreach (var kid in Candidates) {
                        switch (kid.Match) {
                            case (byte)'s':
                                types.Add( "string" );
                                break;
                            case (byte)'i':
                                types.Add( "int" );
                                break;
                            default:
                                throw new NotImplementedException("Implement more types");
                        }
                    }
                    return types;
                }
                return EmptyParseTypes;
            }
        }

        /// <summary>
        /// A parse node ('@' node) is followed by a parse type node (i.e. 'i', 's', etc).
        /// For example, in the template "GET /players/{?}" and the handler lambda (string name) => {...},
        /// the prepared template will evaluate to "GET /players/@s". In this example, this node
        /// would be corresponsing to the character 'i'.
        /// </summary>
        internal bool IsParseTypeNode {
            get {
                return (Parent != null && Parent.Match=='@' );
            }
        }

        /// <summary>
        /// A parse node ('@' node) represents variable to be parsed using a specific type.
        /// For example, in the template "GET /players/{?}" and the handler lambda (string name) => {...},
        /// the prepared template will evaluate to "GET /players/@s". In this example, this node
        /// would be corresponsing to the character '@'.
        /// </summary>
        internal bool IsParseNode {
            get {
                return (Match == '@');
            }
        }

        /// <summary>
        /// All the handlers in the top level processor. This is the list used
        /// by the HandlerIndex property.
        /// </summary>
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


        
/*        /// <summary>
        /// The index relative to the first character of the verb and uri template
        /// </summary>
        internal int MatchParseCharInTemplateRelativeToProcessor {
            get {
                if (Parent == null || DetectedType == NodeType.RequestProcessorNode)
                    return 0;
                if (this.IsParseNode || this.IsParseTypeNode)
                    return Parent.MatchParseCharInTemplateRelativeToProcessor;
                return Parent.MatchParseCharInTemplateRelativeToProcessor + MatchCharInTemplateRelative;
            }
        }
        */

        /// <summary>
        /// The index relative to the first character of the verb and uri template
        /// </summary>
        internal int MatchParseCharInTemplateRelativeToSwitch {
            get {
                if (Parent == null || DetectedType == NodeType.CharMatchNode)
                    return MatchCharInTemplateRelative;
                if (this.IsParseNode || this.IsParseTypeNode)
                    return Parent.MatchParseCharInTemplateRelativeToSwitch;
                return Parent.MatchParseCharInTemplateRelativeToSwitch + MatchCharInTemplateRelative;
            }
        }

        //        /// <summary>
//        /// Processors are nested. This function returns the character match position
//        /// of this node relative to the processor. There is a new processor for every
//        /// variable element in the uri template.
//        /// </summary>
//        internal int CharIndexRelativeToProcessor {
//            get {
//                if (this.DetectedType == NodeType.RequestProcessorNode) {
//                    return 0;
//                }
//                return Parent.CharIndexRelativeToProcessor + MatchCharInTemplateRelative;
//            }
//        }

        /// <summary>
        /// The index relative to the first character of the verb and uri template
        /// </summary>
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
        /// <returns></returns>
        private RequestProcessorMetaData GetAnyCandidateHandler() {
            if (HandlerIndex != -1)
                return AllHandlers[HandlerIndex];
            return Candidates[0].GetAnyCandidateHandler();
        }

        /// <summary>
        /// If true, this node (and also all of its siblings) represents a bransch of
        /// differentiating characters at a specific location in a sequence of the template.
        /// </summary>
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