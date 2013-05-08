// ***********************************************************************
// <copyright file="AstVerifier.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    /// <summary>
    /// 
    /// </summary>
    internal class AstValueJump : AstNode {
        internal ParseNode ParseNode { get; set; }

        internal override string DebugString {
            get {
                return "AstValueJump";
            }
        }

        internal override void GenerateCsCodeForNode() {
            Prefix.Add("nextSize -= valueSize;");
            Prefix.Add("if (nextSize < 0) {");
            Prefix.Add("    throw new Exception(\"Unable to deserialize App. Unexpected end of content\");");
            Prefix.Add("}");
            Prefix.Add("pfrag += valueSize;");
        }
    }
}