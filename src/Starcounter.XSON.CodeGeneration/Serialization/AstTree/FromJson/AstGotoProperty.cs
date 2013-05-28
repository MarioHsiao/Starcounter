using System;
using Starcounter.Templates;

// TODO: 
// This needs to be more generic. At the moment it only supports properties that are properly 
// formatted with starting and ending quotes.

namespace Starcounter.Internal.Application.CodeGeneration {
    internal class AstGotoProperty : AstNode {
        internal override string DebugString {
            get {
                return "GotoNextProperty...";
            }
        }

        internal override void GenerateCsCodeForNode() {
            Prefix.Add("// Skip until start of next property or end of current object.");
            Prefix.Add("while (true) {");
            Prefix.Add("    if (*pBuffer == '\"')");
            Prefix.Add("        break;");
            Prefix.Add("    if (*pBuffer == '}') {");
            Prefix.Add("        pBuffer++;");
            Prefix.Add("        leftBufferSize--;");
            Prefix.Add("        return (bufferSize - leftBufferSize);");
            Prefix.Add("    }");
            Prefix.Add("    pBuffer++;");
            Prefix.Add("    leftBufferSize--;");
            Prefix.Add("    if (leftBufferSize < 0)");
            Prefix.Add("         JsonHelper.ThrowUnexpectedEndOfContentException();");
            Prefix.Add("}");
            Prefix.Add("pBuffer++;");
            Prefix.Add("leftBufferSize--;");
            Prefix.Add("if (leftBufferSize < 0)");
            Prefix.Add("    JsonHelper.ThrowUnexpectedEndOfContentException();");
        }
    }
}
