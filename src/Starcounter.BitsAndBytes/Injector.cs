
using System;
using Dynamo.Ioc;
using Dynamo.Ioc.Index;

namespace Starcounter.Internal {
    public class Injector : IocContainer {

        public Injector(Func<ILifetime> defaultLifetimeFactory = null, CompileMode defaultCompileMode = CompileMode.Delegate, IIndex index = null)
            : base(defaultLifetimeFactory, defaultCompileMode, index) {
        }

        public Injector()
            : base(() => { return new ContainerLifetime(); }) {
        }
    }
}
