

using System.Text;
namespace Starcounter.Internal.Uri {
    internal class AstInvoke : AstNode {

        internal ParseNode ParseNode { get; set; }
        /// <summary>
        /// Creates a short single line string representing this abstract syntax tree (AST) node
        /// </summary>
        internal override string DebugString {
            get {
                return "Code.Invoke(...)";
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            Prefix.Add("handler = this;");
            Prefix.Add("if (!invoke)");
            Prefix.Add("   resource = null;");
            Prefix.Add("else");
            var sb = new StringBuilder();
            sb.Append("   resource = Code.Invoke(");
            int t = 0;
            foreach (var type in ParseNode.Handler.ParameterTypes) {
                if (t > 0) {
                    sb.Append(',');
                }
                sb.Append("val");
                if (t > 0) {
                    sb.Append(t);
                }
                t++;
            }
            sb.Append(");");
            Prefix.Add(sb.ToString());
            Prefix.Add("return true;");
        }
    }
}
