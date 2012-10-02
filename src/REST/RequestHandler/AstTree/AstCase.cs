

using System.Text;
namespace Starcounter.Internal.Uri
{
    internal class AstCase : AstNode
    {
//        internal char Match { get; set; }
        internal ParseNode ParseNode { get; set; }

        internal override string DebugString {
            get {
                if (ParseNode.Match == 0) {
                    return "case '':";
                }
                return "case '" + (char)ParseNode.Match + "':";
            }
        }

        internal override void GenerateCsCodeForNode() {
            var sb = new StringBuilder();
            sb.Append("case (byte)'");
            sb.Append((char)ParseNode.Match);
            sb.Append("':");
            Prefix.Add(sb.ToString());
//            Prefix.Add("Console.WriteLine(\"Tested true for '" + (char)ParseNode.Match + "'\");");
            Suffix.Add("   break;");
        }
    }
}
