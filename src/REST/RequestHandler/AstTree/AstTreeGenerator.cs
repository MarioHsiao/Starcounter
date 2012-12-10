// ***********************************************************************
// <copyright file="AstTreeGenerator.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;

namespace Starcounter.Internal.Uri {

    /// <summary>
    /// Class AstTreeGenerator
    /// </summary>
    internal static class AstTreeGenerator {

        /// <summary>
        /// Builds the ast tree.
        /// </summary>
        /// <param name="parseTree">The parse tree.</param>
        /// <returns>AstNamespace.</returns>
        internal static AstNamespace BuildAstTree(ParseNode parseTree) {
            var ns = new AstNamespace();
            CreateRpNode(parseTree, ns);
            return ns;
        }

        /// <summary>
        /// Creates the rp node.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="parent">The parent.</param>
        /// <returns>AstRequestProcessorClass.</returns>
        internal static AstRequestProcessorClass CreateRpNode(ParseNode input, AstNode parent) {
            AstRequestProcessorClass rpclass;
            rpclass = new AstRequestProcessorClass() {
                Parent = parent
            };
            if (parent is AstRequestProcessorClass) {
                rpclass.AllocateSubClassNo();
            }
            rpclass.ParseNode = input;
            if (parent != null)
                rpclass.Parent = parent;

            if (rpclass.IsSingleHandler) {
                input.Handler.AstClass = rpclass;
            }

            if (rpclass.IsTopLevel) {
                var cons = new AstRpConstructor() {
                    Parent = rpclass,
                    ParseNode = input
                };
            } else {
                var pver = new AstVerifyFunction() {
                    Parent = rpclass,
                    VerificationName = rpclass.PropertyName,
                    Handler = input.Handler
                };
            }

            var fn = new AstProcessFunction() {
                Parent = rpclass
            };

            AstNode nextParent = fn;
            if (input.IsParseTypeNode) {
                var verify = new AstVerifier() {
                    Parent = nextParent
                };
                nextParent = verify;

                var parse = new AstParseCode() {
                    Parent = nextParent,
                    ParseNode = input
                };
                nextParent = parse;
            }
            else if (input.HandlerIndex != -1) {
                var verify = new AstVerifier() {
                    Parent = nextParent
                };
                var invoke = new AstInvoke() {
                    Parent = verify,
                    ParseNode = input
                };
                var fnFail = new AstProcessFail() {
                    Parent = fn
                };
            }

            if (input.Candidates.Count > 0 && input.Candidates[0].DetectedType == NodeType.CharMatchNode) {
                var pin = new AstUnsafe() {
                    Parent = nextParent
                };
                nextParent = new AstSwitch() {
                    ParseNode = input,
                    Parent = pin
                };
            }

            foreach (var cand in input.Candidates) {
                CreateCodeNode(cand, nextParent);
            }

            if (input.HandlerIndex == -1 || input.IsParseTypeNode) {
                var fnFail = new AstProcessFail() {
                    Parent = fn
                };
            }

            return rpclass;
        }

        /// <summary>
        /// Creates the code node.
        /// </summary>
        /// <param name="pn">The pn.</param>
        /// <param name="parent">The parent.</param>
        internal static void CreateCodeNode(ParseNode pn, AstNode parent) {
            switch (pn.DetectedType) {
                case NodeType.CharMatchNode:
                    var cn = new AstCase();
                    cn.Parent = parent;
                    cn.ParseNode = pn;
                    AstNode nextParent;
                    if (pn.Candidates.Count > 1) {
                        var nsn = new AstSwitch() {
                            Parent = cn,
                            ParseNode = pn
                        };
                        nextParent = nsn;
                    }
                    else {
                        nextParent = cn;
                    }
                    foreach (var cand in pn.Candidates) {
                        CreateCodeNode(cand, nextParent);
                    }
                    break;
                case NodeType.Heureka: {
                        if (parent is AstCase) {
                            CreateCallProcessorAndSubRpClass(pn, parent);
                        }
                        else {
                            var ret = new AstInvoke() {
                                Parent = parent,
                                ParseNode = pn
                            };
                        }
                    }
                    break;
                case NodeType.TestAllCandidatesNode: {
                        var elif = new AstElseIfList() {
                            Parent = parent,
                            ParseNode = pn
                        };
                        foreach (var kid in pn.Candidates) {
                            CreateCodeNode(kid, elif);
                        }
                        //                        var returnFalse = new AstCodeStatement() {
                        //                            Parent = parent,
                        //                            Statement = "break;"
                        //                        };
                    }
                    break;
                case NodeType.RequestProcessorNode:
                    CreateCallProcessorAndSubRpClass(pn, parent);
                    break;
                default:
                    new AstError() {
                        Parent = parent,
                        ParseNode = pn
                    };
                    break;
            }
        }

        /// <summary>
        /// Creates the call processor and sub rp class.
        /// </summary>
        /// <param name="pn">The pn.</param>
        /// <param name="parent">The parent.</param>
        internal static void CreateCallProcessorAndSubRpClass( ParseNode pn, AstNode parent ) {
            var rpClass = CreateRpNode(pn, parent.TopClass);
            var call = new AstIfCallSub() {
                Parent = parent,
                RpClass = rpClass,
                ParseNode = pn
//                            Statement = "(Sub.Process(...))"
            };
            var elseNode = new AstCodeStatement() {
                Parent = call,
                Statement = "return true;"
            };
        }
    }

    /// <summary>
    /// Class AstError
    /// </summary>
    internal class AstError : AstNode {
        /// <summary>
        /// Gets or sets the parse node.
        /// </summary>
        /// <value>The parse node.</value>
        internal ParseNode ParseNode { get; set; }
        /// <summary>
        /// Gets the debug string.
        /// </summary>
        /// <value>The debug string.</value>
        internal override string DebugString {
            get {
                return "Error " + ParseNode.DetectedType;
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            var sb = new StringBuilder();
            sb.Append(DebugString);
            Prefix.Add(sb.ToString());
        }

    }
}