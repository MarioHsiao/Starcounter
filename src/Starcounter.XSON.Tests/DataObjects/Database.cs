
using Starcounter.Advanced;
using System.Collections.Generic;
namespace Starcounter.Internal.XSON.Tests {

    public class Database : IBindableRetriever {

        public static Database The = new Database();

        public static List<Entity> Entities = new List<Entity>();


        IBindable IBindableRetriever.Retrieve(ulong identifier) {
            return Entities[(int)identifier];
        }
    }
}
