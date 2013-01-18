using System;
using Starcounter;

namespace QueryProcessingTest {
    public class Person : Entity {
        String Name;
        DateTime Birthday;
        Int64 Gender; // 0 - man, 1 -female (standard)
    }

    public class Student : Person {
        University Place;
        String Program;
        Int64 StartYear;
    }

    public class Employee : Person {
        Employer Company;
        Manager Boss;
        DateTime StartDate;
        Decimal Salary;
    }

    public class Manager : Employee {

    }

    public class Teacher : Employee {
    }

    public class Professor : Teacher {
        String Subject;
    }

    public class Employer : Entity {
        Employer Director;
        String Address;
    }

    public class University : Employer {

    }
}
