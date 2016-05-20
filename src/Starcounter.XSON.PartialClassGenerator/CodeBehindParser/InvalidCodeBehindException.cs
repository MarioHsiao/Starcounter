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

        public int Line {
            get {
                var y = Node.SyntaxTree.GetLineSpan(Node.Span);
                return y.StartLinePosition.Line + 1;
            }
        }

        public int Column {
            get {
                var y = Node.SyntaxTree.GetLineSpan(Node.Span);
                return y.StartLinePosition.Character;
            }
        }

        public int EndLine {
            get {
                var y = Node.SyntaxTree.GetLineSpan(Node.Span);
                return y.EndLinePosition.Line + 1;
            }
        }

        public int EndColumn {
            get {
                var y = Node.SyntaxTree.GetLineSpan(Node.Span);
                return y.EndLinePosition.Character;
            }
        }
    }
}
