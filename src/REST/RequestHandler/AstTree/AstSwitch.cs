

using System.Text;
namespace Starcounter.Internal.Uri
{
    internal class AstSwitch : AstVerifier
    {
        internal ParseNode ParseNode { get; set; }

        internal override string DebugString {
            get {
                return "switch (i=" + ParseNode.MatchCharInTemplateRelative + ")";
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            GenVerifier(ParseNode.MatchCharInTemplateRelative,false);
            Prefix.Add("switch (*pfrag) {");
            Suffix.Add("}");
        }

        internal override bool IndentChildren {
            get {
                return true;
            }
        }

    }
}
