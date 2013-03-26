// ***********************************************************************
// <copyright file="IRequestHandler.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Advanced;

namespace Starcounter {

    /// <summary>
    /// Interface IRequestHandler
    /// </summary>
    public interface IRequestHandler {

        /// <summary>
        /// Processes the request batch.
        /// </summary>
        /// <param name="requestBatch">The request batch.</param>
        void ProcessRequestBatch(Request requestBatch);
    }
}