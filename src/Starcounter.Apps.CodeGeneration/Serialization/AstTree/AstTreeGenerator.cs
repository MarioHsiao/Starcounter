﻿// ***********************************************************************
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
        /// 
        /// </summary>
        /// <param name="appTemplate"></param>
        /// <returns></returns>
        internal static AstJsonSerializerClass BuildAstTree(TApp appTemplate) {
            ParseNode parseTree = ParseTreeGenerator.BuildParseTree(RegisterTemplatesForApp(appTemplate));
            return CreateJsonSerializer(parseTree);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appTemplate"></param>
        private static List<RequestProcessorMetaData> RegisterTemplatesForApp(TApp appTemplate) {
            List<RequestProcessorMetaData> handlers = new List<RequestProcessorMetaData>();
            foreach (Template child in appTemplate.Children) {
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
            AstJsonSerializerClass jsClass = new AstJsonSerializerClass() {
                ParseNode = input
            };
            
            var cons = new AstJSConstructor() {
                Parent = jsClass,
                ParseNode = input
            };

            CreateSerializerFunction(input, jsClass);
            CreateDeserializerFunction(input, jsClass);

            return jsClass;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="jsClass"></param>
        private static void CreateSerializerFunction(ParseNode input, AstJsonSerializerClass jsClass) {
            Template template;

            var sf = new AstSerializeFunction() {
                Parent = jsClass
            };
            jsClass.SerializeFunction = sf;

            var astu = new AstUnsafe() {
                ParseNode = input,
                Parent = sf
            };

            var astObj = new AstWriteObject() {
                Parent = astu
            };
            
            for (int i = 0; i < input.AllHandlers.Count; i++) {
                template = (Template)input.AllHandlers[i].Code;
                
                new AstWritePropertyName() {
                    Template = template,
                    Parent = astObj
                };

                new AstWriteDelimiter() {
                    Delimiter = ':',
                    Parent = astObj
                };

                if (template is TObjArr) {
                    var astArray = new AstWriteArray() {
                        Parent = astObj
                    };

                    var astLoop = new AstForLoop() {
                        Template = template,
                        Parent = astArray
                    };

                    new AstWritePropertyValue() {
                        Template = ((TObjArr)template).App,
                        VariableName = "listApp",
                        Parent = astLoop
                    };
                } else {
                    new AstWritePropertyValue() {
                        Template = template,
                        Parent = astObj
                    };
                }
                
                if ((i + 1) < input.AllHandlers.Count) {
                    new AstWriteDelimiter() {
                        Delimiter = ',',
                        Parent = astObj
                    };
                }
            }

            new AstCodeStatement() {
                Statement = "return (bufferSize - nextSize);",
                Parent = astu
            };
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
                    if (pn.Handler.Code is TObjArr) {
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

                    if (!(pn.Handler.Code is ActionProperty)) {
                        new AstSetValue() {
                            ParseNode = pn,
                            Parent = pj
                        };
                    }

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