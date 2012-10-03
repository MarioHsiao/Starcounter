

using System.Text;
namespace Starcounter.Internal.Uri {
    internal class AstProcessFunction : AstNode {
        internal char Match { get; set; }

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
            sb.Append("public override bool Process( IntPtr fragment, int size, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {");
            Prefix.Add("");
            Prefix.Add(sb.ToString());
            Suffix.Add("}");
        }

    }
}
