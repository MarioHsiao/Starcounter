
using Starcounter.Advanced;
using System;


namespace Starcounter.Internal.XSON.Tests {

    public class Entity : IBindable {

        public static UInt64 NextObjectNo = 0;

        public UInt64 ObjectNo;

        public Entity() {
            ObjectNo = NextObjectNo;
            NextObjectNo++;
            Database.Entities.Add(this);
        }

        ulong IBindable.Identity {
            get {
                return ObjectNo;
            }
        }

//        IBindableRetriever IBindable.Retriever {
//            get {
//                return Database.The;
//            }
//        }
    }
}
