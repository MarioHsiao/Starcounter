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
            Prefix.Add("valueSize = JsonHelper.WriteString((IntPtr)pBuffer, leftBufferSize, \"" + Template.TemplateName + "\");");
            Prefix.Add("if (valueSize == -1)");
            Prefix.Add("    throw ErrorCode.ToException(Starcounter.Internal.Error.SCERRUNSPECIFIED);");
            Prefix.Add("leftBufferSize -= valueSize;");
            Prefix.Add("pBuffer += valueSize;");
        }
    }
}
