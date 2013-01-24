// ***********************************************************************
// <copyright file="AstProcessFunction.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    /// <summary>
    /// Class AstProcessFunction
    /// </summary>
    internal class AstSerializeFunction : AstNode {
        /// <summary>
        /// Gets the debug string.
        /// </summary>
        /// <value>The debug string.</value>
        internal override string DebugString {
            get {
                return "bool Serialize()";
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            Prefix.Add("");
            Prefix.Add("public static bool Serialize(IntPtr fragment, int size) {");
            Suffix.Add("}");
        }
    }
}
