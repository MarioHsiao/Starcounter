// ***********************************************************************
// <copyright file="AstTreeGenerator.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// Class AstTreeGenerator
    /// </summary>
    internal static class AstTreeGenerator {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="puppletTemplate"></param>
        /// <returns></returns>
        internal static AstNamespace BuildAstTree(TObj objTemplate) {
            ParseNode parseTree = ParseTreeGenerator.BuildParseTree(objTemplate);

            string ns = objTemplate.Namespace;
            if (String.IsNullOrEmpty(ns)) {
                ns = "__starcountergenerated__";
            }

            return BuildAstTree(parseTree, ns, objTemplate.ClassName + "Serializer");
        }

        /// <summary>
        /// Creates the json serializer class.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>An AstRequestProcessorClass node.</returns>
        private static AstNamespace BuildAstTree(ParseNode input, string ns, string className) {
            AstNamespace astNs = new AstNamespace() {
                Namespace = ns
            };

            AstJsonSerializerClass jsClass = new AstJsonSerializerClass() {
                Parent = astNs,
                ParseNode = input,
                ClassName = className
            };
            
            var cons = new AstJSConstructor() {
                Parent = jsClass,
                ParseNode = input
            };
           
//            CreateSerializerFunction(input, jsClass);
            CreateDeserializerFunction(input, jsClass);

            return astNs;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="jsClass"></param>
        private static AstSerializeFunction CreateSerializerFunction(ParseNode input, AstJsonSerializerClass jsClass) {
            Template template;

            var sf = new AstSerializeFunction() {
                Parent = jsClass
            };
            jsClass.SerializeFunction = sf;
            
            var astUnsafe = new AstUnsafe() {
                ParseNode = input,
                Parent = sf
            };

            var astObj = new AstJsonObject() {
                Parent = astUnsafe
            };
            
            for (int i = 0; i < input.AllTemplates.Count; i++) {
                template = input.AllTemplates[i].Template;
                
                new AstJsonProperty() {
                    Template = template,
                    Parent = astObj
                };

                new AstJsonDelimiter() {
                    Delimiter = ':',
                    Parent = astObj
                };

                new AstJsonPropertyValue() {
                    Template = template,
                    Parent = astObj
                };
                
                if ((i + 1) < input.AllTemplates.Count) {
                    new AstJsonDelimiter() {
                        Delimiter = ',',
                        Parent = astObj
                    };
                }
            }

            new AstCodeStatement() {
                Statement = "return (bufferSize - leftBufferSize);",
                Parent = astUnsafe
            };

            return sf;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="jsClass"></param>
        private static void CreateDeserializerFunction(ParseNode input, AstJsonSerializerClass jsClass) {
            AstNode nextParent;

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
                CreateVerificationNode(nextParent, input.Candidates[0]);

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
                    ExceptionCode = CreateExceptionMessage(input),
                    Parent = dc
                };
            }

            if (input.TemplateIndex == -1) {
                var fnFail = new AstProcessFail() {
                    Parent = df
                };
            }
        }

        /// <summary>
        /// Creates the code node.
        /// </summary>
        /// <param name="pn">The pn.</param>
        /// <param name="parent">The parent.</param>
        private static void CreateCodeNode(ParseNode pn, AstNode parent) {
            AstNode nextParent;

            switch (pn.DetectedType) {
                case NodeType.CharMatchNode:
                    var cn = new AstCase();
                    cn.Parent = parent;
                    cn.ParseNode = pn;
                    nextParent = cn;

                    if (pn.Candidates.Count > 1) {
                        CreateVerificationNode(cn, pn.Candidates[0]);

                        var nsn = new AstSwitch() {
                            Parent = cn,
                            ParseNode = pn.Candidates[0]
                        };
                        nextParent = nsn;
                    } else {
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
                            ExceptionCode = CreateExceptionMessage(pn),
                            Parent = dc
                        };
                    }

                    break;
                case NodeType.Heureka:
                    nextParent = parent;
                    CreateVerificationNode(nextParent, pn);
                    
                    new AstGotoValue() {
                        Parent = nextParent
                    };

                    bool addGotoValue = false;
                    if (pn.Template.Template is TObjArr) {
                        // If the value to parse is a list we need to add some additional 
                        // code for looping and checking end of array.
                        nextParent = new AstParseJsonObjectArray() {
                            Parent = nextParent
                        };

                        nextParent = new AstWhile() {
                            Parent = nextParent,
                            Indentation = 8
                        };
                        addGotoValue = true;
                    }

                    var pj = new AstParseJsonValue() {
                        ParseNode = pn,
                        Parent = nextParent
                    };

                    if (addGotoValue) {
                        new AstGotoValue() {
                            Parent = pj,
                            IsValueArrayObject = true
                        };
                    }

                    break;
                default:
                    new AstError() {
                        Parent = parent,
                        ParseNode = pn
                    };
                    break;
            }
        }

        private static void CreateVerificationNode(AstNode parent, ParseNode node) {
            if (!node.TagAsVerified) {
                int vlen = node.MatchCharInTemplateAbsolute;
                if (vlen > 0) {
                    new AstVerifier() {
                        Parent = parent,
                        ParseNode = node
                    };
                }
                node.TagAsVerified = true;
            }
        }

        private static string CreateExceptionMessage(ParseNode node) {
            return "ErrorCode.ToException(Starcounter.Internal.Error.SCERRUNSPECIFIED, \"char: '\" + (char)*pBuffer + \"', offset: \" + (bufferSize - leftBufferSize) + \"\");";
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