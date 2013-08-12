
using System;

namespace Starcounter.Advanced {

    /// <summary>
    /// Used to provide a way to store a reference to a bindable object using a 64 bit integer.
    /// The database object class Entity implements this interface such that Puppet objects 
    /// (view model objects mirrored to external clients) can be bound to database objects.
    /// </summary>
    public interface IBindable {

        /// <summary>
        /// Returns a unique ID corresponding to the bound data object.
        /// </summary>
        UInt64 Identity { get; }
    }
}
