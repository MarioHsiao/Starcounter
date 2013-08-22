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
    public static class CodeBehindParser {
        /// <summary>
        /// Parses the specified c# file using Roslyn and builds a metadata
        /// structure used to generate code for json Apps.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <param name="codeBehindFilename">The codebehind filename.</param>
        /// <returns>CodeBehindMetadata.</returns>
        public static CodeBehindMetadata Analyze(string className, string codebehind, string filePathNote ) {
            CodeBehindMetadata metadata;
            CSharpToken token;
            MonoCSharpEnumerator mce;

            if ((codebehind == null) || codebehind.Equals("") ) {
                return CodeBehindMetadata.Empty;
            }

            mce = new MonoCSharpEnumerator(codebehind, filePathNote );
            metadata = new CodeBehindMetadata();
            CodeBehindClassInfo lastClassInfo = null; 
            while (mce.MoveNext()) {
                token = mce.Token;
                switch (token) {
                    case CSharpToken.NAMESPACE:
                        AnalyzeNamespaceNode(mce);
                        break;
                    case CSharpToken.CLASS:
                        lastClassInfo = AnalyzeClassNode(className, mce, metadata);
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
                        AnalyzeHandleMethodNode(lastClassInfo, mce, metadata);
                        break;
                }

            }
            if (metadata.RootClassInfo == null) {
                metadata.JsonPropertyMapList.Add( new CodeBehindClassInfo(null) {
                    IsRootClass = true
                });
#if DEBUG
                if (metadata.RootClassInfo == null) {
                    throw new Exception("Expected root class information in partial class code-gen");
                }
#endif
            }
            return metadata;
        }

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="attribute"></param>
//        /// <returns></returns>
//        private static bool IsJsonMapAttribute(string attribute ) {
//            var i = InterpretedJsonMapAttribute.EvaluateAttributeString(attribute);
//            return (i != null);
//        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mce"></param>
        private static void SkipToOpenBrace(MonoCSharpEnumerator mce) {
			do {
				if (mce.Token == CSharpToken.OPEN_BRACE)
					break;
			} while (mce.MoveNext());
        }

        /// <summary>
        /// Skips all tokens in the next block that is found.
        /// </summary>
        /// <param name="mce"></param>
        private static void SkipBlock(MonoCSharpEnumerator mce) {
            int braceCount;
            CSharpToken token;

            braceCount = 0;
            do {
                token = mce.Token;
                if (token == CSharpToken.OPEN_BRACE)
                    braceCount++;

                if (token == CSharpToken.CLOSE_BRACE) {
                    braceCount--;
                    if (braceCount == 0)
                        break;
                }
            } while (mce.MoveNext());
        }

        /// <summary>
        /// Assumes that the current position of the enumerator is positioned at the class identifer.
        /// If a baseclass
        /// is found, the generic argument will be retrieved as well if it exists.
        /// /// </summary>
        /// <param name="mce"></param>
        /// <param name="baseClass"></param>
        /// <param name="genericArgument"></param>
        /// <returns></returns>
        private static void ProcessClassDeclaration(MonoCSharpEnumerator mce, out string baseClass, out string genericArgument, out string baseClassGenericArgument) {
            // bool isTypedJsonClass;
            string baseClassNameStr = "";
            string genericArgStr = "";
            string baseClassGenericArgStr = "";

            genericArgument = null;
            baseClass = null;
            baseClassGenericArgument = null;
            //    isTypedJsonClass = false;

            // Since we allow inheritance we have no idea if the class we found is a valid
            // typed json class or not. So we have to assume that the first one is the basetype
            // and not an interface or something.
            while (mce.MoveNext()) {
                if (mce.Token == CSharpToken.OPEN_BRACE || mce.Token == CSharpToken.COMMA) {
                    baseClass = baseClassNameStr;
                    //		isTypedJsonClass = true;
                    break;
                }
                else if (mce.Token == CSharpToken.IDENTIFIER) {
                    baseClassNameStr += mce.Value;
                }
                else if (mce.Token == CSharpToken.DOT) {
                    baseClassNameStr += ".";
                }
                else if (mce.Token == CSharpToken.OP_GENERICS_LT) {
                    while (mce.MoveNext()) {
                        if (mce.Token == CSharpToken.OP_GENERICS_GT) {
                            genericArgument = genericArgStr;
                            break;
                        }
                        else if (mce.Token == CSharpToken.IDENTIFIER) {
                            genericArgStr += mce.Value;
                        }
                        else if (mce.Token == CSharpToken.DOT) {
                            genericArgStr += ".";
                        }
                        else if (mce.Token == CSharpToken.COMMA) {
                            genericArgStr += ",";
                        }
                    }
                }
                else if (mce.Token == CSharpToken.COLON) {
                    while (mce.MoveNext()) {
                        if (mce.Token == CSharpToken.OPEN_BRACE || mce.Token == CSharpToken.COMMA) {
                            baseClass = baseClassNameStr;
                            //       isTypedJsonClass = true;
                            break;
                        }
                        else if (mce.Token == CSharpToken.IDENTIFIER) {
                            baseClassNameStr += mce.Value;
                        }
                        else if (mce.Token == CSharpToken.DOT) {
                            baseClassNameStr += ".";
                        }
                        else if (mce.Token == CSharpToken.OP_GENERICS_LT) {
                            while (mce.MoveNext()) {
                                if (mce.Token == CSharpToken.OP_GENERICS_GT) {
                                    baseClassGenericArgument = baseClassGenericArgStr;
                                    break;
                                }
                                else if (mce.Token == CSharpToken.IDENTIFIER) {
                                    baseClassGenericArgStr += mce.Value;
                                }
                                else if (mce.Token == CSharpToken.DOT) {
                                    baseClassGenericArgStr += ".";
                                }
                                else if (mce.Token == CSharpToken.COMMA) {
                                    baseClassGenericArgStr += ",";
                                }
                            }
                        }
                    }
                    break;
                }

            }
            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mce"></param>
        /// <param name="metadata"></param>
        private static void AnalyzeHandleMethodNode(CodeBehindClassInfo ci, MonoCSharpEnumerator mce, CodeBehindMetadata metadata) {
            string methodName;
            string inputTypeName = "";
            CSharpToken prevToken = CSharpToken.UNDEFINED;

            mce.MoveNext();
            if (mce.Token == CSharpToken.IDENTIFIER) {
                methodName = mce.Value;
                if ("Handle".Equals(methodName)) {
                    // Yep, this is a handle method. Lets check that it is valid and retrieve the input type.
                    while (mce.MoveNext()) {
                        if (mce.Token == CSharpToken.CLOSE_PARENS) {
                            var info = new InputBindingInfo() {
                                DeclaringClassName = mce.CurrentClass,
                                DeclaringClassNamespace = mce.CurrentNamespace,
                                FullInputTypeName = inputTypeName
                            };
//                            metadata.RootClassInfo.InputBindingList.Add(info);
                            ci.InputBindingList.Add(info);
                            break;
                        } else if (mce.Token == CSharpToken.IDENTIFIER) {
                            if (prevToken == CSharpToken.IDENTIFIER) {
                                // There is no separation between the type declaration and the parameter name
                                // so we have to assume that when we get two identifier tokens after each other
                                // the last one is the parameter name.
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
                    var jmi = CodeBehindClassInfo.EvaluateAttributeString(attribute);
                    if (jmi != null) {
                        jmi.IsDeclaredInCodeBehind = true;
                        mce.LastFoundJsonAttribute = jmi;
                    }
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
        private static CodeBehindClassInfo AnalyzeClassNode(string className, MonoCSharpEnumerator mce, CodeBehindMetadata metadata) {
            CodeBehindClassInfo classInfo;
            string foundClassName;
            string genericArg;
            string baseClassGenericArg;
            string baseClass;
            
            // First get the name of the class.
            mce.MoveNext();
            foundClassName = mce.Value;

            // We need to remove the last read attribute here, even if we are not interested in the class 
            // to avoid it gets connected to another class or method.
            classInfo = mce.LastFoundJsonAttribute;
            mce.LastFoundJsonAttribute = null;

            ProcessClassDeclaration(mce, out baseClass, out genericArg, out baseClassGenericArg );
                if (className.Equals(foundClassName)) {
#if DEBUG
                    if (metadata.RootClassInfo != null)
                        throw new Exception("Did not expect root class information to be set in partial class codegen");
#endif
                    if (classInfo == null) {
                        classInfo = new CodeBehindClassInfo(null);
                        classInfo.IsRootClass = true;
                        classInfo.IsDeclaredInCodeBehind = true;
                    }
                    else if (!classInfo.IsRootClass) {
                        throw new Exception(String.Format("The class {0} has the attribute {1} although it has the same name as the .json file name.",
                            foundClassName, classInfo.RawJsonMapAttribute));
                    }
                    classInfo.Namespace = mce.CurrentNamespace;
                    classInfo.GenericArg = genericArg;
                    classInfo.BaseClassGenericArg = baseClassGenericArg;
                    classInfo.BaseClassName = baseClass;
                    classInfo.AutoBindToDataObject = (genericArg != null);
					metadata.JsonPropertyMapList.Add(classInfo);

#if DEBUG
                    if (metadata.RootClassInfo == null)
                        throw new Exception("Did expect root class information to be set in partial class codegen");
#endif
                    if (classInfo.ClassName == null)
                        classInfo.ClassName = className;
                }
                else {
                    var info = classInfo; // JsonMapInfo.EvaluateAttributeString(attribute);
                    if (info != null) {
                        info.AutoBindToDataObject = (genericArg != null);
                        info.ClassName = foundClassName;
                        info.BaseClassName = baseClass;
                        info.GenericArg = genericArg;
                        info.BaseClassGenericArg = baseClassGenericArg;
                        //               info.JsonMapName = attribute.Raw;
                        info.Namespace = mce.CurrentNamespace;
                        info.ParentClasses = mce.ClassList;
                        metadata.JsonPropertyMapList.Add(info);
                    }
                }
                mce.PushClass(foundClassName);
                SkipToOpenBrace(mce);
//            } else {
 //               SkipBlock(mce); // Not a typed json class. We skip the whole class.
 //           }
            return classInfo;
        }

    }

}
