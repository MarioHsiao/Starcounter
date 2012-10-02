

using System.Text;
namespace Starcounter.Internal.Uri {
    internal class AstUnsafe : AstNode {

       // internal string VerificationIndex;

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            Prefix.Add("unsafe {");
            Prefix.Add("   byte* pfrag = (byte*)fragment;");
            Prefix.Add("   byte* ptempl = (byte*)PointerVerificationBytes;");
            var sb = new StringBuilder();
       //     if (VerificationIndex != null) {
       //         sb.Append("   ptempl += ");
       //         sb.Append(VerificationIndex);
       //         sb.Append(';');
       //         Prefix.Add(sb.ToString());
       //     }
            Prefix.Add("   int nextSize = size;");
            Suffix.Add("}");
        }

    }
}
