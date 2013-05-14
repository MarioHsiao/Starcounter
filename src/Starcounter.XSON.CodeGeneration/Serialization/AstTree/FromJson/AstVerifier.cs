using System.Text;

namespace Starcounter.Internal.Application.CodeGeneration {

    internal class AstVerifier : AstNode {

        internal ParseNode ParseNode { get; set; }

        internal override void GenerateCsCodeForNode() {
            GenVerifier(ParseNode);
        }

        internal override string DebugString {
            get {
                if (ShouldVerify)
                    return "Verify(" + VerifyOffset + ", " + VerifyCount + ")";
                return "<NoVerificationNeeded>";
            }
        }

        private bool ShouldVerify {
            get {
                if (ParseNode.Parent == null || ParseNode.Parent.Match == TemplateMetadata.END_OF_PROPERTY)
                    return false;
                return true; 
            }
        }

        private int VerifyOffset {
            get {
                if (ParseNode.Parent.Parent == null) {
                    // This is the toplevel. We are in no switch and cannot skip any characters for verifying.
                    return ParseNode.Parent.MatchCharInTemplateAbsolute;
                }
                // We have already switched on the first character, and we can skip it when verifying.
                return ParseNode.Parent.MatchCharInTemplateAbsolute + 1; 
            }
        }

        private int VerifyCount {
            get {
                if (ParseNode.DetectedType == NodeType.Heureka) {
                    // Verify the last bit of the property. We want the last 
                    // character as well (the one that is switched in other cases).
                    return ParseNode.MatchCharInTemplateAbsolute - VerifyOffset; 
                }

                // Not in the end. We skip the last character since we will be switching
                // on that in the next step.
                return (ParseNode.MatchCharInTemplateAbsolute - 1) - VerifyOffset; 
            }
        }

        /// <summary>
        /// Generates code for verifying an uri.
        /// </summary>
        /// <param name="pn"></param>
        /// <param name="breakInsteadOfReturn">
        /// if set to true an break statement will be generated instead of return.
        /// </param>
        internal void GenVerifier(ParseNode pn) {
            int numberToVerify;
            int verifyOffset;

            if (!ShouldVerify)
                return;

            verifyOffset = VerifyOffset;
            numberToVerify = VerifyCount;

            if (numberToVerify > 0) {
                var compareBytes = numberToVerify;

                int handlerIndex = FindTemplateIndex(pn);
                if (handlerIndex != -1) {
                    Prefix.Add("pver = ((byte*)PointerVerificationBytes + VerificationOffset" + handlerIndex + " + " + verifyOffset + ");");
                }

                while (compareBytes >= 8) {
                    Prefix.Add("leftBufferSize -= 8;");
                    Prefix.Add("if (leftBufferSize < 0 || (*(UInt64*)pBuffer) != (*(UInt64*)pver) )");
                    AddException();
                    Prefix.Add("pBuffer += 8;");
                    Prefix.Add("pver += 8;");
                    compareBytes -= 8;
                }
                if (compareBytes >= 4) {
                    Prefix.Add("leftBufferSize -= 4;");
                    Prefix.Add("if (leftBufferSize < 0 || (*(UInt32*)pBuffer) !=  (*(UInt32*)pver) )");
                    AddException();
                    Prefix.Add("pBuffer += 4;");
                    Prefix.Add("pver += 4;");
                    compareBytes -= 4;
                }
                if (compareBytes >= 2) {
                    Prefix.Add("leftBufferSize -= 2;");
                    Prefix.Add("if (leftBufferSize < 0 || (*(UInt16*)pBuffer) != (*(UInt16*)pver) )");
                    AddException();
                    Prefix.Add("pBuffer += 2;");
                    Prefix.Add("pver += 2;");
                    compareBytes -= 2;
                }
                if (compareBytes >= 1) {
                    Prefix.Add("leftBufferSize --;");
                    Prefix.Add("if (leftBufferSize < 0 || (*pBuffer) != (*pver) )");
                    AddException();
                    Prefix.Add("pBuffer++;");
                    Prefix.Add("pver++;");
                }

                Prefix.Add("leftBufferSize--;");
                Prefix.Add("pBuffer++;");
            }
        }

        private void AddException() {
            Prefix.Add("    throw ErrorCode.ToException(Starcounter.Internal.Error.SCERRUNSPECIFIED);");
        }

        private int FindTemplateIndex(ParseNode node) {
            int templateIndex = -1;

            if (node.TemplateIndex != -1) {
                templateIndex = node.TemplateIndex;
            } else {
                foreach (ParseNode candidate in node.Candidates) {
                    templateIndex = FindTemplateIndex(candidate);
                    if (templateIndex != -1)
                        break;
                }
            }
            return templateIndex;
        }
    }
}
