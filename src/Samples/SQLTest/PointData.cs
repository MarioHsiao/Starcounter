using System;
using System.Collections.Generic;
using Starcounter;

namespace SQLTest.Test2
{
    public static class Indexes
    {
    }
}

namespace SQLTest.PointDb
{
    public static class PointData
    {
        static List<object> objectList = new List<object>();

        public static void CreateData()
        {
            Db.Transaction(delegate
            {
                // Control that data is not already created.
                if (Db.SQL("select p from IntegerPoint p").First != null)
                    return;

                // Create instances of IntegerPoint.

                IntegerPoint point1 = IntegerPoint.Init(null, null, null);
                objectList.Add(point1);
                IntegerPoint point2 = IntegerPoint.Init(null, null, 1);
                objectList.Add(point2);
                IntegerPoint point3 = IntegerPoint.Init(null, null, Int64.MaxValue);
                objectList.Add(point3);

                IntegerPoint point4 = IntegerPoint.Init(null, 1, null);
                objectList.Add(point4);
                IntegerPoint point5 = IntegerPoint.Init(null, 1, 1);
                objectList.Add(point5);
                IntegerPoint point6 = IntegerPoint.Init(null, 1, Int64.MaxValue);
                objectList.Add(point6);

                IntegerPoint point7 = IntegerPoint.Init(null, Int64.MaxValue, null);
                objectList.Add(point7);
                IntegerPoint point8 = IntegerPoint.Init(null, Int64.MaxValue, 1);
                objectList.Add(point8);
                IntegerPoint point9 = IntegerPoint.Init(null, Int64.MaxValue, Int64.MaxValue);
                objectList.Add(point9);

                IntegerPoint point10 = IntegerPoint.Init(1, null, null);
                objectList.Add(point10);
                IntegerPoint point11 = IntegerPoint.Init(1, null, 1);
                objectList.Add(point11);
                IntegerPoint point12 = IntegerPoint.Init(1, null, Int64.MaxValue);
                objectList.Add(point12);

                IntegerPoint point13 = IntegerPoint.Init(1, 1, null);
                objectList.Add(point13);
                IntegerPoint point14 = IntegerPoint.Init(1, 1, 1);
                objectList.Add(point14);
                IntegerPoint point15 = IntegerPoint.Init(1, 1, Int64.MaxValue);
                objectList.Add(point15);

                IntegerPoint point16 = IntegerPoint.Init(1, Int64.MaxValue, null);
                objectList.Add(point16);
                IntegerPoint point17 = IntegerPoint.Init(1, Int64.MaxValue, 1);
                objectList.Add(point17);
                IntegerPoint point18 = IntegerPoint.Init(1, Int64.MaxValue, Int64.MaxValue);
                objectList.Add(point18);

                IntegerPoint point19 = IntegerPoint.Init(Int64.MaxValue, null, null);
                objectList.Add(point19);
                IntegerPoint point20 = IntegerPoint.Init(Int64.MaxValue, null, 1);
                objectList.Add(point20);
                IntegerPoint point21 = IntegerPoint.Init(Int64.MaxValue, null, Int64.MaxValue);
                objectList.Add(point21);

                IntegerPoint point22 = IntegerPoint.Init(Int64.MaxValue, 1, null);
                objectList.Add(point22);
                IntegerPoint point23 = IntegerPoint.Init(Int64.MaxValue, 1, 1);
                objectList.Add(point23);
                IntegerPoint point24 = IntegerPoint.Init(Int64.MaxValue, 1, Int64.MaxValue);
                objectList.Add(point24);

                IntegerPoint point25 = IntegerPoint.Init(Int64.MaxValue, Int64.MaxValue, null);
                objectList.Add(point25);
                IntegerPoint point26 = IntegerPoint.Init(Int64.MaxValue, Int64.MaxValue, 1);
                objectList.Add(point26);
                IntegerPoint point27 = IntegerPoint.Init(Int64.MaxValue, Int64.MaxValue, Int64.MaxValue);
                objectList.Add(point27);
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
        /// Creates indexes for SqlTest3, which USES DIFFERENT COMBINED INDEXES.
        /// </summary>
        /// <returns>Returns true if no exceptions happened</returns>
        public static bool CreateIndexes()
        {
            // Combined index on Nullable Int64 property ASC, Nullable Int64 property ASC and Nullable Int64 property ASC.
            Db.SlowSQL("create index IntegerPoint_X_Y_Z on SqlTest.PointDb.IntegerPoint (X, Y, Z)");
            // Combined index on Nullable Int64 property ASC, Nullable Int64 property ASC and Nullable Int64 property DESC.
            Db.SlowSQL("create index IntegerPoint_X_Y_ZDESC on SqlTest.PointDb.IntegerPoint (X, Y, Z desc)");
            // Combined index on Nullable Int64 property ASC, Nullable Int64 property DESC and Nullable Int64 property ASC.
            Db.SlowSQL("create index IntegerPoint_X_YDESC_Z on SqlTest.PointDb.IntegerPoint (X, Y desc, Z)");
            // Combined index on Nullable Int64 property ASC, Nullable Int64 property DESC and Nullable Int64 property DESC.
            Db.SlowSQL("create index IntegerPoint_X_YDESC_ZDESC on SqlTest.PointDb.IntegerPoint (X, Y desc, Z desc)");
            // Combined index on Nullable Int64 property DESC, Nullable Int64 property ASC and Nullable Int64 property ASC.
            Db.SlowSQL("create index IntegerPoint_XDESC_Y_Z on SqlTest.PointDb.IntegerPoint (X desc, Y, Z)");
            // Combined index on Nullable Int64 property DESC, Nullable Int64 property ASC and Nullable Int64 property DESC.
            Db.SlowSQL("create index IntegerPoint_XDESC_Y_ZDESC on SqlTest.PointDb.IntegerPoint (X desc, Y, Z desc)");
            // Combined index on Nullable Int64 property DESC, Nullable Int64 property DESC and Nullable Int64 property ASC.
            Db.SlowSQL("create index IntegerPoint_XDESC_YDESC_Z on SqlTest.PointDb.IntegerPoint (X desc, Y desc, Z)");
            // Combined index on Nullable Int64 property DESC, Nullable Int64 property DESC and Nullable Int64 property DESC.
            Db.SlowSQL("create index IntegerPoint_XDESC_YDESC_ZDESC on SqlTest.PointDb.IntegerPoint (X desc, Y desc, Z desc)");
            return true;
        }
 
            /// <summary>
        /// Drop indexes created for SqlTest2.
        /// </summary>
        /// <returns>Returns true if no exceptions happened</returns>
        public static bool DropIndexes()
        {
            Db.SlowSQL("drop index IntegerPoint_X_Y_Z on SqlTest.PointDb.IntegerPoint");
            Db.SlowSQL("drop index IntegerPoint_X_Y_ZDESC on SqlTest.PointDb.IntegerPoint");
            Db.SlowSQL("drop index IntegerPoint_X_YDESC_Z on SqlTest.PointDb.IntegerPoint");
            Db.SlowSQL("drop index IntegerPoint_X_YDESC_ZDESC on SqlTest.PointDb.IntegerPoint");
            Db.SlowSQL("drop index IntegerPoint_XDESC_Y_Z on SqlTest.PointDb.IntegerPoint");
            Db.SlowSQL("drop index IntegerPoint_XDESC_Y_ZDESC on SqlTest.PointDb.IntegerPoint");
            Db.SlowSQL("drop index IntegerPoint_XDESC_YDESC_Z on SqlTest.PointDb.IntegerPoint");
            Db.SlowSQL("drop index IntegerPoint_XDESC_YDESC_ZDESC on SqlTest.PointDb.IntegerPoint");
            return true;
        }
}
}