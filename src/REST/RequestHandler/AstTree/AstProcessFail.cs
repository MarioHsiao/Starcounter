// ***********************************************************************
// <copyright file="AstProcessFail.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
namespace Starcounter.Internal.Uri {
    /// <summary>
    /// Class AstProcessFail
    /// </summary>
    internal class AstProcessFail : AstNode {

        /// <summary>
        /// Gets the debug string.
        /// </summary>
        /// <value>The debug string.</value>
        internal override string DebugString {
            get {
                return "fail";
            }
        }


        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            Suffix.Add("handler = null;");
            Suffix.Add("resource = null;");
            Suffix.Add("return false;");
        }

    }
}


