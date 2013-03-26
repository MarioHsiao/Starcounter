using System;
using System.Collections.Generic;
using Starcounter;

namespace SQLTest.EmployeeDb
{
    public static class EmployeeData
    {
        static List<object> objectList = new List<object>();

        public static void CreateData()
        {
            Db.Transaction(delegate
            {

                // Control that data is not already created.
                if (Db.SQL("select e from SQLTest.EmployeeDb.Employee e").First != null)
                    return;

                // Create instances of Location.

                Location location1 = new Location();
                location1.Name = "England";
                location1.Description = "Part of United Kingdom";
                location1.Type = LocationType.Country;
                //location1.NType = null;
                location1.NType = LocationType.Country;
                objectList.Add(location1);

                Location location2 = new Location();
                location2.Name = "Stockholm";
                location2.Description = "Capital of Sweden";
                location2.Type = LocationType.City;
                location2.NType = LocationType.City;
                objectList.Add(location2);

                Location location3 = new Location();
                location3.Name = "Solna";
                location3.Description = "Town just north of Stockholm";
                location3.Type = LocationType.City;
                location3.NType = LocationType.City;
                objectList.Add(location3);

                Location location4 = new Location();
                location4.Name = "Danderyd";
                location4.Description = "Rich suburb of Stockholm";
                location4.Type = LocationType.City;
                location4.NType = LocationType.City;
                objectList.Add(location4);

                Location location5 = new Location();
                location5.Name = "Falköping";
                location5.Description = "Small village in Sweden";
                location5.Type = LocationType.City;
                location5.NType = LocationType.City;
                objectList.Add(location5);

                Location location6 = new Location();
                location6.Name = "Saint Petersburg";
                location6.Description = "Big city in Russia";
                location6.Type = LocationType.City;
                location6.NType = LocationType.City;
                objectList.Add(location6);

                // Create instances of Department.

                Byte[] byteArr = new Byte[16];
                for (Int32 i = 0; i < 16; i++)
                {
                    byteArr[i] = 255;
                }
                Binary bin1 = new Binary(byteArr);

                for (Int32 i = 0; i < 16; i++)
                {
                    byteArr[i] = 254;
                }
                Binary bin2 = new Binary(byteArr);

                for (Int32 i = 0; i < 16; i++)
                {
                    byteArr[i] = 253;
                }
                Binary bin3 = new Binary(byteArr);

                Department department1 = new Department();
                department1.Name = "Head office";
                department1.Description = "Administration";
                department1.Location = location2;
                department1.IdBinary = bin1;
                objectList.Add(department1);

                Department department2 = new Department();
                department2.Name = "Kernel";
                department2.Description = "Development of database kernel";
                department2.Location = location5;
                department2.IdBinary = bin2;
                objectList.Add(department2);

                Department department3 = new Department();
                department3.Name = "Server";
                department3.Description = "Development of database server";
                department3.Location = location2;
                department3.IdBinary = bin3;
                objectList.Add(department3);

                Department department4 = new Department();
                department4.Name = "Sales";
                department4.Description = "Sales and marketing";
                department4.Location = null;
                department4.IdBinary = bin3;
                objectList.Add(department4);

                // Create instances of Person.

                Person person1 = new Person();
                person1.FirstName = "Bengt";
                person1.LastName = "Idestam";
                person1.Home = null;
                person1.Father = null;
                objectList.Add(person1);

                Person person2 = new Person();
                person2.FirstName = "Jon";
                person2.LastName = "Idestam";
                person2.Home = location2;
                person2.Father = person1;
                objectList.Add(person2);

                // Create instances of Employee.

                Employee employee1 = new Employee();
                employee1.FirstName = "Joachim";
                employee1.LastName = "Wester";
                employee1.Home = location1;
                employee1.Father = null;
                employee1.HireDate = new DateTime(2003, 1, 1);
                employee1.NHireDate = null;
                employee1.Manager = null;
                employee1.Department = null;
                objectList.Add(employee1);

                Employee employee2 = new Employee();
                employee2.FirstName = "Åsa";
                employee2.LastName = "Holmström";
                employee2.Home = location4;
                employee2.Father = null;
                employee2.HireDate = new DateTime(2009, 12, 1);
                employee2.NHireDate = new DateTime(2009, 12, 1);
                employee2.Manager = employee1;
                employee2.Department = department1;
                objectList.Add(employee2);

                Employee employee3 = new Employee();
                employee3.FirstName = "PETER";
                employee3.LastName = null;
                employee3.Home = location2;
                employee3.Father = person2;
                employee3.HireDate = new DateTime(2005, 10, 1);
                employee3.NHireDate = new DateTime(2005, 10, 1);
                employee3.Manager = employee2;
                employee3.Department = department3;
                objectList.Add(employee3);

                Employee employee4 = new Employee();
                employee4.FirstName = "Christian";
                employee4.LastName = "Holmstrand";
                employee4.Home = location3;
                employee4.Father = null;
                employee4.HireDate = new DateTime(2003, 1, 1);
                employee4.NHireDate = new DateTime(2003, 1, 1);
                employee4.Manager = employee3;
                employee4.Department = department3;
                objectList.Add(employee4);

                Employee employee5 = new Employee();
                employee5.FirstName = "per";
                employee5.LastName = "Samuelsson";
                employee5.Home = location2;
                employee5.Father = null;
                employee5.HireDate = new DateTime(2003, 1, 1);
                employee5.NHireDate = new DateTime(2003, 1, 1);
                employee5.Manager = employee3;
                employee5.Department = department3;
                objectList.Add(employee5);

                Employee employee6 = new Employee();
                employee6.FirstName = "Erik";
                employee6.LastName = "Ohlsson";
                employee6.Home = location5;
                employee6.Father = null;
                employee6.HireDate = new DateTime(2003, 1, 1);
                employee6.NHireDate = new DateTime(2003, 1, 1);
                employee6.Manager = employee2;
                employee6.Department = department2;
                objectList.Add(employee6);

                // Create more instances of Person.

                Person person3 = new Person();
                person3.FirstName = "Lovisa";
                person3.LastName = "Idestam";
                person3.Home = location2;
                person3.Father = employee3;
                objectList.Add(person3);

                // Create instances of SalaryEmployee.

                SalaryEmployee salaryEmployee1 = new SalaryEmployee();
                salaryEmployee1.FirstName = "Alexey";
                salaryEmployee1.LastName = "Moiseenko";
                salaryEmployee1.Home = location6;
                salaryEmployee1.Father = null;
                salaryEmployee1.HireDate = new DateTime(2010, 9, 1);
                salaryEmployee1.NHireDate = null;
                salaryEmployee1.Manager = null;
                salaryEmployee1.Department = department3;
                salaryEmployee1.Commission = false;
                salaryEmployee1.NCommission = null;
                salaryEmployee1.SalaryByte = 10;
                salaryEmployee1.NSalaryByte = null;
                salaryEmployee1.SalaryDecimal = 10;
                salaryEmployee1.NSalaryDecimal = null;
                salaryEmployee1.SalaryDouble = 10;
                salaryEmployee1.NSalaryDouble = null;
                salaryEmployee1.SalaryInt16 = 10;
                salaryEmployee1.NSalaryInt16 = null;
                salaryEmployee1.SalaryInt32 = 10;
                salaryEmployee1.NSalaryInt32 = null;
                salaryEmployee1.SalaryInt64 = 10;
                salaryEmployee1.NSalaryInt64 = null;
                salaryEmployee1.SalarySByte = 10;
                salaryEmployee1.NSalarySByte = null;
                salaryEmployee1.SalarySingle = 10;
                salaryEmployee1.NSalarySingle = null;
                salaryEmployee1.SalaryUInt16 = 10;
                salaryEmployee1.NSalaryUInt16 = null;
                salaryEmployee1.SalaryUInt32 = 10;
                salaryEmployee1.NSalaryUInt32 = null;
                salaryEmployee1.SalaryUInt64 = 10;
                salaryEmployee1.NSalaryUInt64 = null;
                objectList.Add(salaryEmployee1);

                SalaryEmployee salaryEmployee2 = new SalaryEmployee();
                salaryEmployee2.FirstName = "Petros";
                salaryEmployee2.LastName = null;
                salaryEmployee2.Home = null;
                salaryEmployee2.Father = null;
                salaryEmployee2.HireDate = new DateTime(2010, 10, 1);
                salaryEmployee2.NHireDate = new DateTime(2010, 10, 1);
                salaryEmployee2.Manager = employee2;
                salaryEmployee2.Department = department1;
                salaryEmployee2.Commission = false;
                salaryEmployee2.NCommission = false;
                salaryEmployee2.SalaryByte = 20;
                salaryEmployee2.NSalaryByte = 20;
                salaryEmployee2.SalaryDecimal = 20;
                salaryEmployee2.NSalaryDecimal = 20;
                salaryEmployee2.SalaryDouble = 20;
                salaryEmployee2.NSalaryDouble = 20;
                salaryEmployee2.SalaryInt16 = 20;
                salaryEmployee2.NSalaryInt16 = 20;
                salaryEmployee2.SalaryInt32 = 20;
                salaryEmployee2.NSalaryInt32 = 20;
                salaryEmployee2.SalaryInt64 = 20;
                salaryEmployee2.NSalaryInt64 = 20;
                salaryEmployee2.SalarySByte = 20;
                salaryEmployee2.NSalarySByte = 20;
                salaryEmployee2.SalarySingle = 20;
                salaryEmployee2.NSalarySingle = 20;
                salaryEmployee2.SalaryUInt16 = 20;
                salaryEmployee2.NSalaryUInt16 = 20;
                salaryEmployee2.SalaryUInt32 = 20;
                salaryEmployee2.NSalaryUInt32 = 20;
                salaryEmployee2.SalaryUInt64 = 20;
                salaryEmployee2.NSalaryUInt64 = 20;
                objectList.Add(salaryEmployee2);

                SalaryEmployee salaryEmployee3 = new SalaryEmployee();
                salaryEmployee3.FirstName = "Andreas";
                salaryEmployee3.LastName = "Thyrhaug";
                salaryEmployee3.Home = null;
                salaryEmployee3.Father = null;
                salaryEmployee3.HireDate = new DateTime(2010, 12, 13);
                salaryEmployee3.NHireDate = new DateTime(2010, 12, 13);
                salaryEmployee3.Manager = employee3;
                salaryEmployee3.Department = department2;
                salaryEmployee3.Commission = true;
                salaryEmployee3.NCommission = true;
                salaryEmployee3.SalaryByte = 30;
                salaryEmployee3.NSalaryByte = 30;
                salaryEmployee3.SalaryDecimal = 30;
                salaryEmployee3.NSalaryDecimal = 30;
                salaryEmployee3.SalaryDouble = 30;
                salaryEmployee3.NSalaryDouble = 30;
                salaryEmployee3.SalaryInt16 = 30;
                salaryEmployee3.NSalaryInt16 = 30;
                salaryEmployee3.SalaryInt32 = 30;
                salaryEmployee3.NSalaryInt32 = 30;
                salaryEmployee3.SalaryInt64 = 30;
                salaryEmployee3.NSalaryInt64 = 30;
                salaryEmployee3.SalarySByte = 30;
                salaryEmployee3.NSalarySByte = 30;
                salaryEmployee3.SalarySingle = 30;
                salaryEmployee3.NSalarySingle = 30;
                salaryEmployee3.SalaryUInt16 = 30;
                salaryEmployee3.NSalaryUInt16 = 30;
                salaryEmployee3.SalaryUInt32 = 30;
                salaryEmployee3.NSalaryUInt32 = 30;
                salaryEmployee3.SalaryUInt64 = 30;
                salaryEmployee3.NSalaryUInt64 = 30;
                objectList.Add(salaryEmployee3);

                // Create instances of SqlTest.Test1b.Person (another namespace).

                SQLTest.Test1b.Person person = new SQLTest.Test1b.Person();
                person.FirstName = "Adam";
                person._LastName = "Adamsson";
                objectList.Add(person);
            });
        }

