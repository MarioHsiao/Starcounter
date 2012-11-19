using Starcounter;
using Starcounter.Binding;
using System;

namespace AccountTest
{
#if ACCOUNTTEST_MODEL
    public class AUser : Entity
    {
        public String FirstName;
        public String LastName;
        public String UserId;

        public override String ToString()
        {
            return "User " + FirstName + " " + LastName + " with ID " + UserId;
        }
    }

    public class Account : Entity
    {
        public Int64 AccountId;
        public AUser Client;
        public Decimal Amount;
    }
#endif
}
