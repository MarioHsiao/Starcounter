
using Sc.Server.Internal;
using Sc.Server.Binding;
using Starcounter.Binding;

namespace Starcounter
{


    // TODO:
    // We must keep DbObject around because generated code links to DbState
    // methods with this type are parameter. Remove class and change all
    // references to Entity once the generated code has been changed.
    
    public class DbObject
    {

        internal ObjectRef ThisRef;
    }
    
    public abstract class Entity : DbObject, IObjectView
    {

        /// <summary>
        /// Specialized comparison operator for database objects. The
        /// framework-provided comparison operator for objects compares them by
        /// reference only, and for entity object's we want the comparison
        /// to be value based. This means that even if two objects are
        /// different in memory, the can still be considered equal if they only
        /// reference the same kernel database object.
        /// </summary>
        /// <param name="obj1">
        /// The first object in the comparison
        /// </param>
        /// <param name="obj2">
        /// The second object in the comparison
        /// </param>
        /// <returns>
        /// True if both references are null or if both references are not null
        /// and references the same kernel database object. False otherwise.
        /// </returns>
        public static bool operator == (Entity obj1, Entity obj2)
        {
            if (object.ReferenceEquals(obj1, null))
            {
                return object.ReferenceEquals(obj2, null);
            }
            return obj1.Equals(obj2);
        }

        /// <summary>
        /// Specialized comparison operator for database objects. The
        /// framework-provided comparison operator for objects compares them by
        /// reference only, and for entity object's we want the comparison
        /// to be value based. This means that even if two objects are
        /// different in memory, the can still be considered equal if they only
        /// reference the same kernel database object.
        /// </summary>
        /// <param name="obj1">
        /// The first object in the comparison
        /// </param>
        /// <param name="obj2">
        /// The second object in the comparison
        /// </param>
        /// <returns>
        /// False if both references are null or if both references are not null
        /// and references the same kernel database object. True otherwise.
        /// </returns>
        public static bool operator != (Entity obj1, Entity obj2)
        {
            if (object.ReferenceEquals(obj1, null))
            {
                return !object.ReferenceEquals(obj2, null);
            }
            return !obj1.Equals(obj2);
        }
        
        private TypeBinding typeBinding_;

        public Entity(Sc.Server.Internal.Uninitialized u) { }

        public Entity(ulong typeAddr, Sc.Server.Binding.TypeBinding typeBinding, Sc.Server.Internal.Uninitialized u)
        {
            DbState.Insert(this, typeAddr, typeBinding);
        }

        public void Delete()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Overrides the .NET frameworks Object's Equals method, that will
        /// compare for object reference equality. For a database object, the
        /// equals function uses value equality logic rather than reference
        /// equality logic; that means, that even if two instances are
        /// different in memory, they can still be considered the same if they
        /// reference the same database kernel object.
        /// </summary>
        /// <param name="obj">
        /// The object to compare this instance to.
        /// </param>
        /// <returns>
        /// True if obj is not null, is an entity object and references the
        /// same kernel object as this; false otherwise.
        /// </returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Entity);
        }

        public bool Equals(Entity obj)
        {
            if (obj != null)
            {
                if (obj.ThisRef.ObjectID == ThisRef.ObjectID)
                {
                    return (GetType() == obj.GetType());
                }
            }
            return false;
        }

        /// <summary>
        /// The GetHashCode implementation will return the hash-code of the
        /// unique object ID of a given object.
        /// </summary>
        public override int GetHashCode()
        {
            return ThisRef.ObjectID.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("{0}({1})", GetType().Name, ThisRef.ObjectID.ToString());
        }

        internal void Attach(ObjectRef objectRef, TypeBinding typeBinding)
        {
            ThisRef.ETI = objectRef.ETI;
            ThisRef.ObjectID = objectRef.ObjectID;
            typeBinding_ = typeBinding;
        }

        internal void Attach(ulong addr, ulong oid, TypeBinding typeBinding)
        {
            ThisRef.ETI = addr;
            ThisRef.ObjectID = oid;
            typeBinding_ = typeBinding;
        }

        internal ushort TableId { get { return typeBinding_.TableId; } }

        ITypeBinding IObjectView.TypeBinding { get { return typeBinding_; } }

        bool IObjectView.EqualsOrIsDerivedFrom(IObjectView obj)
        {
            throw new System.NotSupportedException();
        }

        Binary? IObjectView.GetBinary(int index)
        {
            return typeBinding_.GetPropertyBinding(index).GetBinary(this);
        }

        bool? IObjectView.GetBoolean(int index)
        {
            return typeBinding_.GetPropertyBinding(index).GetBoolean(this);
        }

        byte? IObjectView.GetByte(int index)
        {
            return typeBinding_.GetPropertyBinding(index).GetByte(this);
        }

        System.DateTime? IObjectView.GetDateTime(int index)
        {
            return typeBinding_.GetPropertyBinding(index).GetDateTime(this);
        }

        decimal? IObjectView.GetDecimal(int index)
        {
            return typeBinding_.GetPropertyBinding(index).GetDecimal(this);
        }

        double? IObjectView.GetDouble(int index)
        {
            return typeBinding_.GetPropertyBinding(index).GetDouble(this);
        }

        short? IObjectView.GetInt16(int index)
        {
            return typeBinding_.GetPropertyBinding(index).GetInt16(this);
        }

        int? IObjectView.GetInt32(int index)
        {
            return typeBinding_.GetPropertyBinding(index).GetInt32(this);
        }

        long? IObjectView.GetInt64(int index)
        {
            return typeBinding_.GetPropertyBinding(index).GetInt64(this);
        }

        IObjectView IObjectView.GetObject(int index)
        {
            return typeBinding_.GetPropertyBinding(index).GetObject(this);
        }

        sbyte? IObjectView.GetSByte(int index)
        {
            return typeBinding_.GetPropertyBinding(index).GetSByte(this);
        }

        float? IObjectView.GetSingle(int index)
        {
            return typeBinding_.GetPropertyBinding(index).GetSingle(this);
        }

        string IObjectView.GetString(int index)
        {
            return typeBinding_.GetPropertyBinding(index).GetString(this);
        }

        ushort? IObjectView.GetUInt16(int index)
        {
            return typeBinding_.GetPropertyBinding(index).GetUInt16(this);
        }

        uint? IObjectView.GetUInt32(int index)
        {
            return typeBinding_.GetPropertyBinding(index).GetUInt32(this);
        }

        ulong? IObjectView.GetUInt64(int index)
        {
            return typeBinding_.GetPropertyBinding(index).GetUInt64(this);
        }
    }
}