        public static void DeleteData()
        {
            Db.Transaction(delegate
            {
                foreach (IObjectView obj in objectList)
                {
                    if (obj != null)
                        obj.Delete();
                }
                objectList.Clear();
            });
        }

        /// <summary>
        /// Creates indexes for SqlTest2, which USES APPROPRIATE SIMPLE (NOT COMBINED) ASCENDING INDEXES.
        /// </summary>
        /// <returns>Returns true if no exceptions happened</returns>
        public static bool CreateIndexes()
        {
            // Index on Enum property ASC.
            Db.SlowSQL("create index Location_Type on SQLTest.EmployeeDb.Location (Type)");
            // Index on Nullable Enum property ASC.
            Db.SlowSQL("create index Location_NType on SqlTest.EmployeeDb.Location (NType)");
            // Index on Binary property. DOES NOT WORK!
            Db.SlowSQL("create index Department_IdBinary on SqlTest.EmployeeDb.Department (IdBinary)");
            // Index on Object property ASC.
            Db.SlowSQL("create index Employee_Department on SqlTest.EmployeeDb.Employee (Department)");
            // Index on DateTime property ASC.
            Db.SlowSQL("create index Employee_HireDate on SqlTest.EmployeeDb.Employee (HireDate)");
            // Index on Boolean property ASC.
            Db.SlowSQL("create index SalaryEmployee_Commission on SqlTest.EmployeeDb.SalaryEmployee (Commission)");
            // Index on Nullable Boolean property ASC.
            Db.SlowSQL("create index SalaryEmployee_NCommission on SqlTest.EmployeeDb.SalaryEmployee (NCommission)");
            // Index on Byte property ASC.
            Db.SlowSQL("create index SalaryEmployee_SalaryByte on SqlTest.EmployeeDb.SalaryEmployee (SalaryByte)");
            // Index on Nullable Byte property ASC.
            Db.SlowSQL("create index SalaryEmployee_NSalaryByte on SqlTest.EmployeeDb.SalaryEmployee (NSalaryByte)");
            // Index on DateTime property ASC.
            Db.SlowSQL("create index SalaryEmployee_HireDate on SqlTest.EmployeeDb.SalaryEmployee (HireDate)");
            // Index on Nullable DateTime property ASC.
            Db.SlowSQL("create index SalaryEmployee_NHireDate on SqlTest.EmployeeDb.SalaryEmployee (NHireDate)");
            // Index on Decimal property ASC.
            Db.SlowSQL("create index SalaryEmployee_SalaryDecimal on SqlTest.EmployeeDb.SalaryEmployee (SalaryDecimal)");
            // Index on Nullable Decimal property ASC.
            Db.SlowSQL("create index SalaryEmployee_NSalaryDecimal on SqlTest.EmployeeDb.SalaryEmployee (NSalaryDecimal)");
            // Index on Int16 property ASC.
            Db.SlowSQL("create index SalaryEmployee_SalaryInt16 on SqlTest.EmployeeDb.SalaryEmployee (SalaryInt16)");
            // Index on Nullable Int16 property ASC.
            Db.SlowSQL("create index SalaryEmployee_NSalaryInt16 on SqlTest.EmployeeDb.SalaryEmployee (NSalaryInt16)");
            // Index on Int32 property ASC.
            Db.SlowSQL("create index SalaryEmployee_SalaryInt32 on SqlTest.EmployeeDb.SalaryEmployee (SalaryInt32)");
            // Index on Nullable Int32 property ASC.
            Db.SlowSQL("create index SalaryEmployee_NSalaryInt32 on SqlTest.EmployeeDb.SalaryEmployee (NSalaryInt32)");
            // Index on Int64 property ASC.
            Db.SlowSQL("create index SalaryEmployee_SalaryInt64 on SqlTest.EmployeeDb.SalaryEmployee (SalaryInt64)");
            // Index on Nullable Int64 property ASC.
            Db.SlowSQL("create index SalaryEmployee_NSalaryInt64 on SqlTest.EmployeeDb.SalaryEmployee (NSalaryInt64)");
            // Index on Object property ASC.
            Db.SlowSQL("create index SalaryEmployee_Manager on SqlTest.EmployeeDb.SalaryEmployee (Manager)");
            // Index on SByte property ASC.
            Db.SlowSQL("create index SalaryEmployee_SalarySByte on SqlTest.EmployeeDb.SalaryEmployee (SalarySByte)");
            // Index on Nullable SByte property ASC.
            Db.SlowSQL("create index SalaryEmployee_NSalarySByte on SqlTest.EmployeeDb.SalaryEmployee (NSalarySByte)");
            // Index on String property ASC.
            Db.SlowSQL("create index SalaryEmployee_LastName on SqlTest.EmployeeDb.SalaryEmployee (LastName)");
            // Index on UInt16 property ASC.
            Db.SlowSQL("create index SalaryEmployee_SalaryUInt16 on SqlTest.EmployeeDb.SalaryEmployee (SalaryUInt16)");
            // Index on Nullable UInt16 property ASC.
            Db.SlowSQL("create index SalaryEmployee_NSalaryUInt16 on SqlTest.EmployeeDb.SalaryEmployee (NSalaryUInt16)");
            // Index on UInt32 property ASC.
            Db.SlowSQL("create index SalaryEmployee_SalaryUInt32 on SqlTest.EmployeeDb.SalaryEmployee (SalaryUInt32)");
            // Index on Nullable UInt32 property ASC.
            Db.SlowSQL("create index SalaryEmployee_NSalaryUInt32 on SqlTest.EmployeeDb.SalaryEmployee (NSalaryUInt32)");
            // Index on UInt64 property ASC.
            Db.SlowSQL("create index SalaryEmployee_SalaryUInt64 on SqlTest.EmployeeDb.SalaryEmployee (SalaryUInt64)");
            // Index on Nullable UInt64 property ASC.
            Db.SlowSQL("create index SalaryEmployee_NSalaryUInt64 on SqlTest.EmployeeDb.SalaryEmployee (NSalaryUInt64)");
            return true;
        }

