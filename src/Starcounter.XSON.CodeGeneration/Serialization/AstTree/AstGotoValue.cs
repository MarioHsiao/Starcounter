using System;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
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
                Prefix.Add("while (*pfrag != ',') {");
                Prefix.Add("    if (*pfrag == ']')");
                Prefix.Add("        break;");
            } else
                Prefix.Add("while (*pfrag != ':') {");

            Prefix.Add("    pfrag++;");
            Prefix.Add("    nextSize--;");
            Prefix.Add("    if (nextSize < 0)");
            Prefix.Add("         throw new Exception(\"Deserialization failed.\");");
            Prefix.Add("}");

            if (IsValueArrayObject){
                Prefix.Add("if (*pfrag == ']')");
                Prefix.Add("    break;");
            }
            Prefix.Add("pfrag++; // Skip ':' or ','");
            Prefix.Add("nextSize--;");
            Prefix.Add("if (nextSize < 0)");
            Prefix.Add("    throw new Exception(\"Deserialization failed.\");");
            Prefix.Add("while (*pfrag == ' ' || *pfrag == '\\n' || *pfrag == '\\r') {");
            Prefix.Add("    pfrag++;");
            Prefix.Add("    nextSize--;");
            Prefix.Add("    if (nextSize < 0)");
            Prefix.Add("         throw new Exception(\"Deserialization failed.\");");
            Prefix.Add("}");
        }
    }
}
