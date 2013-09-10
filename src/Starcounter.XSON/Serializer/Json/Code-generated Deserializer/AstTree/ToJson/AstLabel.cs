
namespace Starcounter.Internal.Application.CodeGeneration {
    public class AstLabel : AstNode {
        internal string Label { get; set; }

        internal override string DebugString {
            get {
                return "Label:" + Label;
            }
        }

        internal override void GenerateCsCodeForNode() {
            Prefix.Add(Label + ":");
        }
    }
}
