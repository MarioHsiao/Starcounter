

using System;
using System.Text;
namespace Starcounter.Internal.Uri {
    internal class AstParseCode : AstNode {

        internal ParseNode ParseNode { get; set; }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
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
