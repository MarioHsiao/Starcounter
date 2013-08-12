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
                return "int ToJson(...)";
            }
        }

        internal override void GenerateCsCodeForNode() {
            Prefix.Add("");
            Prefix.Add("public override int ToJsonUtf8(Obj realObj, out byte[] buffer) {");
            Prefix.Add("    Arr arr;");
            Prefix.Add("    bool nameWritten = false;");
            Prefix.Add("    bool recreateBuffer = false;");
            Prefix.Add("    byte[] childObjArr = null;");
            Prefix.Add("    dynamic obj = realObj;");
            Prefix.Add("    int templateNo = 0;");
            Prefix.Add("    int posInArray = -1;");
            Prefix.Add("    int valueSize = -1;");
            Prefix.Add("    int bufferSize = 512;");
            Prefix.Add("    Obj childObj;");
            Prefix.Add("    Template tProperty;");
            Prefix.Add("    TObj tObj = realObj.Template;");
            Prefix.Add("    byte[] apa = new byte[bufferSize];");
            Prefix.Add("    int offset = 1;");

            Prefix.Add("    apa[0] = (byte)'{';");
            Prefix.Add("    unsafe {");

            Suffix.Add("    }");
            Suffix.Add("}");
        }
    }
}
