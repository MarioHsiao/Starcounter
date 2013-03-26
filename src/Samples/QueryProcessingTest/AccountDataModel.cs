using System;
using Starcounter;

namespace QueryProcessingTest {
    [Database]
    public class User {
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

    [Database]
    public class Account {
        public Int64 AccountId;
        public User Client;
        public Decimal Amount;
        public DateTime Updated;
    }
}
