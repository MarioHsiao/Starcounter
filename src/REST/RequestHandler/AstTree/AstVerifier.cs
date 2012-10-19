﻿// ***********************************************************************
// <copyright file="AstVerifier.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;

namespace Starcounter.Internal.Uri {
    /// <summary>
    /// Class AstVerifier
    /// </summary>
    internal abstract class AstVerifier : AstNode {

        /// <summary>
        /// Gens the verifier.
        /// </summary>
        /// <param name="n">The n.</param>
        /// <param name="breakInsteadOfReturn">if set to <c>true</c> [break instead of return].</param>
        internal void GenVerifier(int n, bool breakInsteadOfReturn) {
            if (n > 0) {
                var compareBytes = n;
                while (compareBytes >= 8) {
                    Prefix.Add("nextSize -= 8;");
                    Prefix.Add("if (nextSize<0 || (*(UInt64*)pfrag) != (*(UInt64*)pfrag) ) {");
                    AddBreakOrReturn(breakInsteadOfReturn);
                    Prefix.Add("pfrag += 8;");
                    Prefix.Add("ptempl += 8;");
                    compareBytes -= 8;
                }
                if (compareBytes >= 4) {
                    Prefix.Add("nextSize -= 4;");
                    Prefix.Add("if (nextSize<0 || (*(UInt32*)pfrag) !=  (*(UInt32*)pfrag) ) {");
                    AddBreakOrReturn(breakInsteadOfReturn);
                    Prefix.Add("pfrag += 4;");
                    Prefix.Add("ptempl += 4;");
                    compareBytes -= 4;
                }
                if (compareBytes >= 2) {
                    Prefix.Add("nextSize -= 2;");
                    Prefix.Add("if (nextSize<0 || (*(UInt16*)pfrag) != (*(UInt16*)pfrag) ) {");
                    AddBreakOrReturn(breakInsteadOfReturn);
                    Prefix.Add("pfrag += 2;");
                    Prefix.Add("ptempl += 2;");
                    compareBytes -= 2;
                }
//                if (verifyLastByte) {
                    // Not needed if switch/case on byte has been performed
                    if (compareBytes >= 1) {
                        Prefix.Add("nextSize --;");
                        Prefix.Add("if (nextSize<0 || (*pfrag) != (*pfrag) ) {");
                        AddBreakOrReturn(breakInsteadOfReturn);
                        Prefix.Add("pfrag ++;");
                        Prefix.Add("ptempl ++;");
                    }
//                }
            }
        }

        /// <summary>
        /// Adds the break or return.
        /// </summary>
        /// <param name="doBreak">if set to <c>true</c> [do break].</param>
        void AddBreakOrReturn(bool doBreak) {
            if (doBreak) {
                Prefix.Add("    break;");
            }
            else {
                Prefix.Add("    handler = null;");
                Prefix.Add("    resource = null;");
                Prefix.Add("    return false;");
            }
            Prefix.Add("}");
        }

    }
}