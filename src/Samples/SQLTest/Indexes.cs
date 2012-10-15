using System;
using Starcounter;

namespace SqlTest.Test2
{
    public static class Indexes
    {
        /// <summary>
        /// Creates indexes for SqlTest2, which USES APPROPRIATE SIMPLE (NOT COMBINED) ASCENDING INDEXES.
        /// </summary>
        /// <returns>Returns true if no exceptions happened</returns>
        public static bool CreateIndexes()
        {
            // Index on Enum property ASC.
            Db.SlowSQL("create index Location_Type on SqlTest.Test1.Location (Type)");
            // Index on Nullable Enum property ASC.
            Db.SlowSQL("create index Location_NType on SqlTest.Test1.Location (NType)");
            // Index on Binary property. DOES NOT WORK!
            Db.SlowSQL("create index Department_IdBinary on SqlTest.Test1.Department (IdBinary)");
            // Index on Object property ASC.
            Db.SlowSQL("create index Employee_Department on SqlTest.Test1.Employee (Department)");
            // Index on Boolean property ASC.
            Db.SlowSQL("create index SalaryEmployee_Commission on SqlTest.Test1.SalaryEmployee (Commission)");
            // Index on Nullable Boolean property ASC.
            Db.SlowSQL("create index SalaryEmployee_NCommission on SqlTest.Test1.SalaryEmployee (NCommission)");
            // Index on Byte property ASC.
            Db.SlowSQL("create index SalaryEmployee_SalaryByte on SqlTest.Test1.SalaryEmployee (SalaryByte)");
            // Index on Nullable Byte property ASC.
            Db.SlowSQL("create index SalaryEmployee_NSalaryByte on SqlTest.Test1.SalaryEmployee (NSalaryByte)");
            // Index on DateTime property ASC.
            Db.SlowSQL("create index SalaryEmployee_HireDate on SqlTest.Test1.SalaryEmployee (HireDate)");
            // Index on Nullable DateTime property ASC.
            Db.SlowSQL("create index SalaryEmployee_NHireDate on SqlTest.Test1.SalaryEmployee (NHireDate)");
            // Index on Decimal property ASC.
            Db.SlowSQL("create index SalaryEmployee_SalaryDecimal on SqlTest.Test1.SalaryEmployee (SalaryDecimal)");
            // Index on Nullable Decimal property ASC.
            Db.SlowSQL("create index SalaryEmployee_NSalaryDecimal on SqlTest.Test1.SalaryEmployee (NSalaryDecimal)");
            // Index on Int16 property ASC.
            Db.SlowSQL("create index SalaryEmployee_SalaryInt16 on SqlTest.Test1.SalaryEmployee (SalaryInt16)");
            // Index on Nullable Int16 property ASC.
            Db.SlowSQL("create index SalaryEmployee_NSalaryInt16 on SqlTest.Test1.SalaryEmployee (NSalaryInt16)");
            // Index on Int32 property ASC.
            Db.SlowSQL("create index SalaryEmployee_SalaryInt32 on SqlTest.Test1.SalaryEmployee (SalaryInt32)");
            // Index on Nullable Int32 property ASC.
            Db.SlowSQL("create index SalaryEmployee_NSalaryInt32 on SqlTest.Test1.SalaryEmployee (NSalaryInt32)");
            // Index on Int64 property ASC.
            Db.SlowSQL("create index SalaryEmployee_SalaryInt64 on SqlTest.Test1.SalaryEmployee (SalaryInt64)");
            // Index on Nullable Int64 property ASC.
            Db.SlowSQL("create index SalaryEmployee_NSalaryInt64 on SqlTest.Test1.SalaryEmployee (NSalaryInt64)");
            // Index on Object property ASC.
            Db.SlowSQL("create index SalaryEmployee_Manager on SqlTest.Test1.SalaryEmployee (Manager)");
            // Index on SByte property ASC.
            Db.SlowSQL("create index SalaryEmployee_SalarySByte on SqlTest.Test1.SalaryEmployee (SalarySByte)");
            // Index on Nullable SByte property ASC.
            Db.SlowSQL("create index SalaryEmployee_NSalarySByte on SqlTest.Test1.SalaryEmployee (NSalarySByte)");
            // Index on String property ASC.
            Db.SlowSQL("create index SalaryEmployee_LastName on SqlTest.Test1.SalaryEmployee (LastName)");
            // Index on UInt16 property ASC.
            Db.SlowSQL("create index SalaryEmployee_SalaryUInt16 on SqlTest.Test1.SalaryEmployee (SalaryUInt16)");
            // Index on Nullable UInt16 property ASC.
            Db.SlowSQL("create index SalaryEmployee_NSalaryUInt16 on SqlTest.Test1.SalaryEmployee (NSalaryUInt16)");
            // Index on UInt32 property ASC.
            Db.SlowSQL("create index SalaryEmployee_SalaryUInt32 on SqlTest.Test1.SalaryEmployee (SalaryUInt32)");
            // Index on Nullable UInt32 property ASC.
            Db.SlowSQL("create index SalaryEmployee_NSalaryUInt32 on SqlTest.Test1.SalaryEmployee (NSalaryUInt32)");
            // Index on UInt64 property ASC.
            Db.SlowSQL("create index SalaryEmployee_SalaryUInt64 on SqlTest.Test1.SalaryEmployee (SalaryUInt64)");
            // Index on Nullable UInt64 property ASC.
            Db.SlowSQL("create index SalaryEmployee_NSalaryUInt64 on SqlTest.Test1.SalaryEmployee (NSalaryUInt64)");
            return true;
        }

