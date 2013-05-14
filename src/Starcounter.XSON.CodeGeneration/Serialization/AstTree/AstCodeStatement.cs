// ***********************************************************************
// <copyright file="AstCodeStatement.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;

namespace Starcounter.Internal.Application.CodeGeneration {
    /// <summary>
    /// Class AstCodeStatement
    /// </summary>
    internal class AstCodeStatement : AstNode {

        /// <summary>
        /// Gets or sets the statement.
        /// </summary>
        /// <value>The statement.</value>
        internal string Statement { get; set; }

        /// <summary>
        /// Gets the debug string.
        /// </summary>
        /// <value>The debug string.</value>
        internal override string DebugString {
            get {
                return Statement;
            }
        }

        /// <summary>
        /// Generates the cs code for node.
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            var sb = new StringBuilder();
            sb.Append(DebugString);
            Prefix.Add(sb.ToString());
        }
    }
}
