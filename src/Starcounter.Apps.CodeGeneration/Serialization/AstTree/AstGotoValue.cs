using System;
using Starcounter.Internal.Uri;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    internal class AstGotoValue : AstNode {
        internal override string DebugString {
            get {
                return "GotoNextValue...";
            }
        }

        internal override void GenerateCsCodeForNode() {
            Prefix.Add("pfrag++;");
            Prefix.Add("while (*pfrag == ' ') {");
            Prefix.Add("    pfrag++;");
            Prefix.Add("    nextSize--;");
            Prefix.Add("    if (nextSize < 0)");
            Prefix.Add("         throw new Exception(\"Deserialization failed.\");");
            Prefix.Add("}");
            Prefix.Add("pfrag++;");
            Prefix.Add("nextSize--;");
            Prefix.Add("if (nextSize < 0)");
            Prefix.Add("    throw new Exception(\"Deserialization failed.\");");
            Prefix.Add("while (*pfrag == ' ') {");
            Prefix.Add("    pfrag++;");
            Prefix.Add("    nextSize--;");
            Prefix.Add("    if (nextSize < 0)");
            Prefix.Add("         throw new Exception(\"Deserialization failed.\");");
            Prefix.Add("}");
        }
    }
}
