using Starcounter;
using Starcounter.Binding;
using System;

#if ACCOUNTTEST_MODEL
namespace accounttest
{
    public class User : Entity
    {
        public String FirstName;
        public String LastName;
        public String UserId;

        public override String ToString()
        {
            return "User " + FirstName + " " + LastName + " with ID " + UserId;
        }
    }

    public class account : Entity
    {
        public Int64 AccountId;
        public User Client;
        public Decimal Amount;
    }

}
#endif
