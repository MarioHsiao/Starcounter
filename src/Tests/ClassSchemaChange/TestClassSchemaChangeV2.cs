using System;
using Starcounter;
using System.Diagnostics;

namespace TestClassSchemaChange {
    class Program {
        static void Main() {
            var personBD = new DateTime(1942, 5, 4);
            if (Db.SQL("select p from person p").First == null) {
                Db.Transact(delegate {
                    Person person = new Person { BirthDate = personBD, FirstName = "Sven", LastName = "Persson" };
                    Account account1 = new Account { AccountId = 1, Amount = 100m, Client = person };
                    Animal animal = new Animal { BirthDate = new DateTime(1984, 9, 23) };
                    Account account2 = new Account { AccountId = 2, Amount = 200m, Client = person };
                });
            }
            int count = 0;
            foreach (Person p in Db.SQL<Person>("select p from person p")) {
                count++;
                ScAssertion.Assert(p.BirthDate.Ticks != 0);
				ScAssertion.Assert(p.FirstName == "Sven");
            }
            ScAssertion.Assert(count == 1);
            count = 0;
        }
    }

    [Database]
    public class Animal {
        public DateTime BirthDate;
    }

    [Database]
    public class Person : Animal {
        public String FirstName;
        public String LastName { get; set; }
    }
    [Database]
    public class Account {
        public Person Client;
        public UInt64 AccountId;
        public Decimal Amount;
    }

    [Database]
    public class Organization {
        public UInt32 OrganizationId;
    }
}