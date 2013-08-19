
using Starcounter.Advanced;
using System.Collections.Generic;
namespace Starcounter.Internal.XSON.Tests {

    /// <summary>
    /// 
    /// </summary>
    public class Database : IBindableRetriever {

        /// <summary>
        /// 
        /// </summary>
        public static Database The = new Database();

        /// <summary>
        /// 
        /// </summary>
        public static List<Entity> Entities = new List<Entity>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        IBindable IBindableRetriever.Retrieve(ulong identifier) {
            return Entities[(int)identifier];
        }
    }
}
