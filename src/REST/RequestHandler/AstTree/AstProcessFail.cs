

using System;
using System.Text;
namespace Starcounter.Internal.Uri {
    internal class AstProcessFail : AstNode {

        internal override string DebugString {
            get {
                return "fail";
            }
        }


        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            Suffix.Add("handler = null;");
            Suffix.Add("resource = null;");
            Suffix.Add("return false;");
        }

    }
}


