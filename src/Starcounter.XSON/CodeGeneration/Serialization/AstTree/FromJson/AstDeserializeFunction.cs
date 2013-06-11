// ***********************************************************************
// <copyright file="AstProcessFunction.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.Internal.Application.CodeGeneration {
    /// <summary>
    /// Class AstProcessFunction
    /// </summary>
    internal class AstDeserializeFunction : AstNode {
        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        internal override string DebugString {
            get {
                return "int PopulateFromJson(...)";
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            Prefix.Add("public override int PopulateFromJson(Obj realObj, IntPtr buffer, int bufferSize) {");
            Prefix.Add("    int valueSize;");
            Prefix.Add("    dynamic obj = realObj;");
            Suffix.Add("}");
        }
    }
}
