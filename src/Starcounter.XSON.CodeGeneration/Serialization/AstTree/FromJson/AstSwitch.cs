// ***********************************************************************
// <copyright file="AstSwitch.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;

namespace Starcounter.Internal.Application.CodeGeneration {
    /// <summary>
    /// Class AstSwitch
    /// </summary>
    internal class AstSwitch : AstNode
    {
        /// <summary>
        /// Gets or sets the parse node.
        /// </summary>
        /// <value>The parse node.</value>
        internal ParseNode ParseNode { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        internal override string DebugString {
            get {
                return "switch (i=" + ParseNode.MatchCharInTemplateAbsolute + ")";
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            Prefix.Add("switch (*pBuffer) {");
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
