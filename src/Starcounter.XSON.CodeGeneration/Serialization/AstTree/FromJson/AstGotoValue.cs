using System;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration {
    internal class AstGotoValue : AstNode {
        internal override string DebugString {
            get {
                if (IsValueArrayObject)
                    return "GotoNextValueInArray";
                return "GotoNextValue";
            }
        }
        
        internal bool IsValueArrayObject { get; set; }

        internal override void GenerateCsCodeForNode() {
            Prefix.Add("// Skip until start of value to parse.");

            if (IsValueArrayObject) {
                Prefix.Add("while (*pBuffer != ',') {");
                Prefix.Add("    if (*pBuffer == ']')");
                Prefix.Add("        break;");
            } else
                Prefix.Add("while (*pBuffer != ':') {");

            Prefix.Add("    pBuffer++;");
            Prefix.Add("    leftBufferSize--;");
            Prefix.Add("    if (leftBufferSize < 0)");
            Prefix.Add("         JsonHelper.ThrowUnexpectedEndOfContentException();");
            Prefix.Add("}");

            if (IsValueArrayObject){
                Prefix.Add("if (*pBuffer == ']')");
                Prefix.Add("    break;");
            }
            Prefix.Add("pBuffer++; // Skip ':' or ','");
            Prefix.Add("leftBufferSize--;");
            Prefix.Add("if (leftBufferSize < 0)");
            Prefix.Add("    JsonHelper.ThrowUnexpectedEndOfContentException();");
            Prefix.Add("while (*pBuffer == ' ' || *pBuffer == '\\n' || *pBuffer == '\\r') {");
            Prefix.Add("    pBuffer++;");
            Prefix.Add("    leftBufferSize--;");
            Prefix.Add("    if (leftBufferSize < 0)");
            Prefix.Add("         JsonHelper.ThrowUnexpectedEndOfContentException();");
            Prefix.Add("}");
        }
    }
}
