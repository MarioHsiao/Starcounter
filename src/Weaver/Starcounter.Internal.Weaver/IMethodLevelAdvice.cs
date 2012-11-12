// ***********************************************************************
// <copyright file="IMethodLevelAdvice.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

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
        /// <value>The target methods.</value>
        IEnumerable<MethodDefDeclaration> TargetMethods {
            get;
        }

        /// <summary>
        /// Gets set of operands to which the advice should be applied,
        /// or <b>null</b> if the advice should be applied to all operands.
        /// </summary>
        /// <value>The operands.</value>
        IEnumerable<MetadataDeclaration> Operands {
            get;
        }

        /// <summary>
        /// Gets the kinds of join points to which the advice should be applied.
        /// </summary>
        /// <value>The join point kinds.</value>
        JoinPointKinds JoinPointKinds {
            get;
        }
    }
}