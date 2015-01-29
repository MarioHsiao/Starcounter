using Starcounter.Metadata;

namespace Starcounter.Advanced {
    /// <summary>
    /// Runtime type exposing a given <see cref="RawView"/>
    /// as a <see cref="IRuntimeEntity"/>.
    /// </summary>
    internal class RawViewBasedRuntimeEntity : IRuntimeEntity {
        readonly RawView instance;

        internal RawViewBasedRuntimeEntity(RawView rw) {
            instance = rw;
        }

        IRuntimeEntity IRuntimeEntity.Type {
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

        IRuntimeEntity IRuntimeEntity.Inherits {
            get {
                var baseView = instance.Inherits as RawView;
                return baseView != null ? new RawViewBasedRuntimeEntity(baseView) : null;
            }
            set {
                RaiseExceptionWhenModified();
            }
        }

        bool IRuntimeEntity.IsType {
            get {
                return true;
            }
            set {
                RaiseExceptionWhenModified();
            }
        }

        string IRuntimeEntity.Name {
            get {
                return instance.FullName;
            }
            set {
                RaiseExceptionWhenModified();
            }
        }

        Binding.IObjectProxy IRuntimeEntity.Proxy {
            get { return instance; }
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
