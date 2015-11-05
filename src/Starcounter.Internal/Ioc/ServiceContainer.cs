using System;
using System.Collections.Generic;

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
    public sealed class ServiceContainer : IServices {
        Dictionary<Type, object> instances = new Dictionary<Type, object>();
        Dictionary<Type, Func<ServiceContainer, object>> methods = new Dictionary<Type, Func<ServiceContainer, object>>();

        /// <summary>
        /// Register a single instance service.
        /// </summary>
        /// <typeparam name="T">Service interface.</typeparam>
        /// <param name="instance">Implementation of that service.</param>
        public void Register<T>(T instance) where T : class {
            if (instance == null) {
                throw new ArgumentNullException("instance");
            }

            instances[typeof(T)] = instance;
        }

        /// <summary>
        /// Register a service using a delegate, invoked the every time
        /// the service is retreived.
        /// </summary>
        /// <typeparam name="T">Service interface.</typeparam>
        /// <param name="instance">Implementation of that service.</param>
        public void Register<T>(Func<ServiceContainer, object> factory) {
            methods.Add(typeof(T), factory);
        }

        /// <inheritdoc/>
        T IServices.Get<T>() {
            object instance;
            if (!instances.TryGetValue(typeof(T), out instance)) {
                instance = methods[typeof(T)](this);
            }
            return (T)instance;
        }
    }
}
