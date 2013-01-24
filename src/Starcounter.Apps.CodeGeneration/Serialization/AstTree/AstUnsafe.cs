// ***********************************************************************
// <copyright file="AstUnsafe.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;
using Starcounter.Internal.Uri;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    /// <summary>
    /// Class AstUnsafe
    /// </summary>
    internal class AstUnsafe : AstNode {
        internal ParseNode ParseNode { get; set; }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            Prefix.Add("unsafe {");
            Prefix.Add("    byte* pfrag = (byte*)buffer;");
            Prefix.Add("    byte* pver;"); // = (byte*)PointerVerificationBytes;");
            Prefix.Add("    int nextSize = bufferSize;");
            Suffix.Add("}");
        }
    }
}
