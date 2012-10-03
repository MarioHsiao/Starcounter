using System.Text;

namespace Starcounter.Internal.Uri {
    internal class AstElseIfList : AstVerifier {

        internal ParseNode ParseNode { get; set; }

        internal override bool IndentChildren {
            get {
                return false;
            }
        }

        internal override string DebugString {
            get {
                return "// ifelseif";
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            GenVerifier(ParseNode.MatchParseCharInTemplateRelativeToSwitch,!(Parent is AstUnsafe));
        }

    }
}