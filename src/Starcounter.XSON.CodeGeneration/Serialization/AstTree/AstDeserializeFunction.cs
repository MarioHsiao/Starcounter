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
                return "int Populate(...)";
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            Prefix.Add("public override int PopulateFromJson(IntPtr buffer, int bufferSize, dynamic obj) {");
            Prefix.Add("    int valueSize;");
            Suffix.Add("}");
        }
    }
}
