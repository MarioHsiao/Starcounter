// ***********************************************************************
// <copyright file="AstCase.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;
namespace Starcounter.Internal.Uri
{
    /// <summary>
    /// Class AstCase
    /// </summary>
    internal class AstCase : AstNode
    {
//        internal char Match { get; set; }
        /// <summary>
        /// Gets or sets the parse node.
        /// </summary>
        /// <value>The parse node.</value>
        internal ParseNode ParseNode { get; set; }

        /// <summary>
        /// Gets the debug string.
        /// </summary>
        /// <value>The debug string.</value>
        internal override string DebugString {
            get {
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
            var sb = new StringBuilder();
            sb.Append("case (byte)'");
            sb.Append((char)ParseNode.Match);
            sb.Append("':");
            Prefix.Add(sb.ToString());

            if (ParseNode.Match == (byte)' ') {
                Prefix.Add("case (byte)'\\r':");
            }

//            Prefix.Add("Console.WriteLine(\"Tested true for '" + (char)ParseNode.Match + "'\");");
            Suffix.Add("   break;");
        }
    }
}
