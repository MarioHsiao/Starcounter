// ***********************************************************************
// <copyright file="AstProcessFunction.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;
namespace Starcounter.Internal.Uri {
    /// <summary>
    /// Class AstProcessFunction
    /// </summary>
    internal class AstProcessFunction : AstNode {
        /// <summary>
        /// Gets or sets the match.
        /// </summary>
        /// <value>The match.</value>
        internal char Match { get; set; }

        /// <summary>
        /// Gets the debug string.
        /// </summary>
        /// <value>The debug string.</value>
        internal override string DebugString {
            get {
                return "void Process()";
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            var sb = new StringBuilder();
            sb.Append("public override bool Process(IntPtr uri, int uriSize, IntPtr fragment, int size, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {");
            Prefix.Add("");
            Prefix.Add(sb.ToString());
            Suffix.Add("}");
        }

    }
}
