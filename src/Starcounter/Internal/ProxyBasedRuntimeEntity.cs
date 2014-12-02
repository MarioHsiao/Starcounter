using Sc.Server.Weaver;
using Starcounter.Binding;

namespace Starcounter.Internal {
    /// <summary>
    /// Runtime type exposing a given <see cref="IObjectProxy"/>
    /// as an entity (via <see cref="IEntity2"/>.
    /// </summary>
    internal class ProxyBasedRuntimeEntity : IEntity2 {
        readonly IObjectProxy proxy;
        int typeIndex;
        int isTypeIndex;
        int inheritsIndex;
        int typeNameIndex;

        internal ProxyBasedRuntimeEntity(IObjectProxy p) {
            proxy = p;
            Initialize();
        }

        void Initialize() {
            var tb = (TypeBinding)proxy.TypeBinding;
            var tableDef = tb.TypeDef.TableDef;

            for (int i = 0; i < tableDef.ColumnDefs.Length; i++) {
                var column = tableDef.ColumnDefs[i];
                switch (column.Name) {
                    case WeavedNames.TypeColumn:
                        typeIndex = i;
                        break;
                    case WeavedNames.InheritsColumn:
                        inheritsIndex = i;
                        break;
                    case WeavedNames.IsTypeColumn:
                        isTypeIndex = i;
                        break;
                    case WeavedNames.TypeNameColumn:
                        typeNameIndex = i;
                        break;
                };
            }
        }

        IObjectProxy IEntity2.Proxy {
            get {
                return proxy;
            }
        }

        IEntity2 IEntity2.Type {
            get {
                return Entity.From(DbState.ReadTypeReference(proxy.Identity, proxy.ThisHandle, typeIndex));
            }
            set {
                DbState.WriteTypeReference(proxy.Identity, proxy.ThisHandle, typeIndex, value.Proxy);
            }
        }

        IEntity2 IEntity2.Inherits {
            get {
                return Entity.From(DbState.ReadTypeReference(proxy.Identity, proxy.ThisHandle, inheritsIndex));
            }
            set { DbState.WriteTypeReference(proxy.Identity, proxy.ThisHandle, inheritsIndex, value.Proxy); }
        }

        string IEntity2.Name {
            get { return DbState.ReadTypeName(proxy.Identity, proxy.ThisHandle, typeNameIndex); }
            set { DbState.WriteTypeName(proxy.Identity, proxy.ThisHandle, typeNameIndex, value); }
        }

        bool IEntity2.IsType {
            get { return DbState.ReadBoolean(proxy.Identity, proxy.ThisHandle, isTypeIndex); }
            set { DbState.WriteBoolean(proxy.Identity, proxy.ThisHandle, isTypeIndex, value); }
        }
    }
}
