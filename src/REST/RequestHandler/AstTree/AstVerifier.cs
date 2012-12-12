// ***********************************************************************
// <copyright file="AstVerifier.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;

namespace Starcounter.Internal.Uri {
    /// <summary>
    /// 
    /// </summary>
    internal class AstVerifier : AstNode {
        internal override string DebugString {
            get {
                return "Verify(...)";
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            Prefix.Add("if (Verify(uri, uriSize)) {");
            Suffix.Add("}");
        }

    }
}
