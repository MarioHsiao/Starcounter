// ***********************************************************************
// <copyright file="AstRpConstructor.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;

namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// Class AstRpConstructor
    /// </summary>
    internal class AstJSConstructor : AstNode {

        /// <summary>
        /// Gets or sets the parse node.
        /// </summary>
        /// <value>The parse node.</value>
        internal ParseNode ParseNode { get; set; }

        /// <summary>
        /// Creates a short single line string representing this abstract syntax tree (AST) node
        /// </summary>
        /// <value>The debug string.</value>
        internal override string DebugString {
            get {

                return "static " + ((AstJsonSerializerClass)Parent).ClassName + "() {...}";
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            bool comma;
            int t;
            TemplateMetadata md;
            StringBuilder sb;
            AstJsonSerializerClass jsClass = (AstJsonSerializerClass)Parent;

            sb = new StringBuilder();
            sb.Append("private static byte[] VerificationBytes = new byte[] {");

            Prefix.Add("");
            comma = false;
            t = 0;

            Prefix.Add("#pragma warning disable 0414");
            for(int h = 0; h < ParseNode.AllTemplates.Count; h++){
                md = ParseNode.AllTemplates[h];
                Prefix.Add("private static int VerificationOffset" + h + " = " + t + "; // " + md.TemplateName);
                
                char c;
                string pvu = md.TemplateName;
                for (int i = 0; i < pvu.Length; i++) {
                    c = pvu[i];

                    if (comma)
                        sb.Append(',');
                    sb.Append("(byte)'");
                    sb.Append(c);
                    sb.Append('\'');
                    t++;
                    comma = true;
                }
            }
            Prefix.Add("#pragma warning restore 0414");

            sb.Append("};");
            Prefix.Add(sb.ToString());
            Prefix.Add("private static IntPtr PointerVerificationBytes;");
            Prefix.Add(""); 

            Prefix.Add("public " + jsClass.ClassName + "() {");
            Prefix.Add("    PointerVerificationBytes = Marshal.AllocHGlobal(VerificationBytes.Length); // TODO. Free when program exists");
            Prefix.Add("    Marshal.Copy(VerificationBytes, 0, PointerVerificationBytes, VerificationBytes.Length);");
            Suffix.Add("}");
        }
    }
}
