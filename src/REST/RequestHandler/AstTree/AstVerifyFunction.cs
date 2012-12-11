// ***********************************************************************
// <copyright file="AstVerifier.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;

namespace Starcounter.Internal.Uri {
//    VerificationIndex = rpclass.PropertyName;

    /// <summary>
    /// 
    /// </summary>
    internal class AstVerifyFunction : AstNode {
        internal string VerificationName { get; set; }
        internal RequestProcessorMetaData Handler { get; set; }

        internal override string DebugString {
            get { return "bool Verify(...)"; }
        }

        internal override void GenerateCsCodeForNode() {
            Prefix.Add("private unsafe bool Verify(IntPtr uriStart, int uriSize) {");
            Prefix.Add("    byte* ptemplate = (byte*)(PointerVerificationBytes + " + VerificationName + "VerificationOffset);");
            Prefix.Add("    byte* puri = (byte*)uriStart;");
            Prefix.Add("    int nextSize = uriSize;");

            int count;
            if (Handler.ParameterTypes.Count > 0) {
                for (count = 0; count < Handler.PreparedVerbAndUri.Length; count++) {
                    if (Handler.PreparedVerbAndUri[count] == '@')
                        break;
                }
            } else {
                count = Handler.PreparedVerbAndUri.Length - 1;
            }

            GenVerifier(count);

            Suffix.Add("    return true;");
            Suffix.Add("}");
        }

        /// <summary>
        /// Generates code for verifying an uri.
        /// </summary>
        /// <param name="n">The number of bytes to verify</param>
        internal void GenVerifier(int n) {
            if (n > 0) {
                var compareBytes = n;
                while (compareBytes >= 8) {
                    Prefix.Add("    nextSize -= 8;");
                    Prefix.Add("    if (nextSize<0 || (*(UInt64*)puri) != (*(UInt64*)ptemplate) ) {");
                    Prefix.Add("         return false;");
                    Prefix.Add("    }");
                    Prefix.Add("    puri += 8;");
                    Prefix.Add("    ptemplate += 8;");
                    compareBytes -= 8;
                }
                if (compareBytes >= 4) {
                    Prefix.Add("    nextSize -= 4;");
                    Prefix.Add("    if (nextSize<0 || (*(UInt32*)puri) !=  (*(UInt32*)ptemplate) ) {");
                    Prefix.Add("        return false;");
                    Prefix.Add("    }");
                    Prefix.Add("    puri += 4;");
                    Prefix.Add("    ptemplate += 4;");
                    compareBytes -= 4;
                }
                if (compareBytes >= 2) {
                    Prefix.Add("    nextSize -= 2;");
                    Prefix.Add("    if (nextSize<0 || (*(UInt16*)puri) != (*(UInt16*)ptemplate) ) {");
                    Prefix.Add("        return false;");
                    Prefix.Add("    }");
                    Prefix.Add("    puri += 2;");
                    Prefix.Add("    ptemplate += 2;");
                    compareBytes -= 2;
                }
                if (compareBytes >= 1) {
                    Prefix.Add("    nextSize --;");
                    Prefix.Add("    if (nextSize<0 || (*puri) != (*ptemplate) ) {");
                    Prefix.Add("        return false;");
                    Prefix.Add("    }");
                }
            }
        }
    }
}