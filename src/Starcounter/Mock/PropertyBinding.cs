
using Starcounter;
namespace Sc.Server.Binding
{
    public class PropertyBinding : IPropertyBinding
    {
        public int GetDataIndex()
        {
            throw new System.NotImplementedException();
        }

        public int AccessCost
        {
            get { throw new System.NotImplementedException(); }
        }

        public int Index
        {
            get { throw new System.NotImplementedException(); }
        }

        public IPropertyBinding InversePropertyBinding
        {
            get { throw new System.NotImplementedException(); }
        }

        public int MutateCost
        {
            get { throw new System.NotImplementedException(); }
        }

        public string Name
        {
            get { throw new System.NotImplementedException(); }
        }

        public ITypeBinding TypeBinding
        {
            get { throw new System.NotImplementedException(); }
        }

        public DbTypeCode TypeCode
        {
            get { throw new System.NotImplementedException(); }
        }
    }
}
