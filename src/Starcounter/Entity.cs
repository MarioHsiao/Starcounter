// ***********************************************************************
// <copyright file="Entity.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Advanced;
using Starcounter.Binding;
using Starcounter.Internal;
using System;

namespace Starcounter
{


    /// <summary>
    /// Class NotPersistentAttribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NotPersistentAttribute : Attribute {
    }

    /// <summary>
    /// Class Entity
    /// </summary>
    public abstract class Entity : IObjectView, IBindable
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

        private ObjectRef thisRef;
        public ObjectRef ThisRef { get { return thisRef; } set { thisRef = value; } }
        public ulong Identity { get { throw new NotImplementedException(); } }
        private TypeBinding typeBinding_ = null;

        /// <summary>
        /// 
        /// </summary>
        protected Entity()
            : base() {
            throw ErrorCode.ToException(Error.SCERRCODENOTENHANCED);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="u"></param>
        protected Entity(Uninitialized u) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableId"></param>
        /// <param name="typeBinding"></param>
        /// <param name="u"></param>
        public Entity(ushort tableId, TypeBinding typeBinding, Uninitialized u)
        {
            // Temporary implementation to be replaced by the non-Entity way.
            // Now supports the new API of DbState.
            ulong oid = 0, addr = 0;
            DbState.Insert(tableId, ref oid, ref addr);
            this.thisRef.ETI = addr;
            this.thisRef.ObjectID = oid;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
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

        internal TypeBinding TypeBinding { get { return typeBinding_; } }

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

#if DEBUG
        /// <summary>
        /// Comparing this and given objects and asserting that they are equal.
        /// </summary>
        /// <param name="other">The given object to compare with this object.</param>
        /// <returns>True if the objects are equals and false otherwise.</returns>
        public bool AssertEquals(IObjectView other)
        {
            throw new NotImplementedException("Assert equals is not implemented for Entity.");
        }
#endif

        ulong IBindable.Identity {
            get { return DbHelper.GetObjectID(this); }
        }
    }
}
