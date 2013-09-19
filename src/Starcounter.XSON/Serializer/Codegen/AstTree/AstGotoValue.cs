using System;
using Starcounter.Templates;

namespace Starcounter.XSON.Serializer.Ast {
    internal class AstGotoValue : AstBase {
        internal override string DebugString {
            get {
                return "GotoNextValue";
            }
        }
    }
}
