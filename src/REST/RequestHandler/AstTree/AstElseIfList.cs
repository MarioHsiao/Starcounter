﻿// ***********************************************************************
// <copyright file="AstElseIfList.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;

namespace Starcounter.Internal.Uri {
    /// <summary>
    /// Class AstElseIfList
    /// </summary>
    internal class AstElseIfList : AstJump {

        /// <summary>
        /// Gets or sets the parse node.
        /// </summary>
        /// <value>The parse node.</value>
        internal ParseNode ParseNode { get; set; }

        /// <summary>
        /// Gets a value indicating whether [indent children].
        /// </summary>
        /// <value><c>true</c> if [indent children]; otherwise, <c>false</c>.</value>
        internal override bool IndentChildren {
            get {
                return false;
            }
        }

        /// <summary>
        /// Gets the debug string.
        /// </summary>
        /// <value>The debug string.</value>
        internal override string DebugString {
            get {
                return "// ifelseif";
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            GenerateJump(ParseNode.MatchParseCharInTemplateRelativeToSwitch,!(Parent is AstUnsafe));
        }

    }
}