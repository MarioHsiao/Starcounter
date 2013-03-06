// ***********************************************************************
// <copyright file="AstSwitch.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;
using Starcounter.Internal.Uri;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    /// <summary>
    /// Class AstSwitch
    /// </summary>
    internal class AstSwitch : AstVerifier
    {
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
                return "<VERIFY> -> switch (i=" + ParseNode.Parent.MatchChildrenAt + ")";
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            GenVerifier(ParseNode, false);
            Prefix.Add("switch (*pfrag) {");
            Suffix.Add("}");
        }

        /// <summary>
        /// Gets a value indicating whether [indent children].
        /// </summary>
        /// <value><c>true</c> if [indent children]; otherwise, <c>false</c>.</value>
        internal override bool IndentChildren {
            get {
                return true;
            }
        }

    }
}
