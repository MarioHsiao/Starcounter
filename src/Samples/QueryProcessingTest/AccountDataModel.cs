using System;
using Starcounter;

namespace QueryProcessingTest {
    public class User : Entity {
        public String UserId;
        public int UserIdNr;
        public DateTime BirthDay;
        public String FirstName;
        public String LastName;

        public String Name {
            get { return FirstName + " " + LastName; }
            // set
        }

        public int Age {
            get {
                return (DateTime.Now - BirthDay).Days / 365;
            }
        }
    }

    public class Account : Entity {
        public Int64 AccountId;
        public User Client;
        public Decimal Amount;
        public DateTime Updated;
    }
}
