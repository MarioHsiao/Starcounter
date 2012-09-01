
using Starcounter;
using Starcounter.Query.Execution;

namespace Sc.Server.Binding
{
    public class IndexInfo
    {
        public string Name;
        public ulong Handle;

        public int AttributeCount;

        public DbTypeCode GetTypeCode(int index)
        {
            throw new System.NotImplementedException();
        }

        internal SortOrder GetSortOrdering(int index)
        {
            throw new System.NotImplementedException();
        }

        internal string GetPathName(int index)
        {
            throw new System.NotImplementedException();
        }
    }
}
