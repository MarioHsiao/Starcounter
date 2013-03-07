// ***********************************************************************
// <copyright file="AstCase.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    internal class AstWritePropertyName : AstNode {
        internal Template Template { get; set; }

        internal override string DebugString {
            get {
                return "WriteProperty(" + Template.Name + ")";
            }
        }

        internal override void GenerateCsCodeForNode() {
            Prefix.Add("valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, \"" + Template.Name + "\", tmpArr);");
            Prefix.Add("if (valSize == -1)");
            Prefix.Add("    throw new Exception(\"Buffer too small.\");");
            Prefix.Add("nextSize -= valSize;");
            Prefix.Add("pfrag += valSize;");
        }
    }
}
