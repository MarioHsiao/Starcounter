// ***********************************************************************
// <copyright file="AstCase.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    internal class AstWriteObject : AstNode {
        internal override string DebugString {
            get {
                return "WriteObject";
            }
        }

        internal override bool IndentChildren {
            get {
                return false;
            }
        }

        internal override void GenerateCsCodeForNode() {
            Prefix.Add("if ((nextSize - 2) < 0)");
            Prefix.Add("    throw new Exception(\"Buffer too small.\");");
            Prefix.Add("nextSize -= 2;");

            Prefix.Add("*pfrag++ = (byte)'{';");
            Suffix.Add("*pfrag++ = (byte)'}';");
        }
    }
}
