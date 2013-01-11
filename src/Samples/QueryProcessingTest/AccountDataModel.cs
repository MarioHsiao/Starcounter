using System;
using Starcounter;

namespace QueryProcessingTest {
    public class User : Entity {
        public String UserId;
    }

    public class Account : Entity {
        public Int64 AccountId;
        public User Client;
        public Decimal Amount;
    }
}
