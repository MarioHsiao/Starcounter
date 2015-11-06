using System;
using System.Collections.Concurrent;

namespace Starcounter.Ioc {

    /// <summary>
    /// Simple service container allowing services to be installed and
    /// later retreived using <see cref="IServices"/>.
    /// </summary>
    /// <remarks>
    /// This service container employs a strategy where it allows services
    /// to be registered either as instances, or as delegates providing
    /// instances (to support an implementation to be created in a lazy
    /// fashion). It implements a latest-win strategy; installing services
    /// already installed does not fail, nor use any chainin, instead the
    /// latest installed service takes precedance.
    /// </remarks>
    public sealed class ServiceContainer : IServiceContainer {
        ConcurrentDictionary<Type, object> instances = new ConcurrentDictionary<Type, object>();
        ConcurrentDictionary<Type, Func<ServiceContainer, object>> methods = new ConcurrentDictionary<Type, Func<ServiceContainer, object>>();

        /// <inheritdoc/>
        public void Register<T>(T instance) where T : class {
            if (instance == null) {
                throw new ArgumentNullException("instance");
            }

            instances[typeof(T)] = instance;
        }

        /// <inheritdoc/>
        public void Register<T>(Func<ServiceContainer, T> factory) where T : class {
            methods[typeof(T)] = factory;
        }

        /// <inheritdoc/>
        public T Get<T>() {
            object instance = null;
            if (!instances.TryGetValue(typeof(T), out instance)) {
                Func<ServiceContainer, object> factory;
                if (methods.TryGetValue(typeof(T), out factory)) {
                    instance = factory(this);
                }
            }

            return instance == null ? default(T) : (T)instance;
        }
    }
}
