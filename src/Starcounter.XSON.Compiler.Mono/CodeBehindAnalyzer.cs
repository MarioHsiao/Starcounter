// ***********************************************************************
// <copyright file="CodeBehindAnalyzer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mono.CSharp;
using Starcounter.XSON.Metadata;

namespace Starcounter.XSON.Compiler.Mono {
    /// <summary>
    /// Class CodeBehindAnalyzer
    /// </summary>
    internal static class CodeBehindAnalyzer {
        /// <summary>
        /// Parses the specified c# file using Roslyn and builds a metadata
        /// structure used to generate code for json Apps.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <param name="codeBehindFilename">The codebehind filename.</param>
        /// <returns>CodeBehindMetadata.</returns>
        internal static CodeBehindMetadata Analyze(string className, string codeBehindFilename) {
            CodeBehindMetadata metadata;
            CSharpToken token;
            MonoCSharpEnumerator mce;

            if ((codeBehindFilename == null) || (!File.Exists(codeBehindFilename))) {
                return CodeBehindMetadata.Empty;
            }

            mce = new MonoCSharpEnumerator(codeBehindFilename);
            metadata = new CodeBehindMetadata();
            while (mce.MoveNext()) {
                token = mce.Token;
                switch (token) {
                    case CSharpToken.NAMESPACE:
                        AnalyzeNamespaceNode(mce);
                        break;
                    case CSharpToken.CLASS:
                        AnalyzeClassNode(className, mce, metadata);
                        break;       
                    case CSharpToken.OPEN_BRACE:
                        // If we get an OPEN_BRACE token here it means it belongs to some block
                        // we are not interested in. We just push it to the stack of tokens so
                        // we match the close braces.
                        mce.PushBlock();
                        break;
                    case CSharpToken.CLOSE_BRACE:
                        mce.PopBlock();
                        break;
                    case CSharpToken.OPEN_BRACKET:
                        // There is not a specific token for attributes so we assume that the 
                        // value inside any bracket is an attribute. If it start with "json."
                        // we consider it a valid jsonmap attribute and save it for the next class
                        // or method.
                        AnalyzeAttributeDeclarationNode(mce);
                        break;
                    case CSharpToken.VOID:
                        // There is not a specific token for methods so we assume that a void token might
                        // mean that we have a Handle method to take care of.

                        // TODO: 
                        // need to look for STATIC token since Handle methods cannot be static.
                        AnalyzeHandleMethodNode(mce, metadata);
                        break;
                }

            }
            return metadata;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        private static bool IsJsonMapAttribute(string attribute) {
            return ((attribute != null) && (attribute.StartsWith("json.")));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mce"></param>
        private static void SkipToOpenBrace(MonoCSharpEnumerator mce) {
            while (mce.MoveNext()) {
                if (mce.Token == CSharpToken.OPEN_BRACE)
                    break;
            }
        }

        /// <summary>
        /// Skips all tokens in the next block that is found.
        /// </summary>
        /// <param name="mce"></param>
        private static void SkipBlock(MonoCSharpEnumerator mce) {
            int braceCount;
            CSharpToken token;

            braceCount = 0;
            while (mce.MoveNext()) {
                token = mce.Token;
                if (token == CSharpToken.OPEN_BRACE)
                    braceCount++;

                if (token == CSharpToken.CLOSE_BRACE) {
                    braceCount--;
                    if (braceCount == 0)
                        break;
                }
            }
        }

        /// <summary>
        /// Assumes that the current position of the enumerator is positioned at the class identifer.
        /// Searches the inheritance list (if any) for a valid baseclass for typed json. If a baseclass
        /// is found, the generic argument will be retrieved as well if it exists.
        /// /// </summary>
        /// <param name="mce"></param>
        /// <param name="genericArgument"></param>
        /// <returns></returns>
        private static bool IsTypedJsonClass(MonoCSharpEnumerator mce, out string genericArgument) {
            bool isTypedJsonClass;
            string baseClassName;

            genericArgument = null;
            isTypedJsonClass = false;
            if (mce.Peek() == CSharpToken.COLON) { // The class have a inheritance list.
                mce.MoveNext(); // COLON
                while (mce.MoveNext()) {
                    if (mce.Token == CSharpToken.OPEN_BRACE)
                        break;

                    if (mce.Token == CSharpToken.IDENTIFIER) {
                        baseClassName = mce.Value;
                        if (baseClassName.Equals("Json")) {
                            // A valid baseclass. This class is a typed json class.
                            // Now we check if a generic argument exists.
                            isTypedJsonClass = true;
                            if (mce.Peek() == CSharpToken.OP_GENERICS_LT) {
                                mce.MoveNext(); // OP_GENERICS_LT
                                mce.MoveNext(); // IDENTIFIER

                                if (mce.Peek() != CSharpToken.OP_GENERICS_GT)
                                    throw new NotSupportedException("Baseclass for typed json with more than one generic argument is not currently supported.");
                                genericArgument = mce.Value;
                            }
                            break;
                        }
                    }
                }
            }
            return isTypedJsonClass;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mce"></param>
        /// <param name="metadata"></param>
        private static void AnalyzeHandleMethodNode(MonoCSharpEnumerator mce, CodeBehindMetadata metadata) {
            string methodName;
            string inputTypeName = "";
            CSharpToken prevToken = CSharpToken.UNDEFINED;

            mce.MoveNext();
            if (mce.Token == CSharpToken.IDENTIFIER) {
                methodName = mce.Value;
                if ("Handle".Equals(methodName)) {
                    // Yep, this is a handle method. Lets check that it is valid and retrieve the inputtype.
                    while (mce.MoveNext()) {
                        if (mce.Token == CSharpToken.CLOSE_PARENS) {
                            var info = new InputBindingInfo() {
                                DeclaringClassName = mce.CurrentClass,
                                DeclaringClassNamespace = mce.CurrentNamespace,
                                FullInputTypeName = inputTypeName
                            };
                            metadata.InputBindingList.Add(info);
                            break;
                        } else if (mce.Token == CSharpToken.IDENTIFIER) {
                            if (prevToken == CSharpToken.IDENTIFIER) {
                                // There is no separation between the type declaration and the parametername
                                // so we have to assume that when we get two identifier tokens after each other
                                // the last one is the parametername.
                                continue;
                            }

                            inputTypeName += mce.Value;
                        } else if (mce.Token == CSharpToken.DOT) {
                            inputTypeName += ".";
                        } else if (mce.Token == CSharpToken.COMMA) {
                            throw new Exception("Only one parameter is allowed on an json Handle method.");
                        }
                        prevToken = mce.Token;
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mce"></param>
        private static void AnalyzeAttributeDeclarationNode(MonoCSharpEnumerator mce) {
            string attribute = "";

            while (mce.MoveNext()) {
                if (mce.Token == CSharpToken.CLOSE_BRACKET) {
                    if (IsJsonMapAttribute(attribute))
                        mce.LastFoundJsonAttribute = attribute;
                    break;
                } else if (mce.Token == CSharpToken.IDENTIFIER) {
                    attribute += mce.Value;
                } else if (mce.Token == CSharpToken.DOT) {
                    attribute += ".";
                } else if (mce.Token == CSharpToken.COMMA) {
                    attribute = null;
                }
            }
        }

        /// <summary>
        /// Gets the name of the namespace, adds it to the stack of namespaces and positions 
        /// the tokenizer so that the next token is after the opening brace.
        /// </summary>
        /// <param name="mce"></param>
        private static void AnalyzeNamespaceNode(MonoCSharpEnumerator mce){
            string ns = "";

            // Read the name of the namespace.
            while (mce.MoveNext()) {
                if (mce.Token == CSharpToken.OPEN_BRACE) {
                    mce.PushNamespace(ns);
                    break;
                } else if (mce.Token == CSharpToken.IDENTIFIER) {
                    ns += mce.Value;
                } else if (mce.Token == CSharpToken.DOT) {
                    ns += ".";
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="className"></param>
        /// <param name="mce"></param>
        /// <param name="metadata"></param>
        private static void AnalyzeClassNode(string className, MonoCSharpEnumerator mce, CodeBehindMetadata metadata) {
            string attribute;
            string foundClassName;
            string genericArg;
            
            // First get the name of the class.
            mce.MoveNext();
            foundClassName = mce.Value;

            // We need to remove the last read attribute here, even if we are not interested in the class 
            // to avoid it gets connected to another class or method.
            attribute = mce.LastFoundJsonAttribute;
            mce.LastFoundJsonAttribute = null;

            if (IsTypedJsonClass(mce, out genericArg)) {
                if (className.Equals(foundClassName)) {
                    metadata.RootNamespace = mce.CurrentNamespace;
                    metadata.GenericArgument = genericArg;
                    metadata.AutoBindToDataObject = (genericArg != null);
                } else {
                    if (IsJsonMapAttribute(attribute)) {
                        var info = new JsonMapInfo() {
                            AutoBindToDataObject = (genericArg != null),
                            ClassName = foundClassName,
                            GenericArgument = genericArg,
                            JsonMapName = attribute,
                            Namespace = mce.CurrentNamespace,
                            ParentClasses = mce.ClassList
                        };
                        metadata.JsonPropertyMapList.Add(info);
                    }
                }
                mce.PushClass(foundClassName);
                SkipToOpenBrace(mce);
           } else {
                SkipBlock(mce); // Not a typed json class. We skip the whole class.
           }
        }
    }
}
