﻿using System;
using System.Collections;
using System.Diagnostics;
using Starcounter;
using Starcounter.Binding;

namespace IndexQueryTest.InheritedIndex {
    static public class InheritedIndexTest {
        internal static int nrPersons = 0;
        internal static int nrEmployees = 0;
        internal static int nrStudents = 5;
        internal static int nrManagers = 2;
        internal static int nrTeachers = 4;
        internal static int nrProfessors = 2;

        internal static int TotalTeachers { get { return nrProfessors + nrTeachers; } }
        internal static int TotalEmployees { get { return TotalTeachers + nrManagers; } }
        internal static int TotoalPersons { get { return TotalEmployees + nrStudents; } }

        public static void RunInheritedIndexTest() {
            DropIndexes();
            CreateIndexes();
            UnitTests();
            DropData();
            Populate();
            TestInheritedIndexes();
        }

        internal static void CreateIndexes() {
            Db.SlowSQL("CREATE unique INDEX personName ON Person (Name)");
            Db.SlowSQL("CREATE INDEX teacherName ON IndexQueryTest.InheritedIndex.Teacher (Name)");
            Db.SlowSQL("CREATE INDEX personBirthdayGender ON Person (Birthday, Gender)");
            Db.SlowSQL("CREATE INDEX employeeCompany ON Employee (Company)");
            Db.SlowSQL("CREATE INDEX professorCompany On Professor (Company)");
            Db.SlowSQL("CREATE INDEX employeeBoss ON Employee (Boss)");
        }

        internal static void DropIndexes() {
            CallDropIndex("DROP INDEX personName ON Person");
            CallDropIndex("DROP INDEX teacherName ON IndexQueryTest.InheritedIndex.Teacher");
            CallDropIndex("DROP INDEX personBirthdayGender ON Person");
            CallDropIndex("DROP INDEX employeeCompany ON Employee");
            CallDropIndex("DROP INDEX professorCompany On Professor");
            CallDropIndex("DROP INDEX employeeBoss ON Employee");
        }

        internal static void CallDropIndex(String statement) {
            try {
                Db.SlowSQL(statement);
            } catch (DbException e) {
                if (e.ErrorCode != Starcounter.Error.SCERRINDEXNOTFOUND)
                    throw e;
            }
        }

        internal static void TestInheritedIndexes() {
            PrintQueryPlan("select p from IndexQueryTest.InheritedIndex.Person p where name = ?"); // indexed
            int nrObjects = 0;
            foreach (Person p in Db.SQL("select p from IndexQueryTest.InheritedIndex.Person p where name = ?", "Student1"))
                nrObjects ++;
            Trace.Assert(nrObjects == 1);
            Employer company = Db.SQL<Employer>("select e from employer e where address =  ?", "Here").First;
            PrintQueryPlan("select e from Employee e where company = ?"); // indexed
            nrObjects = 0;
            foreach (Employee e in Db.SQL("select e from Employee e where company = ?", company))
                nrObjects++;
            Trace.Assert(nrObjects == TotalEmployees);
            nrObjects = 0;
            PrintQueryPlan("select e from teacher e where company = ?"); // use inherited index
            foreach (Employee e in Db.SQL("select e from teacher e where company = ?", company))
                nrObjects++;
            Trace.Assert(nrObjects == TotalTeachers);
            nrObjects = 0;
            PrintQueryPlan("select e from professor e where company = ?"); // indexed
            foreach (Employee e in Db.SQL("select e from professor e where company = ?", company))
                nrObjects++;
            Trace.Assert(nrObjects == nrProfessors);
            nrObjects = 0;
            PrintQueryPlan("select e from teacher e ORDER BY company"); // use inherited index
            foreach (Employee e in Db.SQL("select e from teacher e ORDER BY company"))
                nrObjects++;
            Trace.Assert(nrObjects == TotalTeachers);
            nrObjects = 0;
            PrintQueryPlan("select e from teacher e OPTION INDEX (e employeeCompany)"); // use inherited index
            foreach (Employee e in Db.SQL("select e from teacher e OPTION INDEX (e employeeCompany)"))
                nrObjects++;
            Trace.Assert(nrObjects == TotalTeachers);
        }

