using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Internal.Application.CodeGeneration {
    public class AstReturn : AstNode {
        internal override string DebugString {
            get {
                return "Return success";
            }
        }

        internal override void GenerateCsCodeForNode() {
            Prefix.Add("buffer = apa;");
            Prefix.Add("return offset;");
        }
    }
}
