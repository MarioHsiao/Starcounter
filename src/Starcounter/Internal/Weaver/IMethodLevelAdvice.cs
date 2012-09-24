
using System.Collections.Generic;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeWeaver;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// Interface to be implemented by method-level advices.
    /// </summary>
    internal interface IMethodLevelAdvice : IAdvice {
        /// <summary>
        /// Gets the set of methods to which the advice should be applied, or
        /// <b>null</b> if the advice should be applied to all methods.
        /// </summary>
        IEnumerable<MethodDefDeclaration> TargetMethods {
            get;
        }

        /// <summary>
        /// Gets set of operands to which the advice should be applied,
        /// or <b>null</b> if the advice should be applied to all operands.
        /// </summary>
        IEnumerable<MetadataDeclaration> Operands {
            get;
        }

        /// <summary>
        /// Gets the kinds of join points to which the advice should be applied.
        /// </summary>
        JoinPointKinds JoinPointKinds {
            get;
        }
    }
}