        /// <summary>
        /// Drop indexes created for SqlTest2.
        /// </summary>
        /// <returns>Returns true if no exceptions happened</returns>
        public static bool DropIndexes()
        {
            Db.SlowSQL("drop index Location_Type on SqlTest.Test1.Location");
            Db.SlowSQL("drop index Location_NType on SqlTest.Test1.Location");
            Db.SlowSQL("drop index Department_IdBinary on SqlTest.Test1.Department");
            Db.SlowSQL("drop index Employee_Department on SqlTest.Test1.Employee");
            Db.SlowSQL("drop index SalaryEmployee_Commission on SqlTest.Test1.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_NCommission on SqlTest.Test1.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_SalaryByte on SqlTest.Test1.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_NSalaryByte on SqlTest.Test1.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_HireDate on SqlTest.Test1.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_NHireDate on SqlTest.Test1.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_SalaryDecimal on SqlTest.Test1.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_NSalaryDecimal on SqlTest.Test1.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_SalaryInt16 on SqlTest.Test1.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_NSalaryInt16 on SqlTest.Test1.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_SalaryInt32 on SqlTest.Test1.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_NSalaryInt32 on SqlTest.Test1.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_SalaryInt64 on SqlTest.Test1.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_NSalaryInt64 on SqlTest.Test1.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_Manager on SqlTest.Test1.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_SalarySByte on SqlTest.Test1.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_NSalarySByte on SqlTest.Test1.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_LastName on SqlTest.Test1.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_SalaryUInt16 on SqlTest.Test1.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_NSalaryUInt16 on SqlTest.Test1.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_SalaryUInt32 on SqlTest.Test1.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_NSalaryUInt32 on SqlTest.Test1.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_SalaryUInt64 on SqlTest.Test1.SalaryEmployee");
            Db.SlowSQL("drop index SalaryEmployee_NSalaryUInt64 on SqlTest.Test1.SalaryEmployee");
            return true;
        }
    }
}

