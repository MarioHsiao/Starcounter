// ***********************************************************************
// <copyright file="AstProcessFail.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;

namespace Starcounter.Internal.Application.CodeGeneration {
    /// <summary>
    /// Class AstProcessFail
    /// </summary>
    internal class AstProcessFail : AstNode {
        internal String ExceptionCode { get; set; }

        internal override string DebugString {
            get {
                if (ExceptionCode != null)
                    return "Fail: " + ExceptionCode;
                return "Fail";
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            if (ExceptionCode != null)
                Prefix.Add("throw " + ExceptionCode);
            else
                Prefix.Add("throw new Exception(\"Deserialization of App failed.\");");
        }

    }
}


