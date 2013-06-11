// ***********************************************************************
// <copyright file="AstParseCode.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration {
    /// <summary>
    /// 
    /// </summary>
    internal class AstParseJsonObjectArray : AstNode {
        internal Template Template { get; set; }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        /// <value>The debug string.</value>
        internal override string DebugString {
            get {
                return "ParseObjectArray";
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            Prefix.Add("if (*pBuffer != '[')");
            Prefix.Add("    JsonHelper.ThrowWrongValueTypeException(null, \"" + Template.TemplateName + "\", \"" + Template.JsonType + "\", \"\");");
            Prefix.Add("while (leftBufferSize > 0) {");
            Prefix.Add("    while (*pBuffer != '{' && *pBuffer != ']') { // find first object or end of array");
            Prefix.Add("        leftBufferSize--;");
            Prefix.Add("        if (leftBufferSize < 0)");
            Prefix.Add("            JsonHelper.ThrowUnexpectedEndOfContentException();");
            Prefix.Add("        pBuffer++;");
            Prefix.Add("    }");
            Prefix.Add("    if (*pBuffer == ']')");
            Prefix.Add("        break;");
            Suffix.Add("}");
            Suffix.Add("if (*pBuffer == ']') {");
            Suffix.Add("    leftBufferSize--;");
            Suffix.Add("    if (leftBufferSize < 0)");
            Suffix.Add("        JsonHelper.ThrowUnexpectedEndOfContentException();");
            Suffix.Add("    pBuffer++;");
            Suffix.Add("}");
        }
    }
}
