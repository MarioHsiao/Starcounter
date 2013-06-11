using System;

namespace Starcounter.Internal.Application.CodeGeneration {
    internal class AstCheckAlreadyProcessed : AstNode {
        internal int Index { get; set; }

        internal override string DebugString {
            get {
                return "if (!alreadyProcessed)";
            }
        }
        internal override void GenerateCsCodeForNode() {
            Prefix.Add("if (templateNo <= " + Index + ") {");
            Suffix.Add("    templateNo++;");
            Suffix.Add("    nameWritten = false;");
            Suffix.Add("}");
        }
    }
}
