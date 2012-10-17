using System;
using Starcounter;

namespace SQLTest.EmployeeDb
{
    public enum LocationType
    {
        Unknown = 0, Country = 1, City = 2
    }

    public class Location : Entity
    {
        public String Name;
        public String Description;
        public LocationType Type;
        public Nullable<LocationType> NType;

        public Location() { }
    }

    public class Department : Entity
    {
        public String Name;
        public String Description;
        public Location Location;
        public Binary IdBinary;

        public Department() { }
    }

    public class Person : Entity
    {
        public String FirstName;
        public String LastName;
        public Location Home;
        public Person Father;

        public Person() { }
    }

    public class Employee : Person
    {
        public DateTime HireDate;
        public Nullable<DateTime> NHireDate;
        public Employee Manager;
        public Department Department;

        public Employee() { }
    }

    public class SalaryEmployee : Employee
    {
        public Boolean Commission;
        public Nullable<Boolean> NCommission;
        public SByte SalarySByte;
        public Nullable<SByte> NSalarySByte;
        public Int16 SalaryInt16;
        public Nullable<Int16> NSalaryInt16;
        public Int32 SalaryInt32;
        public Nullable<Int32> NSalaryInt32;
        public Int64 SalaryInt64;
        public Nullable<Int64> NSalaryInt64;
        public Byte SalaryByte;
        public Nullable<Byte> NSalaryByte;
        public UInt16 SalaryUInt16;
        public Nullable<UInt16> NSalaryUInt16;
        public UInt32 SalaryUInt32;
        public Nullable<UInt32> NSalaryUInt32;
        public UInt64 SalaryUInt64;
        public Nullable<UInt64> NSalaryUInt64;
        public Decimal SalaryDecimal;
        public Nullable<Decimal> NSalaryDecimal;
        public Single SalarySingle;
        public Nullable<Single> NSalarySingle;
        public Double SalaryDouble;
        public Nullable<Double> NSalaryDouble;

        public SalaryEmployee() { }
    }
}

namespace SQLTest.Test1b
{
    public class Person : Entity
    {
        public String FirstName;
        public String _LastName;
        // public DateTime Date;

        public Person() { }
    }
}

namespace SQLTest.PointDb
{
    public class IntegerPoint : Entity
    {
        public Nullable<Int64> X;
        public Nullable<Int64> Y;
        public Nullable<Int64> Z;

        public IntegerPoint(Nullable<Int64> x, Nullable<Int64> y, Nullable<Int64> z)
        {
            X = x; Y = y; Z = z;
        }
    }
}
