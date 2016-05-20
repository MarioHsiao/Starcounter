using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.XSON.PartialClassGenerator {
    public enum InvalidCodeBehindError {
        NotSpecified = 0,
        DefineInstanceConstructor,
        MultipleMappingAttributes,
        RootClassWithCustomMapping,
        ClassNotPartial,
        ClassGeneric,
        MultipleRootClasses,
        InputHandlerStatic,
        InputHandlerBadParameterCount,
        InputHandlerHasTypeParameters,
        InputHandlerWithRefParameter,
    }

    public class InvalidCodeBehindException : Exception {
        public readonly InvalidCodeBehindError Error = InvalidCodeBehindError.NotSpecified;
        public readonly SyntaxNode Node;

        public InvalidCodeBehindException(InvalidCodeBehindError error, SyntaxNode node = null) : 
            base(Enum.GetName(typeof(InvalidCodeBehindError), error)) {
            Error = error;
            Node = node;
        }
    }
}
