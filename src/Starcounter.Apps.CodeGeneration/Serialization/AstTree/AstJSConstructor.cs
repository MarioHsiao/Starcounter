// ***********************************************************************
// <copyright file="AstRpConstructor.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;
using Starcounter.Internal.Uri;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {

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
            RequestProcessorMetaData md;
            StringBuilder sb;
            AstJsonSerializerClass jsClass = (AstJsonSerializerClass)Parent;

            sb = new StringBuilder();
            sb.Append("public static byte[] VerificationBytes = new byte[] {");

            Prefix.Add("");
            comma = false;
            t = 0;
            
            for(int h = 0; h < ParseNode.AllHandlers.Count; h++){
                md = ParseNode.AllHandlers[h];
                Prefix.Add("public static int VerificationOffset" + h + " = " + t + "; // " + md.UnpreparedVerbAndUri);
                
                char c;
                string pvu = md.PreparedVerbAndUri;
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

            sb.Append("};");
            Prefix.Add(sb.ToString());
            Prefix.Add("public static IntPtr PointerVerificationBytes;");
            Prefix.Add(""); 

            Prefix.Add("static " + jsClass.ClassName + "() {");
            Prefix.Add("    PointerVerificationBytes = BitsAndBytes.Alloc(VerificationBytes.Length); // TODO. Free when program exists");
            Prefix.Add("    BitsAndBytes.SlowMemCopy( PointerVerificationBytes, VerificationBytes, (uint)VerificationBytes.Length);");
            Suffix.Add("}");
        }
    }
}
