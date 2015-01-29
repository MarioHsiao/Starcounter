using Starcounter.Binding;
using Starcounter.Internal;
using Starcounter.Metadata;

namespace Starcounter.Advanced {
    /// <summary>
    /// Runtime type exposing a given <see cref="RawView"/>
    /// as a <see cref="IDbTuple"/>.
    /// </summary>
    internal class RawViewBasedRuntimeTuple : IDbTuple {
        readonly RawView instance;

        internal RawViewBasedRuntimeTuple(RawView rw) {
            instance = rw;
        }

        IDbTuple IDbTuple.Type {
            get {
                // Return the terminating meta type when we implement
                // multiple layers in the type hierarchy.
                // TODO:
                return null;
            }
            set {
                RaiseExceptionWhenModified();
            }
        }

        IDbTuple IDbTuple.Inherits {
            get {
                var baseView = instance.Inherits as RawView;
                return baseView != null ? new RawViewBasedRuntimeTuple(baseView) : null;
            }
            set {
                RaiseExceptionWhenModified();
            }
        }

        bool IDbTuple.IsType {
            get {
                return true;
            }
            set {
                RaiseExceptionWhenModified();
            }
        }

        string IDbTuple.Name {
            get {
                return instance.FullName;
            }
            set {
                RaiseExceptionWhenModified();
            }
        }

        Binding.IObjectProxy IDbTuple.Proxy {
            get { return instance; }
        }

        IDbTuple IDbTuple.Create() {
            var self = (IDbTuple)this;
            var tb = Bindings.GetTypeBinding(self.Name);
            ulong oid = 0, addr = 0;
            DbState.Insert(tb.TableId, ref oid, ref addr);
            var proxy = tb.NewInstance(addr, oid);

            var tuple = TupleHelper.ToTuple(proxy);
            tuple.Type = self;

            return tuple;
        }

        void RaiseExceptionWhenModified() {
            // Any attempt to update the raw view must fail; it's
            // not editable.
            // Provide a better error message.
            // TODO:
            throw ErrorCode.ToException(Error.SCERRINVALIDOPERATION, "Illegal attempt to modify system object (RawView)");
        }
    }
}
