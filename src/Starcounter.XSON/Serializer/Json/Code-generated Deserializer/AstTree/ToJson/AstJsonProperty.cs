// ***********************************************************************
// <copyright file="AstCase.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration {
    internal class AstJsonProperty : AstNode {
        internal Template Template { get; set; }

        internal override string DebugString {
            get {
                return "Property(" + Template.TemplateName + ")";
            }
        }

        internal override void GenerateCsCodeForNode() {
            Prefix.Add("if (!nameWritten) {");
            Prefix.Add("    valueSize = JsonHelper.WriteString((IntPtr)buf, bufferSize - offset, \"" + Template.TemplateName + "\");");
            Prefix.Add("    if (valueSize == -1)");
            Prefix.Add("        goto restart;");
            Prefix.Add("    if (bufferSize < (offset + valueSize + 1))");
            Prefix.Add("        goto restart;");
            Prefix.Add("    nameWritten = true;");
            Prefix.Add("    offset += valueSize + 1;");
            Prefix.Add("    buf += valueSize;");
            Prefix.Add("    *buf++ = (byte)':';");
            Prefix.Add("}");
        }
    }
}
