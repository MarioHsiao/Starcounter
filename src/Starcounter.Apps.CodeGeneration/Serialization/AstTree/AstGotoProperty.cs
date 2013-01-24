using System;
using Starcounter.Internal.Uri;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    internal class AstGotoProperty : AstNode {
        internal override string DebugString {
            get {
                return "GotoNextProperty...";
            }
        }

        internal override void GenerateCsCodeForNode() {
            Prefix.Add("while (true) {");
            Prefix.Add("    if (*pfrag == '\"')");
            Prefix.Add("        break;");
            Prefix.Add("    if (*pfrag == '}') {");
            Prefix.Add("        usedSize = bufferSize - nextSize;");
            Prefix.Add("        return app;");
            Prefix.Add("    }");
            Prefix.Add("    pfrag++;");
            Prefix.Add("    nextSize--;");
            Prefix.Add("    if (nextSize < 0)");
            Prefix.Add("         throw new Exception(\"Deserialization failed.\");");
            Prefix.Add("}");
            Prefix.Add("pfrag++;");
            Prefix.Add("nextSize--;");
            Prefix.Add("if (nextSize < 0)");
            Prefix.Add("    throw new Exception(\"Deserialization failed.\");");
        }
    }
}
