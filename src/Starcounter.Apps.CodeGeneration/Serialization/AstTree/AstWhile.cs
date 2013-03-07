// ***********************************************************************
// <copyright file="AstSwitch.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    /// <summary>
    /// Class AstSwitch
    /// </summary>
    internal class AstWhile : AstNode {
        /// <summary>
        /// Gets the debug string.
        /// </summary>
        /// <value>The debug string.</value>
        internal override string DebugString {
            get {
                return "while (nextSize > 0)";
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            Prefix.Add("while (nextSize > 0) {");
            Suffix.Add("}");
        }
    }
}
