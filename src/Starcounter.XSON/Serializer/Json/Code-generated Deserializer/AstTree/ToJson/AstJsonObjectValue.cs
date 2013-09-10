using System;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration {
    internal class AstJsonObjectValue : AstNode {
        internal Template Template { get; set; }

        internal override string DebugString {
            get {
                return Template.PropertyName + ".ToJson()";
            }
        }

        internal override void GenerateCsCodeForNode() {
            Prefix.Add("if (childObjArr == null) {");
            Prefix.Add("    childObj = obj." + Template.PropertyName + ";");
            Prefix.Add("    if (childObj != null) {");
            Prefix.Add("        valueSize = childObj.ToJsonUtf8(out childObjArr);");
            Prefix.Add("    } else {");
            Prefix.Add("        valueSize = JsonHelper.WriteNull((IntPtr)buf, bufferSize - offset);");
            Prefix.Add("        if (valueSize == -1)");
            Prefix.Add("            goto restart;");
            Prefix.Add("    }");
            Prefix.Add("}");
            Prefix.Add("if (valueSize != -1 && childObjArr != null) {");
            Prefix.Add("    if (bufferSize < (offset + valueSize + 1))");
            Prefix.Add("        goto restart;");
            Prefix.Add("    Buffer.BlockCopy(childObjArr, 0, apa, offset, valueSize);");
            Prefix.Add("    offset += valueSize;");
            Prefix.Add("    buf += valueSize;");
            Prefix.Add("    childObjArr = null;");
            Prefix.Add("} else");
            Prefix.Add("    goto restart;");
        }
    }
}



//                            if (childObjArr == null) {
//                                childObj = obj.Get((TObj)tProperty);
//                                if (childObj != null) {
//                                    valueSize = childObj.ToJsonUtf8(out childObjArr);
//                                } else {
//                                    valueSize = JsonHelper.WriteNull((IntPtr)pfrag, buf.Length - offset);
//                                    if (valueSize == -1)
//                                        goto restart;
//                                }
//                            }

//                            if (valueSize != -1 && childObjArr != null) {
//                                if (buf.Length < (offset + valueSize))
//                                    goto restart;
//                                Buffer.BlockCopy(childObjArr, 0, buf, offset, valueSize);
//                                pfrag += valueSize;
//                                offset += valueSize;
//                                childObjArr = null;
//                            } else
//                                goto restart;
