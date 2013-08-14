using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration {
    internal class AstJsonObjectArrayValue : AstNode {
        internal Template Template { get; set; }

        internal override string DebugString {
            get {
                return "foreach(item in " + Template.PropertyName + ") item.ToJson()";
            }
        }

        internal override void GenerateCsCodeForNode() {
            Prefix.Add("arr = obj." + Template.PropertyName + ";");
            Prefix.Add("if (bufferSize < (offset + arr.Count * 2 + 2))");
            Prefix.Add("    goto restart;");
            Prefix.Add("if (posInArray == -1) {");
            Prefix.Add("    offset++;");
            Prefix.Add("    *buf++ = (byte)'[';");
            Prefix.Add("    posInArray = 0;");
            Prefix.Add("}");
            Prefix.Add("for (int arrPos = posInArray; arrPos < arr.Count; arrPos++) {");
            Prefix.Add("    if (childObjArr == null) {");
            Prefix.Add("        valueSize = arr[arrPos].ToJsonUtf8(out childObjArr);");
            Prefix.Add("        if (valueSize == -1)");
            Prefix.Add("            goto restart;");
            Prefix.Add("        if (bufferSize < (offset + valueSize + 1))");
            Prefix.Add("            goto restart;");
            Prefix.Add("    }");
            Prefix.Add("    Buffer.BlockCopy(childObjArr, 0, apa, offset, valueSize);");
            Prefix.Add("    childObjArr = null;");
            Prefix.Add("    offset += valueSize;");
            Prefix.Add("    buf += valueSize;");
            Prefix.Add("    posInArray++;");
            Prefix.Add("    if ((arrPos + 1) < arr.Count) {");
            Prefix.Add("        offset++;");
            Prefix.Add("        *buf++ = (byte)',';");
            Prefix.Add("    }");
            Prefix.Add("}");
            Prefix.Add("offset++;");
            Prefix.Add("*buf++ = (byte)']';");
            Prefix.Add("posInArray = -1;");
        }
    }
}
