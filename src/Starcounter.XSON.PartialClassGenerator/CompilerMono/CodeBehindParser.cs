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
using Starcounter.XSON.PartialClassGenerator;

namespace Starcounter.XSON.Compiler.Mono {
    /// <summary>
    /// Class CodeBehindAnalyzer
    /// </summary>
    public static class CodeBehindParser {
        /// <summary>
        /// Parses the specified c# file using Mono.CSharp and builds a metadata
        /// structure used to generate code for json Apps.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <param name="codeBehindFilename">The codebehind filename.</param>
        /// <param name="useRoslynParser">Instruct the analyzer to use the newer
        /// Roslyn-based parser</param>
        /// <returns>CodeBehindMetadata.</returns>
        public static CodeBehindMetadata Analyze(string className, string codebehind, string filePathNote, bool useRoslynParser = false) {
            CodeBehindMetadata metadata;
            CSharpToken token;
            MonoCSharpEnumerator mce;
			bool beforeNS = true;
           
            if ((codebehind == null) || codebehind.Equals("") ) {
                return CodeBehindMetadata.Empty;
            }

            if (useRoslynParser) {
                var parser = new RoslynCodeBehindParser(className, codebehind, filePathNote);
                return parser.ParseToMetadata();
            }

            mce = new MonoCSharpEnumerator(codebehind, filePathNote );
            metadata = new CodeBehindMetadata();
            CodeBehindClassInfo lastClassInfo = null; 
            while (mce.MoveNext()) {
                token = mce.Token;
                switch (token) {
					case CSharpToken.USING:
						if (beforeNS) {
							// We only add the using-directives that are specified in the header
							// of the codebehind file, before the first namespace is declared since
							// otherwise we have to place the usingdirectives in the correct namespace 
							// for the generated code.
							AddUsingDirective(mce, metadata);
						}
						break;
                    case CSharpToken.NAMESPACE:
						beforeNS = false;
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
                    case CSharpToken.IDENTIFIER:
                        // Since there is no specific token for constructor we check the name. If the name
                        // of the identifier is the same as the current class we check if it's a constructor.
                        if (mce.Value.Equals(mce.CurrentClass)) {
                            AnalyzeConstructor(mce);
                        }
                        break;
                }

            }
            /*
            if (metadata.RootClassInfo == null) {
                metadata.JsonPropertyMapList.Add( new CodeBehindClassInfo(null) {
                    IsRootClass = true
                });
#if DEBUG
                if (metadata.RootClassInfo == null) {
                    throw new Exception("Expected root class information in partial class code-gen");
                }
#endif
            }*/

            return metadata;
        }

        private static void AnalyzeConstructor(MonoCSharpEnumerator mce) {
            string identifier = mce.Value;

            if (mce.PreviousToken != CSharpToken.STATIC 
                && mce.PreviousToken != CSharpToken.NEW
                && identifier.Equals(mce.CurrentClass)) {
                if (mce.Peek() == CSharpToken.OPEN_PARENS) {
                    // This is an constructor. Since we don't support constructors that take parameters and
                    // a default constructor is already defined in the generated code we always throw an 
                    // exception here. If the extension is used in visual studio it will generate a nice
                    // compiler-error.
                    throw new Exception("Custom constructors are not currently supported in typed json (" + identifier +").");
                }
            }
        }

