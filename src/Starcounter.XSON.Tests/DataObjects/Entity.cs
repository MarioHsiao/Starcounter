
using Starcounter.Advanced;
using System;


namespace Starcounter.Internal.XSON.Tests {

    /// <summary>
    /// 
    /// </summary>
    public class Entity : IBindable {

        /// <summary>
        /// 
        /// </summary>
        public static UInt64 NextObjectNo = 0;

        /// <summary>
        /// 
        /// </summary>
        public UInt64 ObjectNo;

        /// <summary>
        /// 
        /// </summary>
        public Entity() {
            ObjectNo = NextObjectNo;
            NextObjectNo++;
            Database.Entities.Add(this);
        }

        /// <summary>
        /// 
        /// </summary>
        ulong IBindable.Identity {
            get {
                return ObjectNo;
            }
        }

        /// <inheritdoc />
        IBindableRetriever IBindable.Retriever {
            get {
                return Database.The;
            }
        }
    }
}
