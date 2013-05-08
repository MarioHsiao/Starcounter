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
        /// Gets the debug string.
        /// </summary>
        /// <value>The debug string.</value>
        internal override string DebugString {
            get {
                return "TODO!";
//                AstTreeHelper.GetSerializerClass(this).FullAppClassName + " Deserialize()";
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            string appClassName = "TODO!";
//            AstTreeHelper.GetSerializerClass(this).FullAppClassName;
            Prefix.Add("public static " + appClassName + " Deserialize(IntPtr buffer, int bufferSize, out int usedSize) {");
            Prefix.Add("    int valueSize;");
            Prefix.Add("    " + appClassName + " app = new " + appClassName + "();");
            Suffix.Add("}");
        }
    }
}
