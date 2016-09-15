using Microsoft.CodeAnalysis;
using System;
using Starcounter.Internal;

namespace Starcounter.XSON.PartialClassGenerator {
    public enum InvalidCodeBehindError : uint {
        NotSpecified = Error.SCERRUNSPECIFIED,
        DefineInstanceConstructor = Error.SCERRJSONWITHCONSTRUCTOR,
        MultipleMappingAttributes = Error.SCERRJSONMAPPEDMORETHANONCE,
        RootClassWithCustomMapping = Error.SCERRJSONROOTHASCUSTOMMAPPING,
        ClassNotPartial = Error.SCERRJSONCLASSNOTPARTIAL,
        ClassGeneric = Error.SCERRJSONCLASSISGENERIC,
        MultipleRootClasses = Error.SCERRJSONWITHMULTIPLEROOTS,
        InputHandlerStatic = Error.SCERRJSONSTATICINPUTHANDLER,
        InputHandlerAbstract = Error.SCERRJSONABSTRACTINPUTHANDLER,
        InputHandlerBadParameterCount = Error.SCERRJSONINPUTHANDLERBADPARAMETERCOUNT,
        InputHandlerHasTypeParameters = Error.SCERRJSONINPUTHANDLERGENERIC,
        InputHandlerWithRefParameter = Error.SCERRJSONINPUTHANDLERREFPARAM,
        InputHandlerNotVoidReturnType = Error.SCERRJSONINPUTHANDLERNOTVOID,
        TemplateTypeUnsupportedAssignment = Error.SCERRUNSPECIFIED
    }

    public static class InvalidCodeBehindExtensions {
        public static bool IsBadInputHandlerSignature(this InvalidCodeBehindError error) {
            return
                error == InvalidCodeBehindError.InputHandlerAbstract ||
                error == InvalidCodeBehindError.InputHandlerBadParameterCount ||
                error == InvalidCodeBehindError.InputHandlerHasTypeParameters ||
                error == InvalidCodeBehindError.InputHandlerNotVoidReturnType ||
                error == InvalidCodeBehindError.InputHandlerStatic ||
                error == InvalidCodeBehindError.InputHandlerWithRefParameter;
        }
    }

    public class InvalidCodeBehindException : Exception {
        public readonly InvalidCodeBehindError Error = InvalidCodeBehindError.NotSpecified;
        public readonly SyntaxNode Node;

        public InvalidCodeBehindException(InvalidCodeBehindError error, SyntaxNode node = null) : base(string.Empty) {
            Error = error;
            Node = node;
        }

        public string FilePath {
            get {
                return Node.SyntaxTree.FilePath;
            }
        }

        /// <summary>
        /// Gets the zero-based starting line number where the error was found.
        /// </summary>
        /// <remarks>
        /// To translate this to a number that correspond to how they are normally
        /// displayed in an editor, increase it by 1.
        /// </remarks>
        public int Line {
            get {
                var y = Node.SyntaxTree.GetLineSpan(Node.Span);
                return y.StartLinePosition.Line;
            }
        }

        /// <summary>
        /// Gets the zero-based starting column where the error was found.
        /// </summary>
        /// <remarks>
        /// To translate this to a number that correspond to how they are normally
        /// displayed in an editor, increase it by 1.
        /// </remarks>
        public int Column {
            get {
                var y = Node.SyntaxTree.GetLineSpan(Node.Span);
                return y.StartLinePosition.Character;
            }
        }

        /// <summary>
        /// Gets the zero-based ending line number where the error was found.
        /// </summary>
        /// <remarks>
        /// To translate this to a number that correspond to how they are normally
        /// displayed in an editor, increase it by 1.
        /// </remarks>
        public int EndLine {
            get {
                var y = Node.SyntaxTree.GetLineSpan(Node.Span);
                return y.EndLinePosition.Line;
            }
        }

        /// <summary>
        /// Gets the zero-based ending column where the error was found.
        /// </summary>
        /// <remarks>
        /// To translate this to a number that correspond to how they are normally
        /// displayed in an editor, increase it by 1.
        /// </remarks>
        public int EndColumn {
            get {
                var y = Node.SyntaxTree.GetLineSpan(Node.Span);
                return y.EndLinePosition.Character;
            }
        }

        public override string Message {
            get {
                var e = Error;
                string postfix = null;
                if (e.IsBadInputHandlerSignature()) {
                    postfix = "Try instead: void Handle(Input.* input);";
                }
                return ErrorCode.ToMessage((uint)e, postfix).Message;
            }
        }

        public override string HelpLink {
            get {
                return ErrorCode.ToHelpLink((uint)Error);
            }
            set {
                throw new InvalidOperationException("We construct the helplink from the error");
            }
        }
    }
}
