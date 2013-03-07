using System;
using Starcounter.Templates;

// TODO: 
// This needs to be more generic. At the moment it only supports properties that are properly 
// formatted with starting and ending quotes.

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    internal class AstGotoProperty : AstNode {
        internal override string DebugString {
            get {
                return "GotoNextProperty...";
            }
        }

        internal override void GenerateCsCodeForNode() {
            Prefix.Add("// Skip until start of next property or end of current object.");
            Prefix.Add("while (true) {");
            Prefix.Add("    if (*pfrag == '\"')");
            Prefix.Add("        break;");
            Prefix.Add("    if (*pfrag == '}') {");
            Prefix.Add("        pfrag++;");
            Prefix.Add("        nextSize--;");
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
