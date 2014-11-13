
using Starcounter.Advanced;
using Starcounter.Internal;
using System;

namespace Starcounter {

    /// <summary>
    /// Defines a common base class for Starcounter database
    /// classes, as an alternative to the [Database] attribute.
    /// </summary>
    [Database]
    public abstract partial class Entity2 : IEntity {
        /// <summary>
        /// Gets or sets the dynamic type of the current entity.
        /// </summary>
        public Entity2 Type {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Allows any single entity to represent a set of instances 
        /// (such as 2 cars or 3.5 litres of water)
        /// </summary>
        public decimal Quantity { 
            get { throw new NotImplementedException(); } 
            set { throw new NotImplementedException(); } 
        }

        /// <summary>
        /// Creates a new entity whose dynamic type will be the
        /// current entity, i.e. instantiating the current entity
        /// (where the current entity is to be considered a type).
        /// </summary>
        /// <returns>A new entity whose dynamic type is the
        /// current entity.</returns>
        public Entity2 Create() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a new entity whose parent dynamic type
        /// will be the current entity, i.e. deriving the
        /// current entity (where the current entity is to
        /// be considered a type).
        /// </summary>
        /// <returns>A new entity whose base dynamic type is
        /// the current entity.</returns>
        public Entity2 Derive() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets or sets the name of the current entity. The
        /// name is commonly used to name entities that are
        /// types, but can be used on entities that are not
        /// types too, giving them a logical name.
        /// </summary>
        public string Name { 
            get { throw new NotImplementedException(); } 
            set { throw new NotImplementedException(); } 
        }

        /// <summary>
        /// Gets the unique type ID of the current entity.
        /// </summary>
        public string TypeID { 
            get { throw new NotImplementedException(); } 
            set { throw new NotImplementedException(); } 
        }

        /// <summary>
        /// Gets the unique object number of the current
        /// entity.
        /// </summary>
        public ulong ObjectNo { 
            get { return DbHelper.GetObjectNo(this); } 
        }

        /// <summary>
        /// Gets the unique object number of the current
        /// entity, represented as a string.
        /// </summary>
        public string ObjectID { 
            get { return DbHelper.GetObjectID(this); }
        }
        
        /// <summary>
        /// Deletes the current entity.
        /// </summary>
        public void Delete() {
            Db.Delete(this);
        }
        
        /// <summary>
        /// Gets or sets a property of the current entity.
        /// </summary>
        /// <param name="propertName">The name of the
        /// property to set.</param>
        /// <returns>The value of the named property.</returns>
        public object this[string propertName] {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets or sets the parent dynamic type of the
        /// current entity.
        /// </summary>
        /// <seealso cref="Derive"/>
        public Entity2 TypeInherits {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
        
        /// <summary>
        /// Gets or sets a value indicating if the current
        /// entity should be considered a dynamic type.
        /// </summary>
        public bool IsType {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
        
        /// <summary>
        /// Gets or sets a value indicating if the current
        /// entity is an edge.
        /// </summary>
        public bool IsEdge {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
        
        /// <summary>
        /// Gets or sets the subject of the current entity in
        /// a context where the current entity is to be
        /// considered a relation (edge).
        /// </summary>
        public Entity2 EdgeSubject {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); } 
        }

        /// <summary>
        /// Gets or sets the object of the current entity in
        /// a context where the current entity is to be
        /// considered a relation (edge).
        /// </summary>
        public Entity2 EdgeObject {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Initialize a new <see cref="Entity2"/>.
        /// </summary>
        public Entity2()
            : this(__starcounterTypeSpecification.tableHandle, __starcounterTypeSpecification.typeBinding, (Uninitialized)null) {
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override int GetHashCode() {
            return this.__sc__this_id__.GetHashCode();
        }

        /// <summary>
        /// Called on the current entity when it is being
        /// deleted.
        /// </summary>
        public virtual void OnDelete() { }
    }
}
