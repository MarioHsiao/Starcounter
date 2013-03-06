using System.Text;
using Starcounter.Internal.Uri;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {

    internal abstract class AstVerifier : AstNode {
        /// <summary>
        /// Generates code for verifying an uri.
        /// </summary>
        /// <param name="pn"></param>
        /// <param name="breakInsteadOfReturn">
        /// if set to true an break statement will be generated instead of return.
        /// </param>
        internal void GenVerifier(ParseNode pn, bool breakInsteadOfReturn) {
            int numberToVerify;
            int verifyOffset;

            if (pn.Parent == null)
                return;

            numberToVerify = pn.Parent.MatchChildrenAt - 1;
            verifyOffset = pn.Parent.Parent.MatchChildrenAtAbsolute + 1;

            if (pn.Parent.Parent == null) {
                numberToVerify++;
                verifyOffset--;
            }

            if (numberToVerify > 0) {
                var compareBytes = numberToVerify;

                int handlerIndex = FindHandlerIndex(pn);
                if (handlerIndex != -1) {
                    Prefix.Add("pver = ((byte*)PointerVerificationBytes + VerificationOffset" + handlerIndex + " + " + verifyOffset + ");");
                }

                while (compareBytes >= 8) {
                    Prefix.Add("nextSize -= 8;");
                    Prefix.Add("if (nextSize<0 || (*(UInt64*)pfrag) != (*(UInt64*)pver) ) {");
                    AddBreakOrReturn(breakInsteadOfReturn);
                    Prefix.Add("pfrag += 8;");
                    Prefix.Add("pver += 8;");
                    compareBytes -= 8;
                }
                if (compareBytes >= 4) {
                    Prefix.Add("nextSize -= 4;");
                    Prefix.Add("if (nextSize<0 || (*(UInt32*)pfrag) !=  (*(UInt32*)pver) ) {");
                    AddBreakOrReturn(breakInsteadOfReturn);
                    Prefix.Add("pfrag += 4;");
                    Prefix.Add("pver += 4;");
                    compareBytes -= 4;
                }
                if (compareBytes >= 2) {
                    Prefix.Add("nextSize -= 2;");
                    Prefix.Add("if (nextSize<0 || (*(UInt16*)pfrag) != (*(UInt16*)pver) ) {");
                    AddBreakOrReturn(breakInsteadOfReturn);
                    Prefix.Add("pfrag += 2;");
                    Prefix.Add("pver += 2;");
                    compareBytes -= 2;
                }
                if (compareBytes >= 1) {
                    Prefix.Add("nextSize --;");
                    Prefix.Add("if (nextSize<0 || (*pfrag) != (*pver) ) {");
                    AddBreakOrReturn(breakInsteadOfReturn);
                    Prefix.Add("pfrag++;");
                    Prefix.Add("pver++;");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="doBreak"></param>
        private void AddBreakOrReturn(bool doBreak) {
            //if (doBreak) {
            //    Prefix.Add("    break;");
            //} else {
            //    Prefix.Add("    handler = null;");
            //    Prefix.Add("    resource = null;");
            //    Prefix.Add("    return false;");
            //}
            Prefix.Add("    throw new Exception(\"Deserialization failed. Verification failed.\");");
            Prefix.Add("}");
        }

        private int FindHandlerIndex(ParseNode node) {
            int handlerIndex = -1;

            if (node.HandlerIndex != -1) {
                handlerIndex = node.HandlerIndex;
            } else {
                foreach (ParseNode candidate in node.Candidates) {
                    handlerIndex = FindHandlerIndex(candidate);
                    if (handlerIndex != -1)
                        break;
                }
            }
            return handlerIndex;
        }
    }
}
