using Microsoft.CodeAnalysis;
using System;

namespace Starcounter.XSON.PartialClassGenerator {
    public enum InvalidCodeBehindError {
        NotSpecified = 0,
        DefineInstanceConstructor,
        MultipleMappingAttributes,
        RootClassWithCustomMapping,
        ClassNotMapped,
        ClassNotPartial,
        ClassGeneric,
        MultipleRootClasses,
        InputHandlerStatic,
        InputHandlerAbstract,
        InputHandlerBadParameterCount,
        InputHandlerHasTypeParameters,
        InputHandlerWithRefParameter,
        InputHandlerNotVoidReturnType
    }

    public class InvalidCodeBehindException : Exception {
        public readonly InvalidCodeBehindError Error = InvalidCodeBehindError.NotSpecified;
        public readonly SyntaxNode Node;

        public InvalidCodeBehindException(InvalidCodeBehindError error, SyntaxNode node = null) : 
            base(Enum.GetName(typeof(InvalidCodeBehindError), error)) {
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
    }
}
