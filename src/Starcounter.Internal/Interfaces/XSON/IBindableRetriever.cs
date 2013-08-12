
using System;

namespace Starcounter.Advanced {

    /// <summary>
    /// In order to retrieve a bindable object (an object that
    /// supports IBindable) using its 64 bit identifier, a
    /// retriever (supporting IBindableRetriever) can be used.
    /// </summary>
    public interface IBindableRetriever {

        /// <summary>
        /// Returns an object representing the same resource
        /// as reported a specific identifier.
        /// </summary>
        /// </summary>
        /// <param name="identifier">The number representing the bindable object</param>
        /// <returns>The object found by the identifying number</returns>
        IBindable Retrieve(UInt64 identifier);
    }
}
