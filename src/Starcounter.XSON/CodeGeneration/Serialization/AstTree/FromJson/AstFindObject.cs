
namespace Starcounter.Internal.Application.CodeGeneration {
    internal class AstFindObject : AstNode {
        internal override string DebugString {
            get {
                return "FindObject";
            }
        }
        
        internal override void GenerateCsCodeForNode() {
            Prefix.Add("while (*pBuffer != '{') {");
            Prefix.Add("    if (*pBuffer == '\\n' || *pBuffer == '\\r' || *pBuffer == '\\t' || *pBuffer == ' ') {");
            Prefix.Add("        leftBufferSize--;");                           
            Prefix.Add("        if (leftBufferSize < 0)");
            Prefix.Add("             JsonHelper.ThrowInvalidJsonException(\"Beginning of object not found ('{').\");");
            Prefix.Add("        pBuffer++;");
            Prefix.Add("    } else");
            Prefix.Add("        JsonHelper.ThrowInvalidJsonException(\"Unexpected character found, expected '{' but found '\" + (char)*pBuffer + \"'.\");");
            Prefix.Add("}");
        }
    }
}