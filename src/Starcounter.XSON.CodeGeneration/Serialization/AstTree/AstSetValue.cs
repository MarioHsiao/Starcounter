using System;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    internal class AstSetValue : AstNode {
        internal ParseNode ParseNode { get; set; }

        internal override string DebugString {
            get {
                return '"' + ParseNode.Handler.PreparedVerbAndUri + "\": <value>";
            }
        }

        internal override void GenerateCsCodeForNode() {
            Template template = (Template)ParseNode.Handler.Code;

            if (template is TObjArr) {
                Prefix.Add("app." + template.PropertyName + ".Add(val" + ParseNode.HandlerIndex + ");");
            } else {
                Prefix.Add("app." + template.PropertyName + " = val" + ParseNode.HandlerIndex + ";");
            }
        }
    }
}