        internal static void PrintQueryPlan(String query) {
            Console.WriteLine(((IEnumerator)Db.SQL(query,null).GetEnumerator()).ToString());
        }

        internal static void UnitTests() {
            TestGetAllIndexInfos();
            TestGetInheritedIndexInfo();
        }

        internal static void TestGetAllIndexInfos() {
            IndexInfo[] indexes = Bindings.GetTypeBindingInsensitive("Professor").GetAllInheritedIndexInfos();
            Trace.Assert(indexes.Length == 10);
            // Type Professor
            Trace.Assert(indexes[0].Name == "auto");
            Trace.Assert(indexes[1].Name == "professorCompany");
            // Type Teacher
            Trace.Assert(indexes[2].Name == "auto");
            Trace.Assert(indexes[3].Name == "teacherName");
            // Type Employee
            Trace.Assert(indexes[4].Name == "auto");
            Trace.Assert(indexes[5].Name == "employeeBoss");
            Trace.Assert(indexes[6].Name == "employeeCompany");
            // Type Person
            Trace.Assert(indexes[7].Name == "auto");
            Trace.Assert(indexes[8].Name == "personBirthdayGender");
            Trace.Assert(indexes[9].Name == "personName");
        }

        internal static void TestGetInheritedIndexInfo() {
            IndexInfo indx = Bindings.GetTypeBindingInsensitive("Professor").GetInheritedIndexInfo("professorCompany");
            Trace.Assert(indx != null);
            indx = Bindings.GetTypeBindingInsensitive("Professor").GetInheritedIndexInfo("employeeCompany");
            Trace.Assert(indx != null);
            indx = Bindings.GetTypeBindingInsensitive("Teacher").GetInheritedIndexInfo("employeeCompany");
            Trace.Assert(indx != null);
            indx = Bindings.GetTypeBindingInsensitive("Teacher").GetInheritedIndexInfo("professorCompany");
            Trace.Assert(indx == null);
        }

        internal static void Populate() {
            // Create persons, employees, teachers, professors, students and managers
            for (int i = 0; i < nrPersons; i++)
                new Person { Name = "Person" + i, Birthday = new DateTime(1950 + i * 5, i%12+1, i+1), Gender = i % 2 };
            Employer employer = new Employer { Address = "Here" };
            for (int i = 0; i <nrManagers; i++)
                new Manager { Name = "Manager" + i, Birthday = new DateTime(1970 + i * 3, i+1, i+1), Gender = i % 2, Bonus = i, Company = employer };
            for (int i = 0; i<nrEmployees; i++)
                new Employee { Name = "Employee" + i, Birthday = new DateTime(1970 + i * 4, i+1, i+1), Gender = i % 2, Company = employer };
            for (int i = 0; i < nrProfessors; i++)
                new Professor { Name = "Professor" + i, Birthday = new DateTime(1950 + i * 1, i+1, i+1), Gender = i % 2, Company = employer };
            for (int i = 0; i < nrTeachers; i++)
                new Teacher { Name = "Teacher" + i, Birthday = new DateTime(1975 + i * 2, i+1, i+1), Gender = i % 2, Company = employer };
            for (int i = 0; i <nrStudents; i++)
                new Student { Name = "Student" + i, Birthday = new DateTime(1980 + i, i+1, i+1), Gender = i % 2 };
        }

        internal static void DropData() {
            Db.SlowSQL("DELETE From Student");
            Db.SlowSQL("DELETE From Teacher");
            Db.SlowSQL("DELETE From Professor");
            Db.SlowSQL("DELETE From Employee");
            Db.SlowSQL("DELETE From Manager");
            Db.SlowSQL("DELETE From Employer");
            Db.SlowSQL("DELETE From Person");
        }
    }
}
