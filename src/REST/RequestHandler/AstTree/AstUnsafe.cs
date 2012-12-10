// ***********************************************************************
// <copyright file="AstUnsafe.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;
namespace Starcounter.Internal.Uri {
    /// <summary>
    /// Class AstUnsafe
    /// </summary>
    internal class AstUnsafe : AstNode {

       // internal string VerificationIndex;

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            Prefix.Add("unsafe {");
            Prefix.Add("   byte* pfrag = (byte*)fragment;");
            Prefix.Add("   int nextSize = size;");
            Suffix.Add("}");
        }
    }
}
