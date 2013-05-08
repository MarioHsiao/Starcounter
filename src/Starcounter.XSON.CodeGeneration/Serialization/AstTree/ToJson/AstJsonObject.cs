// ***********************************************************************
// <copyright file="AstCase.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration {
    internal class AstJsonObject : AstNode {
        internal override string DebugString {
            get {
                return "Object";
            }
        }

        internal override bool IndentChildren {
            get {
                return false;
            }
        }

        internal override void GenerateCsCodeForNode() {
            Prefix.Add("if ((leftBufferSize - 2) < 0)");
            Prefix.Add("    throw ErrorCode.ToException(Starcounter.Error.SCERRUNSPECIFIED);");
            Prefix.Add("leftBufferSize -= 2;");

            Prefix.Add("*pBuffer++ = (byte)'{';");
            Suffix.Add("*pBuffer++ = (byte)'}';");
        }
    }
}
