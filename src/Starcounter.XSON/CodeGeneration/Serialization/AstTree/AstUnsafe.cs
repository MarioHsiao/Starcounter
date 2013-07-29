// ***********************************************************************
// <copyright file="AstUnsafe.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;

namespace Starcounter.Internal.Application.CodeGeneration {
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
            Prefix.Add("    byte* pBuffer = (byte*)buffer;");
            Prefix.Add("    byte* tmpBuffer = pBuffer;");
            Prefix.Add("    byte* pver = null;");
            Prefix.Add("    int leftBufferSize = bufferSize;");
            Prefix.Add("    int tmpLeftSize = leftBufferSize;");
            Suffix.Add("}");
        }
    }
}
