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
            return BuildAstTree(objTemplate, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objTemplate"></param>
        /// <param name="createChildSerializers"></param>
        /// <returns></returns>
        internal static AstNamespace BuildAstTree(TObj objTemplate, bool createChildSerializers) {
            ParseNode parseTree = ParseTreeGenerator.BuildParseTree(objTemplate);

            string ns = objTemplate.Namespace;
            if (String.IsNullOrEmpty(ns)) {
                ns = "__starcountergenerated__";
            }

            AstNamespace astNs = new AstNamespace() {
                Namespace = ns
            };

            string className = objTemplate.ClassName;
            if (String.IsNullOrEmpty(className))
                className = objTemplate.PropertyName;

            AstNode node = BuildAstTree(parseTree, objTemplate.ClassName + "Serializer");
            node.Parent = astNs;

            if (createChildSerializers) {
                CreateChildSerializers(objTemplate, node);
            }

            return astNs;
        }

        private static void CreateChildSerializers(TObj objTemplate, AstNode parent) {
            AstNode node;
            TObj tChildObj;
            string className;

            foreach (Template child in objTemplate.Properties) {
                tChildObj = null;
                if (child is TObj) {
                    tChildObj = (TObj)child;
                } else if (child is TObjArr) {
                    tChildObj = ((TObjArr)child).App;
                }

                if (tChildObj != null) {
                    className = tChildObj.ClassName;
                    if (string.IsNullOrEmpty(className))
                        className = tChildObj.PropertyName;
                    node = BuildAstTree(ParseTreeGenerator.BuildParseTree(tChildObj), className + "Serializer");
                    node.Parent = parent;
                    CreateChildSerializers(tChildObj, node);
                }
            }
        }

        /// <summary>
        /// Creates the json serializer class.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>An AstRequestProcessorClass node.</returns>
        private static AstNode BuildAstTree(ParseNode input, string className) {
            AstJsonSerializerClass jsClass = new AstJsonSerializerClass() {
                ParseNode = input,
                ClassName = className
            };
            
            var cons = new AstJSConstructor() {
                Parent = jsClass,
                ParseNode = input
            };
  
            // Removed the serializer function since the hardcoded one is almost as fast and even
            // faster in certain circumstances.
//            CreateSerializerFunction(input, jsClass);
            CreateDeserializerFunction(input, jsClass);

            return jsClass;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="jsClass"></param>
        private static AstSerializeFunction CreateSerializerFunction(ParseNode input, AstJsonSerializerClass jsClass) {
            Template template;
            AstNode nextParent;
            
            var sf = new AstSerializeFunction() {
                Parent = jsClass
            };
            jsClass.SerializeFunction = sf;
            nextParent = sf;

            //var astUnsafe = new AstUnsafe() {
            //    ParseNode = input,
            //    Parent = nextParent
            //};

            nextParent = new AstLabel(){
                Parent = nextParent,
                Label = "restart"
            };

            new AstRecreateBuffer() {
                Parent = nextParent
            };

            nextParent = new AstFixed() {
                Parent = nextParent
            };
            
            var astObj = new AstJsonObject() {
                Parent = nextParent
            };
            
            for (int i = 0; i < input.AllTemplates.Count; i++) {
                template = input.AllTemplates[i].Template;

                nextParent = new AstCheckAlreadyProcessed() {
                    Index = i,
                    Parent = astObj
                };

                new AstJsonProperty() {
                    Template = template,
                    Parent = nextParent
                };

                if (template is TObj) {
                    new AstJsonObjectValue(){
                        Template = template,
                        Parent = nextParent
                    };
                } else if (template is TObjArr) {
                    new AstJsonObjectArrayValue() {
                        Template = template,
                        Parent = nextParent
                    };
                } else {
                    new AstJsonPropertyValue() {
                        Template = template,
                        Parent = nextParent
                    };
                }
                
                if ((i + 1) < input.AllTemplates.Count) {
                    new AstJsonDelimiter() {
                        Delimiter = ',',
                        Parent = nextParent
                    };
                }
            }

            new AstReturn() {
                Parent = sf
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