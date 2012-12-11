// ***********************************************************************
// <copyright file="AstIfCallSub.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;
namespace Starcounter.Internal.Uri {
    /// <summary>
    /// Class AstIfCallSub
    /// </summary>
    internal class AstIfCallSub : AstNode {

        /// <summary>
        /// Gets or sets the rp class.
        /// </summary>
        /// <value>The rp class.</value>
        internal AstRequestProcessorClass RpClass { get; set; }
        /// <summary>
        /// Gets or sets the parse node.
        /// </summary>
        /// <value>The parse node.</value>
        internal ParseNode ParseNode { get; set; }

//        internal string Statement { get; set; }

        /// <summary>
        /// Gets the debug string.
        /// </summary>
        /// <value>The debug string.</value>
        internal override string DebugString {
            get {
                string str;
                if (this == Parent.Children[0])
                    str = "if ";
                else
                    str = "else if ";
                return str += "(" + ((RpClass==null)?"<todo>":RpClass.ClassName) + ".Process(...))";
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            var sb = new StringBuilder();
            if (this != Parent.Children[0])
                sb.Append("else ");
            sb.Append("if (");
            sb.Append(RpClass.PropertyName);
            sb.Append(".Process(uri, uriSize, (IntPtr)pfrag, nextSize, invoke, request, out handler, out resource))");
//            sb.Append( ParseNode.Parent.MatchParseCharInTemplateRelativeToProcessor);
            Prefix.Add(sb.ToString());
        }
    }
}
