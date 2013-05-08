// ***********************************************************************
// <copyright file="AstCase.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration {
    internal class AstJsonDelimiter : AstNode {
        internal char Delimiter { get; set; }

        internal override string DebugString {
            get {
                return "Delimiter(" + Delimiter + ")";
            }
        }

        internal override void GenerateCsCodeForNode() {
            Prefix.Add("leftBufferSize--;");
            Prefix.Add("if (leftBufferSize < 0)");
            Prefix.Add("    throw ErrorCode.ToException(Starcounter.Error.SCERRUNSPECIFIED);");
            Prefix.Add("*pBuffer++ = (byte)'" + Delimiter + "';");
        }
    }
}
