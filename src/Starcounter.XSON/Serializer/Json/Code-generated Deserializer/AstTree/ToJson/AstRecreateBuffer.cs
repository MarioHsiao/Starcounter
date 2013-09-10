
namespace Starcounter.Internal.Application.CodeGeneration {
    public class AstRecreateBuffer : AstNode {
        internal override string DebugString {
            get {
                return "IncreaseBuffer()";
            }
        }

        internal override void GenerateCsCodeForNode() {
            Prefix.Add("if (recreateBuffer) {");
            Prefix.Add("    int oldSize = apa.Length;");
            Prefix.Add("    apa = IncreaseCapacity(apa, offset, valueSize);");
            Prefix.Add("    bufferSize = apa.Length;");
            Prefix.Add("}");
            Prefix.Add("recreateBuffer = true;");
        }
    }
}
