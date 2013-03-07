// ***********************************************************************
// <copyright file="AstCase.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    internal class AstWriteDelimiter : AstNode {
        internal char Delimiter { get; set; }

        internal override string DebugString {
            get {
                return "WriteDelimiter(" + Delimiter + ")";
            }
        }

        internal override void GenerateCsCodeForNode() {
            Prefix.Add("nextSize--;");
            Prefix.Add("if (nextSize < 0)");
            Prefix.Add("    throw new Exception(\"Buffer too small.\");");
            Prefix.Add("*pfrag++ = (byte)'" + Delimiter + "';");
        }
    }
}
