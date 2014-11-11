
using Starcounter.Advanced;
using Starcounter.Internal;

namespace Starcounter {

    [Database]
    public abstract partial class Entity2 : IEntity {

        public Entity2()
            : this(__starcounterTypeSpecification.tableHandle, __starcounterTypeSpecification.typeBinding, (Uninitialized)null) {
        }

        public override bool Equals(object obj) {
            IBindable bindable = obj as IBindable;
            if (bindable != null) {
                if (object.ReferenceEquals(this, obj)) {
                    return true;
                }
                if (bindable.Identity == this.__sc__this_id__) {
                    IBindable bindable2 = this;
                    return bindable.Retriever.Equals(bindable2.Retriever);
                }
            }
            return false;
        }

        public override int GetHashCode() {
            return this.__sc__this_id__.GetHashCode();
        }

        public virtual void OnDelete() { }
    }
}
