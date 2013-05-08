// ***********************************************************************
// <copyright file="AstParseCode.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
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
            Prefix.Add("if (*pfrag++ == '[') {");
            Prefix.Add("    nextSize--;");
            Prefix.Add("    while (*pfrag != '{' && *pfrag != ']') { // find first object or end of array");
            Prefix.Add("        pfrag++;");
            Prefix.Add("        nextSize--;");
            Prefix.Add("    }");
            Prefix.Add("    if (*pfrag != ']') {");
            Suffix.Add("    }");
            Suffix.Add("} else");
            Suffix.Add("    throw new Exception(\"Invalid array value\");");
        }
    }
}
