using Sc.Server.Weaver;
using Starcounter.Binding;

namespace Starcounter.Internal {
    /// <summary>
    /// Runtime type exposing a given <see cref="IObjectProxy"/>
    /// as an entity (via <see cref="IRuntimeEntity"/>.
    /// </summary>
    internal class ProxyBasedRuntimeEntity : IRuntimeEntity {
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

        IObjectProxy IRuntimeEntity.Proxy {
            get {
                return proxy;
            }
        }

        IRuntimeEntity IRuntimeEntity.Type {
            get {
                return Entity.From(DbState.ReadTypeReference(proxy.Identity, proxy.ThisHandle, typeIndex));
            }
            set {
                DbState.WriteTypeReference(proxy.Identity, proxy.ThisHandle, typeIndex, value.Proxy);
            }
        }

        IRuntimeEntity IRuntimeEntity.Inherits {
            get {
                return Entity.From(DbState.ReadTypeReference(proxy.Identity, proxy.ThisHandle, inheritsIndex));
            }
            set { DbState.WriteTypeReference(proxy.Identity, proxy.ThisHandle, inheritsIndex, value.Proxy); }
        }

        string IRuntimeEntity.Name {
            get { return DbState.ReadTypeName(proxy.Identity, proxy.ThisHandle, typeNameIndex); }
            set { DbState.WriteTypeName(proxy.Identity, proxy.ThisHandle, typeNameIndex, value); }
        }

        bool IRuntimeEntity.IsType {
            get { return DbState.ReadBoolean(proxy.Identity, proxy.ThisHandle, isTypeIndex); }
            set { DbState.WriteBoolean(proxy.Identity, proxy.ThisHandle, isTypeIndex, value); }
        }
    }
}
