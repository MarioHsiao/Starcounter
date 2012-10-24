// ***********************************************************************
// <copyright file="AstRequestProcessorClass.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
namespace Starcounter.Internal.Uri {
    /// <summary>
    /// Class AstRequestProcessorClass
    /// </summary>
    internal class AstRequestProcessorClass : AstNode {

        /// <summary>
        /// The class no
        /// </summary>
        internal int ClassNo = 0;

        /// <summary>
        /// Gets or sets the process function.
        /// </summary>
        /// <value>The process function.</value>
        internal AstProcessFunction ProcessFunction { get; set; }
        /// <summary>
        /// Gets or sets the parse node.
        /// </summary>
        /// <value>The parse node.</value>
        internal ParseNode ParseNode { get; set; }

        /// <summary>
        /// Allocates the sub class no.
        /// </summary>
        internal void AllocateSubClassNo() {
            ClassNo = Root.GetNextClassNo();
        }

        /// <summary>
        /// Gets the debug string.
        /// </summary>
        /// <value>The debug string.</value>
        internal override string DebugString {
            get {
                var sb = new StringBuilder();
                if (Parent == null)
                    sb.Append("public ");
                sb.Append( "class " );
                sb.Append(ClassName);
                return sb.ToString();
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is top level.
        /// </summary>
        /// <value><c>true</c> if this instance is top level; otherwise, <c>false</c>.</value>
        internal bool IsTopLevel {
            get {
                return (Parent is AstNamespace);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is single handler.
        /// </summary>
        /// <value><c>true</c> if this instance is single handler; otherwise, <c>false</c>.</value>
        internal bool IsSingleHandler {
            get {
                return ParseNode.HandlerIndex != -1;
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            var sb = new StringBuilder();
            Prefix.Add("");
            if (IsTopLevel) {
                Prefix.Add("public class GeneratedRequestProcessor : TopLevelRequestProcessor {");
            }
            else {
                sb.Append("public class ");
                sb.Append(ClassName);
                sb.Append(" : ");
                if (IsSingleHandler) {
                    sb.Append("SingleRequestProcessor");
                    bool hasGeneric = false;
                    foreach (var t in ParseNode.Parent.ParseTypes) {
                        if (hasGeneric)
                            sb.Append(",");
                        else
                            sb.Append("<");
                        sb.Append(t);
                        hasGeneric = true;
                    }
                    if (hasGeneric)
                        sb.Append(">");
                    sb.Append(" {");
                    Prefix.Add(sb.ToString());
                }
            }
            Suffix.Add("}");
        }


        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
        internal string ClassName {
            get {
                if (Parent is AstNamespace) {
                    return "GeneratedRequestProcessor";
                }
                return "Sub" + ClassNo + "Processor";
            }
        }

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        /// <value>The name of the property.</value>
        internal string PropertyName {
            get {
                return "Sub" + ClassNo;
            }
        }
    }
}
