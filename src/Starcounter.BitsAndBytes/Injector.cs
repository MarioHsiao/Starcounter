// ***********************************************************************
// <copyright file="Injector.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Dynamo.Ioc;
using Dynamo.Ioc.Index;

namespace Starcounter.Internal {
    /// <summary>
    /// Class Injector
    /// </summary>
    public class Injector : IocContainer {

        /// <summary>
        /// Initializes a new instance of the <see cref="Injector" /> class.
        /// </summary>
        /// <param name="defaultLifetimeFactory">The default lifetime factory.</param>
        /// <param name="defaultCompileMode">The default compile mode.</param>
        /// <param name="index">The index.</param>
        public Injector(Func<ILifetime> defaultLifetimeFactory = null, CompileMode defaultCompileMode = CompileMode.Delegate, IIndex index = null)
            : base(defaultLifetimeFactory, defaultCompileMode, index) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Injector" /> class.
        /// </summary>
        public Injector()
            : base(() => { return new ContainerLifetime(); }) {
        }
    }
}
