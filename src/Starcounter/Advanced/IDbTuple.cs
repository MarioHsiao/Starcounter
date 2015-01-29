using Starcounter.Binding;

namespace Starcounter.Advanced {
    
    /// <summary>
    /// Defines the interface of any database typpe. All database classes can be
    /// represented by the <see cref="IDbTuple"/> interface using runtime factory/cast
    /// method <see cref="EntityHelper.ToEntity(object)"/>.
    /// </summary>
    public interface IDbTuple {
        /// <summary>
        /// Gets or sets the type of the current entity.
        /// </summary>
        IDbTuple Type {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the type the current entity is
        /// a subtype of.
        /// </summary>
        IDbTuple Inherits {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating if the current entity
        /// is a type.
        /// </summary>
        bool IsType {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the current entity, normally
        /// used when naming a type.
        /// </summary>
        string Name {
            get;
            set;
        }

        /// <summary>
        /// Gets a reference to a <see cref="IObjectProxy"/> that
        /// can be used to access data of the current entity.
        /// </summary>
        IObjectProxy Proxy {
            get;
        }
    }
}
