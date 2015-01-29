using Sc.Server.Weaver;
using Starcounter.Binding;
using Starcounter.Internal;

namespace Starcounter.Advanced {
    /// <summary>
    /// Runtime type exposing a given <see cref="IObjectProxy"/>
    /// as a tuple (via <see cref="IDbTuple"/>.
    /// </summary>
    internal class ProxyBasedRuntimeTuple : IDbTuple {
        readonly IObjectProxy proxy;
        int typeIndex;
        int isTypeIndex;
        int inheritsIndex;
        int typeNameIndex;

        internal ProxyBasedRuntimeTuple(IObjectProxy p) {
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

        IObjectProxy IDbTuple.Proxy {
            get {
                return proxy;
            }
        }

        IDbTuple IDbTuple.Type {
            get {
                return TupleHelper.ToTuple(DbState.ReadTypeReference(proxy.Identity, proxy.ThisHandle, typeIndex));
            }
            set {
                DbState.WriteTypeReference(proxy.Identity, proxy.ThisHandle, typeIndex, value.Proxy);
            }
        }

        IDbTuple IDbTuple.Inherits {
            get {
                return TupleHelper.ToTuple(DbState.ReadTypeReference(proxy.Identity, proxy.ThisHandle, inheritsIndex));
            }
            set { DbState.WriteTypeReference(proxy.Identity, proxy.ThisHandle, inheritsIndex, value.Proxy); }
        }

        string IDbTuple.Name {
            get { return DbState.ReadTypeName(proxy.Identity, proxy.ThisHandle, typeNameIndex); }
            set { DbState.WriteTypeName(proxy.Identity, proxy.ThisHandle, typeNameIndex, value); }
        }

        bool IDbTuple.IsType {
            get { return DbState.ReadBoolean(proxy.Identity, proxy.ThisHandle, isTypeIndex); }
            set { DbState.WriteBoolean(proxy.Identity, proxy.ThisHandle, isTypeIndex, value); }
        }
    }
}
