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

        private string _nickName;
        public String NickName { get { return _nickName; } set { _nickName = value; } }
        public String AnotherNickName { get { return _nickName; } set { _nickName = value; } }

        public String PatronymicName { get; set; }

        public String Name {
            get { return FirstName + " " + LastName; }
            // set
        }

        public int Age {
            get {
                return (DataPopulation.CurrentDate - BirthDay).Days / 365;
            }
        }
    }

    [Database]
    public class Account {
        public Int64 AccountId;
        public User Client;
        public String AccountType;
        public Decimal Amount;
        public DateTime When;
        public String Where;
        public Boolean NotActive;
    }
}
