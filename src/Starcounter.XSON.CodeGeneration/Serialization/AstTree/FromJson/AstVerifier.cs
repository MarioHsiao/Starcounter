using System.Text;

namespace Starcounter.Internal.Application.CodeGeneration {

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

            numberToVerify = pn.MatchCharInTemplateRelative - 1;
            verifyOffset = pn.Parent.MatchCharInTemplateAbsolute + 1;

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
                    Prefix.Add("leftBufferSize -= 8;");
                    Prefix.Add("if (leftBufferSize < 0 || (*(UInt64*)pBuffer) != (*(UInt64*)pver) ) {");
                    AddBreakOrReturn(breakInsteadOfReturn);
                    Prefix.Add("pBuffer += 8;");
                    Prefix.Add("pver += 8;");
                    compareBytes -= 8;
                }
                if (compareBytes >= 4) {
                    Prefix.Add("leftBufferSize -= 4;");
                    Prefix.Add("if (leftBufferSize < 0 || (*(UInt32*)pBuffer) !=  (*(UInt32*)pver) ) {");
                    AddBreakOrReturn(breakInsteadOfReturn);
                    Prefix.Add("pBuffer += 4;");
                    Prefix.Add("pver += 4;");
                    compareBytes -= 4;
                }
                if (compareBytes >= 2) {
                    Prefix.Add("leftBufferSize -= 2;");
                    Prefix.Add("if (leftBufferSize < 0 || (*(UInt16*)pBuffer) != (*(UInt16*)pver) ) {");
                    AddBreakOrReturn(breakInsteadOfReturn);
                    Prefix.Add("pBuffer += 2;");
                    Prefix.Add("pver += 2;");
                    compareBytes -= 2;
                }
                if (compareBytes >= 1) {
                    Prefix.Add("leftBufferSize --;");
                    Prefix.Add("if (leftBufferSize < 0 || (*pBuffer) != (*pver) ) {");
                    AddBreakOrReturn(breakInsteadOfReturn);
                    Prefix.Add("pBuffer++;");
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
            int templateIndex = -1;

            if (node.TemplateIndex != -1) {
                templateIndex = node.TemplateIndex;
            } else {
                foreach (ParseNode candidate in node.Candidates) {
                    templateIndex = FindHandlerIndex(candidate);
                    if (templateIndex != -1)
                        break;
                }
            }
            return templateIndex;
        }
    }
}
