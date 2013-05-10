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
            Prefix.Add("if (*pBuffer++ == '[') {");
            Prefix.Add("    leftBufferSize--;");
            Prefix.Add("    while (*pBuffer != '{' && *pBuffer != ']') { // find first object or end of array");
            Prefix.Add("        pBuffer++;");
            Prefix.Add("        leftBufferSize--;");
            Prefix.Add("    }");
            Prefix.Add("    if (*pBuffer != ']') {");
            Suffix.Add("    }");
            Suffix.Add("} else");
            Suffix.Add("    throw new Exception(\"Invalid array value\");");
        }
    }
}