		private static void AddUsingDirective(MonoCSharpEnumerator mce, CodeBehindMetadata metadata) {
			string usingDirective = "";

			while (mce.MoveNext()) {
				if (mce.Token == CSharpToken.SEMICOLON) {
					metadata.UsingDirectives.Add(usingDirective);
					break;
				} else if (mce.Token == CSharpToken.IDENTIFIER) {
					usingDirective += mce.Value;
				} else if (mce.Token == CSharpToken.DOT) {
					usingDirective += ".";
				} else if (mce.Token == CSharpToken.ASSIGN) {
					usingDirective += "=";
				}
			}
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
        /// If an IBound interface declaration is found the boundClass parameter will be set to the 
		/// generic argument
        /// /// </summary>
        /// <param name="mce"></param>
        /// <param name="baseClass"></param>
        /// <param name="boundClass"></param>
        private static void ProcessClassDeclaration(MonoCSharpEnumerator mce, out string baseClass, out string boundClass) {
            string currentClassStr = "";
            string genericArgStr = "";
			
			baseClass = null;
			boundClass = null;

			var token = mce.Peek();
			if (!(token == CSharpToken.COLON)) {
				if (token == CSharpToken.OP_GENERICS_LT)
					throw new Exception("Generic declaration for typed json is not supported.");
			}

			// Since we allow inheritance we have no idea if the class we found is a valid
			// typed json class or not. So we have to assume that the first one is the basetype
			// and not an interface or something.
            while (mce.MoveNext()) {
                if (mce.Token == CSharpToken.OPEN_BRACE ) {
					if (baseClass == null)
						baseClass = currentClassStr;
                    break;
                }
				else if (mce.Token == CSharpToken.COMMA) {
					if (baseClass == null)
						baseClass = currentClassStr;
					currentClassStr = "";
                } else if (mce.Token == CSharpToken.IDENTIFIER) {
                    currentClassStr += mce.Value;
                }
                else if (mce.Token == CSharpToken.DOT) {
                    currentClassStr += ".";
                }
                else if (mce.Token == CSharpToken.OP_GENERICS_LT) {
					if (!currentClassStr.Equals("IBound")) {
						// Not an IBound interface. We are not interested in this generic argument
						while (mce.MoveNext()) {
							if (mce.Token == CSharpToken.OP_GENERICS_GT)
								break;
						}
					} else {
						while (mce.MoveNext()) {
							if (mce.Token == CSharpToken.OP_GENERICS_GT) {
								boundClass = genericArgStr;
								break;
							}
							else if (mce.Token == CSharpToken.IDENTIFIER) {
								genericArgStr += mce.Value;
							}
							else if (mce.Token == CSharpToken.DOT) {
								genericArgStr += ".";
							}
							else if (mce.Token == CSharpToken.COMMA) {
								throw new Exception("Only one generic argument for an IBound interface is supported");	
							}
						}
					}
                }

            }
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
                } else {
                    SkipBlock(mce);
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
					// TODO:
					// Need to handle the analyzing of attributes better since we now support
					// more than one attribute declared on a json class.
                    var jmi = CodeBehindClassInfo.EvaluateAttributeString(attribute, mce.LastFoundJsonAttribute);
                    if (jmi != null) {
						if (jmi.RawDebugJsonMapAttribute != null)
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
				} else if (mce.Token == CSharpToken.OPEN_PARENS) {
					attribute += "(";
				} else if (mce.Token == CSharpToken.CLOSE_PARENS) {
					attribute += ")";
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
            string baseClass;
            string boundClass;
            
            // First get the name of the class.
            mce.MoveNext();
            foundClassName = mce.Value;

            // We need to remove the last read attribute here, even if we are not interested in the class 
            // to avoid it gets connected to another class or method.
            classInfo = mce.LastFoundJsonAttribute;
            mce.LastFoundJsonAttribute = null;

            ProcessClassDeclaration(mce, out baseClass, out boundClass );
            if (className.Equals(foundClassName)) {
#if DEBUG
                if (metadata.RootClassInfo != null)
                    throw new Exception("Did not expect root class information to be set in partial class codegen");
#endif
                if (classInfo == null) {
                    classInfo = new CodeBehindClassInfo(null);
                    classInfo.IsRootClass = true;
                    classInfo.IsDeclaredInCodeBehind = true;
                } else if (!classInfo.IsRootClass) {
                    throw new Exception(String.Format("The class {0} has the attribute {1} although it has the same name as the .json file name.",
                        foundClassName, classInfo.RawDebugJsonMapAttribute));
                }
                classInfo.Namespace = mce.CurrentNamespace;
                classInfo.BaseClassName = baseClass;
                classInfo.BoundDataClass = boundClass;

                // This is the main codebehind class. If attribute is not set, we add it here and parse it
                // to get the correct information on how to connect the codebehind classes.
                if (classInfo.RawDebugJsonMapAttribute == null) {
                    classInfo = CodeBehindClassInfo.EvaluateAttributeString(className + "_json", classInfo);
                }

                //                    classInfo.AutoBindToDataObject = (genericArg != null);
                metadata.CodeBehindClasses.Add(classInfo);

#if DEBUG
                if (metadata.RootClassInfo == null)
                    throw new Exception("Did expect root class information to be set in partial class codegen");
#endif
                if (classInfo.ClassName == null)
                    classInfo.ClassName = className;
            } else {
                var info = classInfo; // JsonMapInfo.EvaluateAttributeString(attribute);
                if (info != null) {
                    //                       info.AutoBindToDataObject = (genericArg != null);
                    info.ClassName = foundClassName;
                    info.BaseClassName = baseClass;
                    info.BoundDataClass = boundClass;
                    //               info.JsonMapName = attribute.Raw;
                    info.Namespace = mce.CurrentNamespace;
                    info.ParentClasses = mce.ClassList;
                    metadata.CodeBehindClasses.Add(info);
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
