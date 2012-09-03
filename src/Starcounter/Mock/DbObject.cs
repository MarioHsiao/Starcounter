
using Starcounter.Binding;

namespace Starcounter
{
    
    public abstract class DbObject : IObjectView
    {

        public ObjectRef ThisRef;

        public void Attach(ObjectRef objectRef, Sc.Server.Binding.TypeBinding typeBinding)
        {
            ThisRef.ETI = objectRef.ETI;
            ThisRef.ObjectID = objectRef.ObjectID;
        }

        public void Attach(ulong addr, ulong oid, Sc.Server.Binding.TypeBinding typeBinding)
        {
            ThisRef.ETI = addr;
            ThisRef.ObjectID = oid;
        }

        public ushort TableId
        {
            get { throw new System.NotImplementedException(); }
        }

        public ITypeBinding TypeBinding
        {
            get { throw new System.NotImplementedException(); }
        }

        public bool EqualsOrIsDerivedFrom(IObjectView obj)
        {
            throw new System.NotImplementedException();
        }

        public Binary? GetBinary(int index)
        {
            throw new System.NotImplementedException();
        }

        public bool? GetBoolean(int index)
        {
            throw new System.NotImplementedException();
        }

        public byte? GetByte(int index)
        {
            throw new System.NotImplementedException();
        }

        public System.DateTime? GetDateTime(int index)
        {
            throw new System.NotImplementedException();
        }

        public decimal? GetDecimal(int index)
        {
            throw new System.NotImplementedException();
        }

        public double? GetDouble(int index)
        {
            throw new System.NotImplementedException();
        }

        public IObjectView GetExtension(int index)
        {
            throw new System.NotImplementedException();
        }

        public short? GetInt16(int index)
        {
            throw new System.NotImplementedException();
        }

        public int? GetInt32(int index)
        {
            throw new System.NotImplementedException();
        }

        public long? GetInt64(int index)
        {
            throw new System.NotImplementedException();
        }

        public IObjectView GetObject(int index)
        {
            throw new System.NotImplementedException();
        }

        public sbyte? GetSByte(int index)
        {
            throw new System.NotImplementedException();
        }

        public float? GetSingle(int index)
        {
            throw new System.NotImplementedException();
        }

        public string GetString(int index)
        {
            throw new System.NotImplementedException();
        }

        public ushort? GetUInt16(int index)
        {
            throw new System.NotImplementedException();
        }

        public uint? GetUInt32(int index)
        {
            throw new System.NotImplementedException();
        }

        public ulong? GetUInt64(int index)
        {
            throw new System.NotImplementedException();
        }
    }
}
