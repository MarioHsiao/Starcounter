
using System.Text;

namespace Starcounter.Internal.Uri {

    internal class AstRpConstructor : AstNode {

        internal ParseNode ParseNode { get; set; }

        /// <summary>
        /// Creates a short single line string representing this abstract syntax tree (AST) node
        /// </summary>
        internal override string DebugString {
            get {
                return "GeneratedRequestProcessor() {...}";
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            //            var sb = new StringBuilder();
            Prefix.Add("");
            var sb = new StringBuilder();
            sb.Append("public static byte[] VerificationBytes = new byte[] {");
            int t = 0;
            bool comma = false;
            foreach (var handler in ParseNode.AllHandlers) {
//                Prefix.Add("public int " + handler.AstClass.PropertyName + "VerificationOffset = " + t + ";");
                foreach (byte c in handler.PreparedVerbAndUri) {
                    if (c == '@')
                        break;
                    if (comma)
                        sb.Append(',');
                    sb.Append("(byte)'");
                    sb.Append((char)c);
                    sb.Append('\'');
                    t++;
                    comma = true;
                }
            }
            sb.Append("};");
            Prefix.Add(sb.ToString());
            Prefix.Add("public static IntPtr PointerVerificationBytes;");
            Prefix.Add("");
            foreach (var handler in ParseNode.AllHandlers) {
                sb = new StringBuilder();
                sb.Append("public static ");
                if (handler.AstClass == null) {
                    sb.Append("throw new Exception(\"");
                    sb.Append(handler.PreparedVerbAndUri);
                    sb.Append("\");");
                }
                else {
                    sb.Append(handler.AstClass.ClassName);
                    sb.Append(' ');
                    sb.Append(handler.AstClass.PropertyName);
                    sb.Append(" = new ");
                    sb.Append(handler.AstClass.ClassName);
                    sb.Append("();");
                }
                Prefix.Add(sb.ToString());
            }
            Prefix.Add("");
            Prefix.Add("public GeneratedRequestProcessor() {");
            foreach (var handler in ParseNode.AllHandlers) {
                sb = new StringBuilder();
                if (handler.AstClass == null) {
                    sb.Append("throw new Exception(\"");
                    sb.Append(handler.PreparedVerbAndUri);
                    sb.Append("\");");
                }
                else {
                    sb.Append("    Registrations[\"");
                    sb.Append(handler.PreparedVerbAndUri);
                    sb.Append("\"] = ");
                    //                if (handler.AstClass != null)
                    sb.Append(handler.AstClass.PropertyName);
                    sb.Append(";");
                }
                Prefix.Add(sb.ToString());
            }
            Prefix.Add("    PointerVerificationBytes = BitsAndBytes.Alloc(VerificationBytes.Length); // TODO. Free when program exists");
            Prefix.Add("    BitsAndBytes.SlowMemCopy( PointerVerificationBytes, VerificationBytes, (uint)VerificationBytes.Length);");
            Suffix.Add("}");
        }
    }
}
