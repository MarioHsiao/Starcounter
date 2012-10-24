// ***********************************************************************
// <copyright file="ITransactionConflictException.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter {
    /// <summary>
    /// Interface that, when implemented by an <see cref="Exception"/> class,
    /// means that the exception may be solved by an automatic retry of the
    /// transaction.
    /// </summary>
    public interface ITransactionConflictException {

#if false
    /// <summary>
    /// Method invoked before the transaction is retried.
    /// </summary>
    void OnRetrying();
#endif
    }
}
