// ***********************************************************************
// <copyright file="AstTreeGenerator.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Internal.Uri;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {

    /// <summary>
    /// Class AstTreeGenerator
    /// </summary>
    internal static class AstTreeGenerator {

        /// <summary>
        /// Builds the ast tree.
        /// </summary>
        /// <param name="parseTree">The parse tree.</param>
        /// <returns>AstNamespace.</returns>
        internal static AstJsonSerializerClass BuildAstTree(ParseNode parseTree) {
            return CreateJsonSerializer(parseTree);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appTemplate"></param>
        /// <returns></returns>
        internal static AstJsonSerializerClass BuildAstTree(AppTemplate appTemplate) {
            ParseNode parseTree = ParseTreeGenerator.BuildParseTree(RegisterTemplatesForApp(appTemplate));
            return CreateJsonSerializer(parseTree);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appTemplate"></param>
        private static List<RequestProcessorMetaData> RegisterTemplatesForApp(AppTemplate appTemplate) {
            List<RequestProcessorMetaData> handlers = new List<RequestProcessorMetaData>();
            foreach (Template child in appTemplate.Children) {
                if (child is ActionProperty)
                    continue;

                RequestProcessorMetaData rp = new RequestProcessorMetaData();
                rp.UnpreparedVerbAndUri = child.Name;
                rp.Code = child;
                handlers.Add(rp);
            }
            return handlers;
        }

        /// <summary>
        /// Creates the json serializer class.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>An AstRequestProcessorClass node.</returns>
        internal static AstJsonSerializerClass CreateJsonSerializer(ParseNode input) {
            AstJsonSerializerClass jsClass;
            AstNode nextParent;

            jsClass = new AstJsonSerializerClass();
            jsClass.ParseNode = input;
            
            var cons = new AstJSConstructor() {
                Parent = jsClass,
                ParseNode = input
            };

            var sf = new AstSerializeFunction() {
                Parent = jsClass
            };
            jsClass.SerializeFunction = sf;

            new AstCodeStatement() {
                Statement = "return false;",
                Parent = sf
            };

            // TODO:
            // Create the serialize functionality

            var df = new AstDeserializeFunction() {
                Parent = jsClass
            };
            jsClass.DeserializeFunction = df;

            nextParent = new AstUnsafe() {
                ParseNode = input,
                Parent = df
            };

            nextParent = new AstWhile() {
                Parent = nextParent
            };

            new AstGotoProperty() {
                Parent = nextParent
            };

            if (input.Candidates.Count > 0 && input.Candidates[0].DetectedType == NodeType.CharMatchNode) {
                nextParent = new AstSwitch() {
                    ParseNode = input.Candidates[0],
                    Parent = nextParent
                };
            }

            foreach (var cand in input.Candidates) {
                CreateCodeNode(cand, nextParent);
            }

            if (nextParent is AstSwitch) {
                // Add a default case for unknown properties.
                var dc = new AstCase() {
                    IsDefault = true,
                    ParseNode = null,
                    Parent = nextParent
                };
                var fnFail = new AstProcessFail() {
                    Message = "Property not belonging to this app found in content.",
                    Parent = dc
                };
            }

            if (input.HandlerIndex == -1 || input.IsParseTypeNode) {
                var fnFail = new AstProcessFail() {
                    Parent = df
                };
            }

            return jsClass;
        }

        /// <summary>
        /// Creates the code node.
        /// </summary>
        /// <param name="pn">The pn.</param>
        /// <param name="parent">The parent.</param>
        internal static void CreateCodeNode(ParseNode pn, AstNode parent) {
            AstNode nextParent;

            switch (pn.DetectedType) {
                case NodeType.CharMatchNode:
                    var cn = new AstCase();
                    cn.Parent = parent;
                    cn.ParseNode = pn;        
                    if (pn.Candidates.Count > 1) {
                        var nsn = new AstSwitch() {
                            Parent = cn,
                            ParseNode = pn.Candidates[0]
                        };
                        nextParent = nsn;
                    }
                    else {
                        nextParent = cn;
                    }
                    foreach (var cand in pn.Candidates) {
                        CreateCodeNode(cand, nextParent);
                    }

                    if (nextParent is AstSwitch) {
                        // Add a default case for unknown properties.
                        var dc = new AstCase() {
                            IsDefault = true,
                            ParseNode = null,
                            Parent = nextParent
                        };
                        var fnFail = new AstProcessFail() {
                            Message = "Property not belonging to this app found in content.",
                            Parent = dc
                        };
                    }

                    break;
                case NodeType.Heureka:
                    nextParent = new AstElseIfList(){
                        ParseNode = pn,
                        Parent = parent
                    };

                    new AstGotoValue() {
                        Parent = nextParent
                    };

                    bool addGotoValue = false;
                    if (pn.Handler.Code is ListingProperty) {
                        // If the value to parse is a list we need to add some additional 
                        // code for looping and checking end of array.
                        nextParent = new AstWhile() {
                            Parent = nextParent
                        };
                        addGotoValue = true;
                    } 

                    var pj = new AstParseJsonValue() {
                        ParseNode = pn,
                        Parent = nextParent
                    };
                    new AstSetValue() {
                        ParseNode = pn,
                        Parent = pj
                    };
                    new AstValueJump() {
                        ParseNode = pn,
                        Parent = pj
                    };

                    if (addGotoValue) {
                        new AstGotoValue() {
                            Parent = pj,
                            IsValueArrayObject = true
                        };
                    }

                    break;
                case NodeType.TestAllCandidatesNode: 
                    if (parent is AstSwitch) {
                        // If we are inside a switch-stmt, we make a default case label
                        // without any jump instead of an elseif (with jump)
                        parent = new AstCase() {
                            Parent = parent,
                            IsDefault = true
                        };
                    } else {
                        parent = new AstElseIfList() {
                            Parent = parent,
                            ParseNode = pn
                        };
                    }

                    foreach (var kid in pn.Candidates) {
                        CreateCodeNode(kid, parent);
                    }
                    break;
                case NodeType.RequestProcessorNode:
                    throw new NotImplementedException("TODO! RequestProcessorNode");
                    //CreateCallProcessorAndSubRpClass(pn, parent);
                    //break;
                default:
                    new AstError() {
                        Parent = parent,
                        ParseNode = pn
                    };
                    break;
            }
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