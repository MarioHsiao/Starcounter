using System;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration {
    internal class AstGotoValue : AstNode {
        internal override string DebugString {
            get {
                return "GotoNextValue";
            }
        }
        
        internal override void GenerateCsCodeForNode() {
            Prefix.Add("// Skip until start of value to parse.");

            Prefix.Add("while (*pBuffer != ':') {");
            Prefix.Add("    leftBufferSize--;");
            Prefix.Add("    if (leftBufferSize < 0)");
            Prefix.Add("         JsonHelper.ThrowUnexpectedEndOfContentException();");
            Prefix.Add("    pBuffer++;");
            Prefix.Add("}");
            Prefix.Add("pBuffer++; // Skip ':' or ','");
            Prefix.Add("leftBufferSize--;");
            Prefix.Add("if (leftBufferSize < 0)");
            Prefix.Add("    JsonHelper.ThrowUnexpectedEndOfContentException();");
            Prefix.Add("while (*pBuffer == ' ' || *pBuffer == '\\n' || *pBuffer == '\\r') {");
            Prefix.Add("    leftBufferSize--;");
            Prefix.Add("    if (leftBufferSize < 0)");
            Prefix.Add("         JsonHelper.ThrowUnexpectedEndOfContentException();");
            Prefix.Add("    pBuffer++;");
            Prefix.Add("}");
        }
    }
}
