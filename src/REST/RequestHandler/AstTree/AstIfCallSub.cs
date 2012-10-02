

using System.Text;
namespace Starcounter.Internal.Uri {
    internal class AstIfCallSub : AstNode {

        internal AstRequestProcessorClass RpClass { get; set; }
        internal ParseNode ParseNode { get; set; }

//        internal string Statement { get; set; }

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
            sb.Append(".Process((IntPtr)pfrag, nextSize, invoke, request, out handler, out resource))");
//            sb.Append( ParseNode.Parent.MatchParseCharInTemplateRelativeToProcessor);
            Prefix.Add(sb.ToString());
        }
    }
}
