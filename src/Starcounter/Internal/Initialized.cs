// ***********************************************************************
// <copyright file="Initialized.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Internal {
    /// <summary>
    /// Type used to assure unique signatures when weaving database
    /// user code.
    /// </summary>
    /// <remarks>
    /// The weaver assures this class is never used explicitly
    /// in user code in a way that can violate the uniqeness of our
    /// required signatures.
    /// </remarks>
    /// <see cref="Uninitialized"/>
    public sealed class Initialized {
    }
}