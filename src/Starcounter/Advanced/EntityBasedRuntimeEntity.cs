
namespace Starcounter.Advanced {
    /// <summary>
    /// Runtime type exposing a given <see cref="Entity"/>
    /// as a <see cref="IDbTuple"/>.
    /// </summary>
    internal class EntityBasedRuntimeEntity : IDbTuple {
        readonly Entity instance;

        internal EntityBasedRuntimeEntity(Entity entity) {
            instance = entity;
        }

        IDbTuple IDbTuple.Type {
            get {
                return instance.Type == null ? null : EntityHelper.ToEntity(instance.Type);
            }
            set {
                instance.WriteType(value.Proxy);
            }
        }

        IDbTuple IDbTuple.Inherits {
            get {
                return instance.TypeInherits == null ? null : EntityHelper.ToEntity(instance.TypeInherits);
            }
            set {
                instance.WriteInherits(value.Proxy);
            }
        }

        bool IDbTuple.IsType {
            get {
                return instance.IsType;
            }
            set {
                instance.IsType = value;
            }
        }

        string IDbTuple.Name {
            get {
                return instance.Name;
            }
            set {
                instance.Name = value;
            }
        }

        Binding.IObjectProxy IDbTuple.Proxy {
            get { return instance; }
        }
    }
}
