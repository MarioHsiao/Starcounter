using System;
using Starcounter;

namespace SQLTest.InheritedDb {
    public static class InheritedData {
        internal static int nrPersons = 0;
        internal static int nrEmployees = 0;
        internal static int nrStudents = 5;
        internal static int nrManagers = 2;
        internal static int nrTeachers = 4;
        internal static int nrProfessors = 2;

        internal static int TotalTeachers { get { return nrProfessors + nrTeachers; } }
        internal static int TotalEmployees { get { return TotalTeachers + nrManagers; } }
        internal static int TotoalPersons { get { return TotalEmployees + nrStudents; } }

        internal static void CreateIndexes() {
            Db.SlowSQL("CREATE unique INDEX personName ON Person (Name)");
            Db.SlowSQL("CREATE INDEX teacherName ON IndexQueryTest.InheritedIndex.Teacher (Name)");
            Db.SlowSQL("CREATE INDEX personBirthdayGender ON Person (Birthday, Gender)");
            Db.SlowSQL("CREATE INDEX companyIndx ON Employee (Company)");
            Db.SlowSQL("CREATE INDEX companyIndx On Professor (Company)");
            Db.SlowSQL("CREATE INDEX employeeBoss ON Employee (Boss)");
            Db.SlowSQL("create index professorBoss ON Professor (Boss)");
        }

        internal static void DropIndexes() {
            CallDropIndex("DROP INDEX personName ON Person");
            CallDropIndex("DROP INDEX teacherName ON IndexQueryTest.InheritedIndex.Teacher");
            CallDropIndex("DROP INDEX personBirthdayGender ON Person");
            CallDropIndex("DROP INDEX companyIndx ON Employee");
            CallDropIndex("DROP INDEX companyIndx On Professor");
            CallDropIndex("DROP INDEX employeeBoss ON Employee");
            CallDropIndex("DROP index professorBoss ON Professor");
        }

        internal static void CallDropIndex(String statement) {
            try {
                Db.SlowSQL(statement);
            } catch (DbException e) {
                if (e.ErrorCode != Starcounter.Error.SCERRINDEXNOTFOUND)
                    throw e;
            }
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

        internal static void Populate() {
            // Create persons, employees, teachers, professors, students and managers
            for (int i = 0; i < nrPersons; i++)
                new Person { Name = "Person" + i, Birthday = new DateTime(1950 + i * 5, i % 12 + 1, i + 1), Gender = i % 2 };
            Employer employer = new Employer { Address = "Here" };
            for (int i = 0; i < nrManagers; i++)
                new Manager { Name = "Manager" + i, Birthday = new DateTime(1970 + i * 3, i + 1, i + 1), Gender = i % 2, Bonus = i, Company = employer };
            for (int i = 0; i < nrEmployees; i++)
                new Employee { Name = "Employee" + i, Birthday = new DateTime(1970 + i * 4, i + 1, i + 1), Gender = i % 2, Company = employer };
            for (int i = 0; i < nrProfessors; i++)
                new Professor { Name = "Professor" + i, Birthday = new DateTime(1950 + i * 1, i + 1, i + 1), Gender = i % 2, Company = employer };
            for (int i = 0; i < nrTeachers; i++)
                new Teacher { Name = "Teacher" + i, Birthday = new DateTime(1975 + i * 2, i + 1, i + 1), Gender = i % 2, Company = employer };
            for (int i = 0; i < nrStudents; i++)
                new Student { Name = "Student" + i, Birthday = new DateTime(1980 + i, i + 1, i + 1), Gender = i % 2 };
        }
    }
}
