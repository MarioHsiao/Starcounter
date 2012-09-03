
using Sc.Server.Internal;
using Starcounter.Binding;

namespace Starcounter
{
    
    public abstract class Entity : IObjectView
    {

        internal ObjectRef ThisRef;

        public Entity(Sc.Server.Internal.Uninitialized u) { }

        public Entity(ulong typeAddr, Sc.Server.Binding.TypeBinding typeBinding, Sc.Server.Internal.Uninitialized u)
        {
            DbState.Insert(this, typeAddr, typeBinding);
        }

        public void Delete()
        {
            throw new System.NotImplementedException();
        }

        internal void Attach(ObjectRef objectRef, Sc.Server.Binding.TypeBinding typeBinding)
        {
            ThisRef.ETI = objectRef.ETI;
            ThisRef.ObjectID = objectRef.ObjectID;
        }

        internal void Attach(ulong addr, ulong oid, Sc.Server.Binding.TypeBinding typeBinding)
        {
            ThisRef.ETI = addr;
            ThisRef.ObjectID = oid;
        }

        internal ushort TableId
        {
            get { throw new System.NotImplementedException(); }
        }

        ITypeBinding IObjectView.TypeBinding
        {
            get { throw new System.NotImplementedException(); }
        }

        bool IObjectView.EqualsOrIsDerivedFrom(IObjectView obj)
        {
            throw new System.NotImplementedException();
        }

        Binary? IObjectView.GetBinary(int index)
        {
            throw new System.NotImplementedException();
        }

        bool? IObjectView.GetBoolean(int index)
        {
            throw new System.NotImplementedException();
        }

        byte? IObjectView.GetByte(int index)
        {
            throw new System.NotImplementedException();
        }

        System.DateTime? IObjectView.GetDateTime(int index)
        {
            throw new System.NotImplementedException();
        }

        decimal? IObjectView.GetDecimal(int index)
        {
            throw new System.NotImplementedException();
        }

        double? IObjectView.GetDouble(int index)
        {
            throw new System.NotImplementedException();
        }

        short? IObjectView.GetInt16(int index)
        {
            throw new System.NotImplementedException();
        }

        int? IObjectView.GetInt32(int index)
        {
            throw new System.NotImplementedException();
        }

        long? IObjectView.GetInt64(int index)
        {
            throw new System.NotImplementedException();
        }

        IObjectView IObjectView.GetObject(int index)
        {
            throw new System.NotImplementedException();
        }

        sbyte? IObjectView.GetSByte(int index)
        {
            throw new System.NotImplementedException();
        }

        float? IObjectView.GetSingle(int index)
        {
            throw new System.NotImplementedException();
        }

        string IObjectView.GetString(int index)
        {
            throw new System.NotImplementedException();
        }

        ushort? IObjectView.GetUInt16(int index)
        {
            throw new System.NotImplementedException();
        }

        uint? IObjectView.GetUInt32(int index)
        {
            throw new System.NotImplementedException();
        }

        ulong? IObjectView.GetUInt64(int index)
        {
            throw new System.NotImplementedException();
        }
    }
}
