// ***********************************************************************
// <copyright file="AstParseCode.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
namespace Starcounter.Internal.Uri {
    /// <summary>
    /// Class AstParseCode
    /// </summary>
    internal class AstParseCode : AstNode {

        /// <summary>
        /// Gets or sets the parse node.
        /// </summary>
        /// <value>The parse node.</value>
        internal ParseNode ParseNode { get; set; }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        /// <value>The debug string.</value>
        /// <exception cref="System.NotImplementedException">TODO! Add more types here</exception>
        internal override string DebugString {
            get {
                switch (ParseNode.Match) {
                    case (byte)'s':
                        return "ParseString(...)";
                    case (byte)'i':
                        return "ParseInt(...)";
                    default:
                        throw new NotImplementedException("TODO! Add more types here");
                }
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        /// <exception cref="System.NotImplementedException">TODO! Add more types here</exception>
        internal override void GenerateCsCodeForNode() {
            var sb = new StringBuilder();
            switch (ParseNode.Match) {
                case (byte)'s':
                    Prefix.Add("string val;");
                    Prefix.Add("if (ParseUriString(fragment,size,out val)) {");
                    break;
                case (byte)'i':
                    Prefix.Add("int val;");
                    Prefix.Add("if (ParseUriInt(fragment,size,out val)) {");
                    break;
                default:
                    throw new NotImplementedException("TODO! Add more types here");
            }
            Suffix.Add("}");
        }

    }
}
