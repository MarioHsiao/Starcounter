// ***********************************************************************
// <copyright file="AstProcessFail.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    /// <summary>
    /// Class AstProcessFail
    /// </summary>
    internal class AstProcessFail : AstNode {
        internal String Message { get; set; }

        internal override string DebugString {
            get {
                if (Message != null)
                    return "Fail: " + Message;
                return "Fail";
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            if (Message != null)
                Prefix.Add("throw new Exception(\"" + Message + "\");");
            else
                Prefix.Add("throw new Exception(\"Deserialization of App failed.\");");
        }

    }
}


