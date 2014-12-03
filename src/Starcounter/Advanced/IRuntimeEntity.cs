using Starcounter.Binding;

namespace Starcounter.Advanced {
    
    /// <summary>
    /// Defines the interface of any entity. All database classes that
    /// are defined not using <see cref="Entity"/> class as their base
    /// class can be represented by the <see cref="IRuntimeEntity"/> interface
    /// using runtime factory/cast method <see cref="Entity.From(object)"/>.
    /// </summary>
    public interface IRuntimeEntity {
        /// <summary>
        /// Gets or sets the type of the current entity.
        /// </summary>
        IRuntimeEntity Type {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the type the current entity is
        /// a subtype of.
        /// </summary>
        IRuntimeEntity Inherits {
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
