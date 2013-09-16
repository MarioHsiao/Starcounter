// ***********************************************************************
// <copyright file="AstProcessFail.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;

namespace Starcounter.Internal.Application.CodeGeneration {
    /// <summary>
    /// 
    /// </summary>
    internal class AstProcessFail : AstNode {
        internal override string DebugString {
            get {
                return "Fail";
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            Prefix.Add("throw ErrorCode.ToException(Starcounter.Internal.Error.SCERRUNSPECIFIED, \"char: '\" + (char)*pBuffer + \"', offset: \" + (bufferSize - leftBufferSize) + \"\");");
        }
    }
}


