
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

        internal ushort TableId
        {
            get { throw new System.NotImplementedException(); }
        }

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
