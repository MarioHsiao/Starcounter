using Starcounter.Binding;

namespace Starcounter.Advanced {
    
    /// <summary>
    /// Defines the interface of any database typpe. All database classes can be
    /// represented by the <see cref="IDbTuple"/> interface using runtime factory/cast
    /// method <see cref="TupleHelper.ToTuple(object)"/>.
    /// </summary>
    public interface IDbTuple {
        /// <summary>
        /// Gets or sets the type of the current tuple.
        /// </summary>
        IDbTuple Type {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the type the current tuple is
        /// a subtype of.
        /// </summary>
        IDbTuple Inherits {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating if the current tuple
        /// is a type.
        /// </summary>
        bool IsType {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a handle to the concept the current tuple
        /// (being a type) instantiates.
        /// </summary>
        /// <remarks>We should consider making this one not part of
        /// the interface, since its more of an implementation detail.
        /// </remarks>
        int Instantiates {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the current tuple, normally
        /// used when naming a type.
        /// </summary>
        string Name {
            get;
            set;
        }

        /// <summary>
        /// Gets a reference to a <see cref="IObjectProxy"/> that
        /// can be used to access data of the current tuple.
        /// </summary>
        IObjectProxy Proxy {
            get;
        }

        /// <summary>
        /// Support runtime instantiation of database tuples acting
        /// as types.
        /// </summary>
        /// <returns>A new database tuple whose type will be set to
        /// the current tuple.</returns>
        IDbTuple New();

        /// <summary>
        /// Support runtime specialization of database tuples acting
        /// as types.
        /// </summary>
        /// <returns>A new database tuple that will be set to inherit
        /// the current tuple.</returns>
        IDbTuple Derive();
    }
}
