using System;
using Starcounter;
using System.Diagnostics;

namespace TestClassSchemaChange {
    class Program {
        static void Main() {
            var personBD = new DateTime(1942, 5, 4);
            if (Db.SQL("select p from person p").First == null) {
                Db.Transact(delegate {
                    Person person = new Person { Birthdate = personBD, FirstName = "Sven", LastName = "Persson" };
                    Account account1 = new Account { AccountId = 1, Ammount = 100m, Client = person };
                    Animal animal = new Animal { Birthdate = new DateTime(1984, 9, 23) };
                    Account account2 = new Account { AccountId = 2, Ammount = 200m, Client = person };
                });
            }
            int count = 0;
            foreach (Person p in Db.SQL<Person>("select p from person p")) {
                count++;
                ScAssertion.Assert(p.Birthdate == personBD);
            }
            ScAssertion.Assert(count == 1);
            count = 0;
        }
    }

    [Database]
    public class Animal {
        public DateTime Birthdate;
    }

    [Database]
    public class Person : Animal {
        public String FirstName;
        public String LastName { get; set; }
        public String FullName { get { return FirstName + " " + LastName; } }
    }
    [Database]
    public class Account {
        public Person Client;
        public UInt64 AccountId;
        public Decimal Ammount;
    }
}