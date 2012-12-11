// ***********************************************************************
// <copyright file="AstVerifier.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;

namespace Starcounter.Internal.Uri {
    /// <summary>
    /// 
    /// </summary>
    internal abstract class AstJump : AstNode {

        /// <summary>
        /// Generates code for verifying an uri.
        /// </summary>
        /// <param name="n">The number of bytes to jump</param>
        /// <param name="breakInsteadOfReturn">
        /// if set to true an break statement will be generated instead of return.
        /// </param>
        internal void GenerateJump(int n, bool breakInsteadOfReturn) {
            if (n > 0) {
                Prefix.Add("nextSize -= " + n + ";");
                Prefix.Add("if (nextSize < 0) {");
                AddBreakOrReturn(breakInsteadOfReturn);
                Prefix.Add("pfrag += " + n  + ";");
            }
        }

        /// <summary>
        /// Adds the break or return.
        /// </summary>
        /// <param name="doBreak">if set to <c>true</c> [do break].</param>
        void AddBreakOrReturn(bool doBreak) {
            if (doBreak) {
                Prefix.Add("    break;");
            } else {
                Prefix.Add("    handler = null;");
                Prefix.Add("    resource = null;");
                Prefix.Add("    return false;");
            }
            Prefix.Add("}");
        }

    }
}