namespace SqlTest.Test3
{
    public static class Indexes
    {
        /// <summary>
        /// Creates indexes for SqlTest3, which USES DIFFERENT COMBINED INDEXES.
        /// </summary>
        /// <returns>Returns true if no exceptions happened</returns>
        public static bool CreateIndexes()
        {
            // Combined index on Nullable Int64 property ASC, Nullable Int64 property ASC and Nullable Int64 property ASC.
            Db.SlowSQL("create index IntegerPoint_X_Y_Z on SqlTest.Test3.IntegerPoint (X, Y, Z)");
            // Combined index on Nullable Int64 property ASC, Nullable Int64 property ASC and Nullable Int64 property DESC.
            Db.SlowSQL("create index IntegerPoint_X_Y_ZDESC on SqlTest.Test3.IntegerPoint (X, Y, Z desc)");
            // Combined index on Nullable Int64 property ASC, Nullable Int64 property DESC and Nullable Int64 property ASC.
            Db.SlowSQL("create index IntegerPoint_X_YDESC_Z on SqlTest.Test3.IntegerPoint (X, Y desc, Z)");
            // Combined index on Nullable Int64 property ASC, Nullable Int64 property DESC and Nullable Int64 property DESC.
            Db.SlowSQL("create index IntegerPoint_X_YDESC_ZDESC on SqlTest.Test3.IntegerPoint (X, Y desc, Z desc)");
            // Combined index on Nullable Int64 property DESC, Nullable Int64 property ASC and Nullable Int64 property ASC.
            Db.SlowSQL("create index IntegerPoint_XDESC_Y_Z on SqlTest.Test3.IntegerPoint (X desc, Y, Z)");
            // Combined index on Nullable Int64 property DESC, Nullable Int64 property ASC and Nullable Int64 property DESC.
            Db.SlowSQL("create index IntegerPoint_XDESC_Y_ZDESC on SqlTest.Test3.IntegerPoint (X desc, Y, Z desc)");
            // Combined index on Nullable Int64 property DESC, Nullable Int64 property DESC and Nullable Int64 property ASC.
            Db.SlowSQL("create index IntegerPoint_XDESC_YDESC_Z on SqlTest.Test3.IntegerPoint (X desc, Y desc, Z)");
            // Combined index on Nullable Int64 property DESC, Nullable Int64 property DESC and Nullable Int64 property DESC.
            Db.SlowSQL("create index IntegerPoint_XDESC_YDESC_ZDESC on SqlTest.Test3.IntegerPoint (X desc, Y desc, Z desc)");
            return true;
        }
 
            /// <summary>
        /// Drop indexes created for SqlTest2.
        /// </summary>
        /// <returns>Returns true if no exceptions happened</returns>
        public static bool DropIndexes()
        {
            Db.SlowSQL("drop index IntegerPoint_X_Y_Z on SqlTest.Test3.IntegerPoint (X, Y, Z)");
            Db.SlowSQL("drop index IntegerPoint_X_Y_ZDESC on SqlTest.Test3.IntegerPoint (X, Y, Z desc)");
            Db.SlowSQL("drop index IntegerPoint_X_YDESC_Z on SqlTest.Test3.IntegerPoint (X, Y desc, Z)");
            Db.SlowSQL("drop index IntegerPoint_X_YDESC_ZDESC on SqlTest.Test3.IntegerPoint (X, Y desc, Z desc)");
            Db.SlowSQL("drop index IntegerPoint_XDESC_Y_Z on SqlTest.Test3.IntegerPoint (X desc, Y, Z)");
            Db.SlowSQL("drop index IntegerPoint_XDESC_Y_ZDESC on SqlTest.Test3.IntegerPoint (X desc, Y, Z desc)");
            Db.SlowSQL("drop index IntegerPoint_XDESC_YDESC_Z on SqlTest.Test3.IntegerPoint (X desc, Y desc, Z)");
            Db.SlowSQL("drop index IntegerPoint_XDESC_YDESC_ZDESC on SqlTest.Test3.IntegerPoint (X desc, Y desc, Z desc)");
            return true;
        }
}
}