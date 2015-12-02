using System;

namespace Starcounter.Ioc {
    /// <summary>
    /// Defines a service simple service container supporting instances to
    /// be registered as services, or registering of factory methods,
    /// providing service implemenetations on demand.
    /// </summary>
    public interface IServiceContainer : IServices {
        /// <summary>
        /// Register a single instance service.
        /// </summary>
        /// <typeparam name="T">Service interface.</typeparam>
        /// <param name="instance">Implementation of that service.</param>
        void Register<T>(T instance) where T : class;

        /// <summary>
        /// Register a service using a delegate, invoked to instantiate
        /// the service. The strategy when this is invoked is implementation
        /// specific.
        /// </summary>
        /// <typeparam name="T">Service interface.</typeparam>
        /// <param name="factory">Factory providing service instances.</param>
        void Register<T>(Func<ServiceContainer, T> factory) where T : class;
    }
}