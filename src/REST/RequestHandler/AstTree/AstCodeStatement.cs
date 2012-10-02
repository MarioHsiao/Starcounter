

using System.Text;
namespace Starcounter.Internal.Uri {
    internal class AstCodeStatement : AstNode {

        internal string Statement { get; set; }

        internal override string DebugString {
            get {
                return Statement;
            }
        }

        internal override void GenerateCsCodeForNode() {
            var sb = new StringBuilder();
            sb.Append(DebugString);
            Prefix.Add(sb.ToString());
        }
    }
}
