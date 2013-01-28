// ***********************************************************************
// <copyright file="AstProcessFunction.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    internal class AstSerializeFunction : AstNode {
        internal override string DebugString {
            get {
                return "bool Serialize(" + AstTreeHelper.GetSerializerClass(this).FullAppClassName + ")";
            }
        }

        internal override void GenerateCsCodeForNode() {
            string fullClassName = AstTreeHelper.GetSerializerClass(this).FullAppClassName;

            Prefix.Add("");
            Prefix.Add("public static int Serialize(IntPtr buffer, int bufferSize, " + fullClassName + " app) {");
            Prefix.Add("    byte[] tmpArr = new byte[1024];");
            Prefix.Add("    int valSize;");
            Suffix.Add("}");
        }
    }
}
