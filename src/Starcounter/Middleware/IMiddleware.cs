
namespace Starcounter {

    /// <summary>
    /// Abstraction that allow middleware to be wrapped up in classes and
    /// registered by applications to customize the request pipeline.
    /// </summary>
    public interface IMiddleware {
        /// <summary>
        /// Registers the current middleware.
        /// </summary>
        /// <remarks>
        /// Invoking this method should preferably done by the code host. Hence,
        /// as an advice to implementors of this interface, we recommend implementing
        /// it explicitly.
        /// </remarks>
        /// <param name="application">The application enabling the current
        /// middleware.</param>
        void Register(Application application);
    }
}
