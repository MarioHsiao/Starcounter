
using Starcounter.Advanced;
using Starcounter.Binding;
using Starcounter.Internal;
using System;

namespace Starcounter {

    /// <summary>
    /// Defines a common base class for Starcounter database
    /// classes, as an alternative to the [Database] attribute.
    /// </summary>
    [Database]
    public partial class Entity : IEntity  {
        /// <summary>
        /// Gets or sets the dynamic type of the current entity.
        /// </summary>
        [Type]
        public IObjectView Type {
            get {
                return DbState.ReadTypeReference(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle___sc__type__);
            }
            set {
                WriteType(value as IObjectProxy);
            }
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
        /// Gets or sets the name of the current entity. The
        /// name is commonly used to name entities that are
        /// types, but can be used on entities that are not
        /// types too, giving them a logical name.
        /// </summary>
        [TypeName]
        public string Name {
            get {
                return DbState.ReadTypeName(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle___sc__type_name__);
            }
            set {
                DbState.WriteTypeName(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle___sc__type_name__, value);
            }
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
        [Inherits]
        public IObjectView TypeInherits {
            get {
                return DbState.ReadInherits(__sc__this_id__, __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle___sc__inherits__);
            }
            set {
                WriteInherits(value as IObjectProxy);
            }
        }
        
        /// <summary>
        /// Gets or sets a value indicating if the current
        /// entity should be considered a dynamic type.
        /// </summary>
        public bool IsType {
            get {
                return DbState.ReadInt32(
                    __sc__this_id__,
                    __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle__sc__instantiates__) != sccoredb.STAR_INVALID_TABLE_ID;
            }
            set {
                throw new NotImplementedException();
            }
        }

        internal int Instatiates {
            get {
                return DbState.ReadInt32(
                    __sc__this_id__,
                    __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle__sc__instantiates__);
            }
            set {
                DbState.WriteInt32(
                    __sc__this_id__,
                    __sc__this_handle__,
                    __starcounterTypeSpecification.columnHandle__sc__instantiates__,
                    value);
            }
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
        public Entity EdgeSubject {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); } 
        }

        /// <summary>
        /// Gets or sets the object of the current entity in
        /// a context where the current entity is to be
        /// considered a relation (edge).
        /// </summary>
        public Entity EdgeObject {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Initialize a new <see cref="Entity"/>.
        /// </summary>
        public Entity()
            : this(__starcounterTypeSpecification.tableHandle, __starcounterTypeSpecification.typeBinding, (Uninitialized)null) {
        }

        /// <summary>
        /// Creates a new entity whose dynamic type will be the
        /// current entity, i.e. instantiating the current entity
        /// (where the current entity is to be considered a type).
        /// </summary>
        /// <returns>A new entity whose dynamic type is the
        /// current entity.</returns>
        public Entity New() {
            // Proper error messages including new error codes.
            // Delayed until final implementation though (see
            // #2500 for more info).
            // TODO:
            if (!IsType) throw new InvalidOperationException("This object is not a type.");
            if (string.IsNullOrEmpty(this.Name)) throw new InvalidOperationException("The type name is not specified.");

            var tb = Bindings.GetTypeBinding(this.Name);
            ulong oid = 0, addr = 0;
            DbState.Insert(tb.TableId, ref oid, ref addr);
            var proxy = (Entity) tb.NewInstance(addr, oid);
            proxy.Type = this;
            return proxy;
        }

        /// <summary>
        /// Deletes the current entity.
        /// </summary>
        public void Delete() {
            Db.Delete(this);
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

        internal void WriteType(IObjectProxy type) {
            DbState.WriteTypeReference(
                __sc__this_id__,
                __sc__this_handle__,
                __starcounterTypeSpecification.columnHandle___sc__type__,
                type);
        }

        internal void WriteInherits(IObjectProxy type) {
            DbState.WriteInherits(
                __sc__this_id__,
                __sc__this_handle__,
                __starcounterTypeSpecification.columnHandle___sc__inherits__,
                type);
        }
    }
}
