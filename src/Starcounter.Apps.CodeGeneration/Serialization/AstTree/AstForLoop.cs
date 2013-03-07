// ***********************************************************************
// <copyright file="AstSwitch.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    internal class AstForLoop : AstNode {
        internal Template Template { get; set; }

        internal override string DebugString {
            get {
                return "For(...)";
            }
        }

        internal override void GenerateCsCodeForNode() {
            Prefix.Add("for(int i = 0; i < app." + Template.PropertyName + ".Count; i++) {");
            Prefix.Add("    var listApp = app." + Template.PropertyName + "[i];");
            Suffix.Add("    if ((i+1) < app." + Template.PropertyName + ".Count) {");
            Suffix.Add("        nextSize--;");
            Suffix.Add("        if (nextSize < 0)");
            Suffix.Add("            throw new Exception(\"Buffer too small.\");");
            Suffix.Add("        *pfrag++ = (byte)',';");
            Suffix.Add("    }");
            Suffix.Add("}");
        }
    }
}
