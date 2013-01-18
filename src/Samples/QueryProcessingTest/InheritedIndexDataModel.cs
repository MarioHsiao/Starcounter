using System;
using Starcounter;

namespace QueryProcessingTest {
    public class Person : Entity {
        public String Name;
        public DateTime Birthday;
        public Int64 Gender; // 0 - man, 1 -female (standard)
    }

    public class Student : Person {
        public University Place;
        public String Program;
        public Int64 StartYear;
    }

    public class Employee : Person {
        public Employer Company;
        public Manager Boss;
        public DateTime StartDate;
        public Decimal Salary;
    }

    public class Manager : Employee {
        public Decimal Bonus;
    }

    public class Teacher : Employee {
        public String Qualification;
    }

    public class Professor : Teacher {
        public String Subject;
    }

    public class Employer : Entity {
        public Employer Director;
        public String Address;
    }

    public class University : Employer {
        public String License;
    }
}
