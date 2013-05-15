// ***********************************************************************
// <copyright file="AstProcessFunction.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.Internal.Application.CodeGeneration {
    internal class AstSerializeFunction : AstNode {
        internal override string DebugString {
            get {
                return "int Serialize(...)";
            }
        }

        internal override void GenerateCsCodeForNode() {
            Prefix.Add("");
            Prefix.Add("public override int Serialize(IntPtr buffer, int bufferSize, dynamic obj) {");
            Prefix.Add("    int valueSize;");
            Prefix.Add("    Obj childObj;");
            Suffix.Add("}");
        }
    }
}
