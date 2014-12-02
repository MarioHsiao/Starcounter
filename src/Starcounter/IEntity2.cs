using Starcounter.Binding;

namespace Starcounter {
    
    /// <summary>
    /// Defines the interface of any entity. All database classes that
    /// are defined not using <see cref="Entity"/> class as their base
    /// class can be represented by the <see cref="IEntity2"/> interface
    /// using runtime factory/cast method <see cref="Entity.From(object)"/>.
    /// </summary>
    /// <remarks>
    /// This interface will be renamed to IEntity as soon as we have
    /// figured out how to support the functionality of the old interface
    /// with the same name.
    /// <p>
    /// Possibly, we'll merge this interface with IObjectView/IObjectProxy
    /// later on.
    /// </p>
    /// </remarks>
    public interface IEntity2 {
        /// <summary>
        /// Gets or sets the type of the current entity.
        /// </summary>
        IEntity2 Type {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the type the current entity is
        /// a subtype of.
        /// </summary>
        IEntity2 Inherits {
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
