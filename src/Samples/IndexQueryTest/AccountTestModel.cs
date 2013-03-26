using Starcounter;
using Starcounter.Binding;
using System;

#if ACCOUNTTEST_MODEL
namespace accounttest
{
    [Database]
    public class User 
    {
        public String FirstName;
        public String LastName;
        public String UserId;

        public override String ToString()
        {
            return "User " + FirstName + " " + LastName + " with ID " + UserId;
        }
    }

    [Database]
    public class account 
    {
        public Int64 AccountId;
        public User Client;
        public Decimal Amount;
    }

}
#endif
