
namespace Starcounter.Advanced {
    /// <summary>
    /// Runtime type exposing a given <see cref="Entity"/>
    /// as a <see cref="IRuntimeEntity"/>.
    /// </summary>
    internal class EntityBasedRuntimeEntity : IRuntimeEntity {
        readonly Entity instance;

        internal EntityBasedRuntimeEntity(Entity entity) {
            instance = entity;
        }

        IRuntimeEntity IRuntimeEntity.Type {
            get {
                return instance.Type == null ? null : new EntityBasedRuntimeEntity(instance.Type);
            }
            set {
                instance.WriteType(value.Proxy);
            }
        }

        IRuntimeEntity IRuntimeEntity.Inherits {
            get {
                return instance.TypeInherits == null ? null : new EntityBasedRuntimeEntity(instance.TypeInherits);
            }
            set {
                instance.WriteInherits(value.Proxy);
            }
        }

        bool IRuntimeEntity.IsType {
            get {
                return instance.IsType;
            }
            set {
                instance.IsType = value;
            }
        }

        string IRuntimeEntity.Name {
            get {
                return instance.Name;
            }
            set {
                instance.Name = value;
            }
        }

        Binding.IObjectProxy IRuntimeEntity.Proxy {
            get { return instance; }
        }
    }
}
