
namespace Starcounter.Internal.Application.CodeGeneration {
    public class AstFixed : AstNode {
        internal override string DebugString {
            get {
                return "fixed(buffer)";
            }
        }

        internal override void GenerateCsCodeForNode() {
            Prefix.Add("fixed (byte* p = &apa[offset]) {");
            Prefix.Add("    byte* buf = p;");
            Suffix.Add("}");
        }
    }
}