        /// <summary>
        /// Drop indexes created for SqlTest2.
        /// </summary>
        /// <returns>Returns true if no exceptions happened</returns>
        public static bool DropIndexes()
        {
            Db.SlowSQL("drop index Location_Type on SqlTest.EmployeeDb.Location");
            Db.SlowSQL("drop index Location_NType on SqlTest.EmployeeDb.Location");
            Db.SlowSQL("drop index Department_IdBinary on SqlTest.EmployeeDb.Department");
            Db.SlowSQL("drop index Employee_Department on SqlTest.EmployeeDb.Employee");
            Db.SlowSQL("drop index Employee_HireDate on SqlTest.EmployeeDb.Employee");
            Db.SlowSQL("drop index SalaryEmployee_Commission on SqlTest.EmployeeDb.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_NCommission on SqlTest.EmployeeDb.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_SalaryByte on SqlTest.EmployeeDb.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_NSalaryByte on SqlTest.EmployeeDb.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_HireDate on SqlTest.EmployeeDb.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_NHireDate on SqlTest.EmployeeDb.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_SalaryDecimal on SqlTest.EmployeeDb.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_NSalaryDecimal on SqlTest.EmployeeDb.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_SalaryInt16 on SqlTest.EmployeeDb.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_NSalaryInt16 on SqlTest.EmployeeDb.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_SalaryInt32 on SqlTest.EmployeeDb.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_NSalaryInt32 on SqlTest.EmployeeDb.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_SalaryInt64 on SqlTest.EmployeeDb.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_NSalaryInt64 on SqlTest.EmployeeDb.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_Manager on SqlTest.EmployeeDb.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_SalarySByte on SqlTest.EmployeeDb.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_NSalarySByte on SqlTest.EmployeeDb.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_LastName on SqlTest.EmployeeDb.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_SalaryUInt16 on SqlTest.EmployeeDb.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_nsalaryUInt16 on SqlTest.EmployeeDb.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_SalaryUInt32 on SqlTest.EmployeeDb.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_NSalaryUInt32 on SqlTest.EmployeeDb.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_SalaryUInt64 on SqlTest.EmployeeDb.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_NSalaryUInt64 on SqlTest.EmployeeDb.SalaryEmployee");
            return true;
        }
    }
}
