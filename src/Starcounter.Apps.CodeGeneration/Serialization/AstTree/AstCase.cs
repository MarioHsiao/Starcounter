// ***********************************************************************
// <copyright file="AstCase.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;
using Starcounter.Internal.Uri;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    /// <summary>
    /// Class AstCase
    /// </summary>
    internal class AstCase : AstNode
    {
        /// <summary>
        /// Gets or sets the parse node.
        /// </summary>
        /// <value>The parse node.</value>
        internal ParseNode ParseNode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if this is the default
        /// label in a switch.
        /// </summary>
        internal bool IsDefault { get; set; }

        /// <summary>
        /// Gets the debug string.
        /// </summary>
        /// <value>The debug string.</value>
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

        /// <summary>
        /// Generates the cs code for node.
        /// </summary>
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
                    Prefix.Add("case (byte)'\\r':");
                }

                // Skip the character we switched on.
                Prefix.Add("    pfrag++;");
                Prefix.Add("    nextSize--;");
            }
            Suffix.Add("   break;");
        }
    }
}
