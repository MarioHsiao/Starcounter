using Sc.Server.Weaver;
using Starcounter.Binding;
using Starcounter.Internal;
using System;

namespace Starcounter.Advanced {
    /// <summary>
    /// Runtime type exposing a given <see cref="IObjectProxy"/>
    /// as a tuple (via <see cref="IDbTuple"/>.
    /// </summary>
    internal class ProxyBasedRuntimeTuple : IDbTuple {
        readonly IObjectProxy proxy;
        int typeIndex;
        int instantiatesIndex;
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
                    case WeavedNames.InstantiatesColumn:
                        instantiatesIndex = i;
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
                var type = DbState.ReadTypeReference(proxy.Identity, proxy.ThisHandle, typeIndex);
                return type != null ? TupleHelper.ToTuple(type) : null;
            }
            set {
                DbState.WriteTypeReference(proxy.Identity, proxy.ThisHandle, typeIndex, value.Proxy);
            }
        }

        IDbTuple IDbTuple.Inherits {
            get {
                var inherits = DbState.ReadTypeReference(proxy.Identity, proxy.ThisHandle, inheritsIndex);
                return inherits != null ? TupleHelper.ToTuple(inherits) : null;
            }
            set { DbState.WriteTypeReference(proxy.Identity, proxy.ThisHandle, inheritsIndex, value.Proxy); }
        }

        string IDbTuple.Name {
            get { return DbState.ReadTypeName(proxy.Identity, proxy.ThisHandle, typeNameIndex); }
            set { DbState.WriteTypeName(proxy.Identity, proxy.ThisHandle, typeNameIndex, value); }
        }

        bool IDbTuple.IsType {
            get { return DbState.ReadInt32(proxy.Identity, proxy.ThisHandle, instantiatesIndex) != sccoredb.STAR_INVALID_TABLE_ID; }
            set { throw new NotImplementedException(); }
        }

        int IDbTuple.Instantiates {
            get { return DbState.ReadInt32(proxy.Identity, proxy.ThisHandle, instantiatesIndex); }
            set { DbState.WriteInt32(proxy.Identity, proxy.ThisHandle, instantiatesIndex, value); }
        }

        IDbTuple IDbTuple.New() {
            // Proper error messages including new error codes.
            // Delayed until final implementation though (see
            // #2500 for more info).
            // TODO:
            var self = (IDbTuple)this;
            if (!self.IsType) throw new InvalidOperationException("This object is not a type.");
            if (string.IsNullOrEmpty(self.Name)) throw new InvalidOperationException("The type name is not specified.");

            var tb = Bindings.GetTypeBinding(self.Name);
            ulong oid = 0, addr = 0;
            DbState.Insert(tb.TableId, ref oid, ref addr);
            var proxy = tb.NewInstance(addr, oid);

            var tuple = TupleHelper.ToTuple(proxy);
            tuple.Type = self;

            return tuple;
        }
    }
}
