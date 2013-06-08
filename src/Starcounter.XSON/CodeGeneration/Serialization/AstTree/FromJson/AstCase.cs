// ***********************************************************************
// <copyright file="AstCase.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;

namespace Starcounter.Internal.Application.CodeGeneration {
    internal class AstCase : AstNode {
        internal ParseNode ParseNode { get; set; }

        internal override string DebugString {
            get {
                if (ParseNode.Match == 0) {
                    return "case '':";
                }
                return "case '" + (char)ParseNode.Match + "':";
            }
        }

        internal override void GenerateCsCodeForNode() {
            var sb = new StringBuilder();
            sb.Append("case (byte)'");
            sb.Append((char)ParseNode.Match);
            sb.Append("':");
            Prefix.Add(sb.ToString());

            if (ParseNode.Match == (byte)' ') {
                Prefix.Add("case (byte)'\"':");
            }

            // Skip the character we switched on.
            Prefix.Add("    pBuffer++;");
            Prefix.Add("    leftBufferSize--;");
            
            Suffix.Add("   break;");
        }
    }
}
