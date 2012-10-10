using Starcounter;
using Starcounter.Binding;
using System;

namespace AccountTest
{
#if ACCOUNTTEST_MODEL
    public class User : Entity
    {
        public String FirstName;
        public String LastName;
        public String UserId;
    }

    public class Account : Entity
    {
        public Int64 AccountId;
        public User Client;
        public Decimal Amount;
    }
#endif
}
