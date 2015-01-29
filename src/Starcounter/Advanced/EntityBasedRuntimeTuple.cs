
namespace Starcounter.Advanced {
    /// <summary>
    /// Runtime type exposing a given <see cref="Entity"/>
    /// as a <see cref="IDbTuple"/>.
    /// </summary>
    internal class EntityBasedRuntimeTuple : IDbTuple {
        readonly Entity instance;

        internal EntityBasedRuntimeTuple(Entity entity) {
            instance = entity;
        }

        IDbTuple IDbTuple.Type {
            get {
                return instance.Type == null ? null : TupleHelper.ToTuple(instance.Type);
            }
            set {
                instance.WriteType(value.Proxy);
            }
        }

        IDbTuple IDbTuple.Inherits {
            get {
                return instance.TypeInherits == null ? null : TupleHelper.ToTuple(instance.TypeInherits);
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

        IDbTuple IDbTuple.Create() {
            throw new System.NotImplementedException();
        }
    }
}
