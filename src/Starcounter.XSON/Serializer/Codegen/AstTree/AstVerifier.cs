using System.Text;
using Starcounter.XSON.Serializer.Parsetree;

namespace Starcounter.XSON.Serializer.Ast {
    internal class AstVerifier : AstBase {
        internal ParseNode ParseNode { get; set; }

        internal override string DebugString {
            get {
                if (ShouldVerify)
                    return "Verify(" + VerifyStart + ", " + VerifyEnd + ", count: " + VerifyCount + ")";
                return "<NoVerificationNeeded>";
            }
        }

        internal bool ShouldVerify {
            get {
                if (ParseNode.Parent == null || ParseNode.Parent.Match == TemplateMetadata.END_OF_PROPERTY)
                    return false;
                return true; 
            }
        }

        internal int VerifyStart {
            get {
                if (ParseNode.Parent.Parent == null) {
                    // This is the toplevel. We are in no switch and cannot skip any characters for verifying.
                    return ParseNode.Parent.MatchCharInTemplateAbsolute;
                }
                // We have already switched on the first character, and we can skip it when verifying.
                return ParseNode.Parent.MatchCharInTemplateAbsolute + 1; 
            }
        }

        internal int VerifyEnd {
            get {
                return ParseNode.MatchCharInTemplateAbsolute; 
            }
        }

        internal int VerifyCount {
            get {
                return VerifyEnd - VerifyStart;
            }
        }
    }
}
