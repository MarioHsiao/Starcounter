// ***********************************************************************
// <copyright file="AstCase.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    internal class AstCase : AstNode {
        internal ParseNode ParseNode { get; set; }

        internal bool IsDefault { get; set; }

        internal override string DebugString {
            get {
                if (IsDefault) {
                    return "default:";
                }

                if (ParseNode.Match == 0) {
                    return "case '':";
                }
                return "case '" + (char)ParseNode.Match + "':";
            }
        }

        internal override void GenerateCsCodeForNode() {
            if (IsDefault) {
                Prefix.Add("default:");
            } else {
                var sb = new StringBuilder();
                sb.Append("case (byte)'");
                sb.Append((char)ParseNode.Match);
                sb.Append("':");
                Prefix.Add(sb.ToString());

                if (ParseNode.Match == (byte)' ') {
                    Prefix.Add("case (byte)'\"':");
                }

                // Skip the character we switched on.
                Prefix.Add("    pfrag++;");
                Prefix.Add("    nextSize--;");
            }

            // AstFail throws exception so we cannot add a break if the child is a fail.
            if (!(this.Children.Count == 1 && this.Children[0] is AstProcessFail))
                Suffix.Add("   break;");
        }
    }
}
