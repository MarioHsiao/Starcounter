
using System;
using System.Text;
using System.Threading;
using Starcounter;

namespace SQLTesting
{
internal class DataTester
{
    /// <summary>
    /// Check if objects already exist in DB.
    /// </summary>
    public static bool ThereAreObjectsInDB()
    {
        using (Transaction transaction = Transaction.NewCurrent())
        {
            ISqlEnumerator sqlResult = null;
            try
            {
                sqlResult = (ISqlEnumerator)Db.SQL("SELECT c FROM Class0 c").GetEnumerator();

                // If there are instances in the database, consider the example
                // data already created.
                // Hence, calling this method each time in the exercise does
                // not duplicate the data set.
                if (sqlResult.MoveNext())
                {
                    Console.Error.WriteLine("   Objects in database already exist. Not creating anything new.");
                    Console.Error.WriteLine("   (assuming that database contains correct amount of objects).");
                    return true;
                }
            }
            finally
            {
                if (sqlResult != null)
                    sqlResult.Dispose();
            }

            // No suitable objects found, creating DB data from scratch...
            return false;
        }
    }

    /// <summary>
    /// Pressure test on DB workload.
    /// </summary>
    public static void PerformanceTestPerQueryType(Int64 queriesPerTest,
                                                   Int32 queryType,
                                                   Byte schedId,
                                                   Int64[] shuffledArray,
                                                   String[] shuffledArrayString)
    {
        using (Transaction transaction = Transaction.NewCurrent())
        {
            Int64 calcChecksum = 0, artChecksum = 0;
            String query = null;
            if (queryType == 0)
            {
                query = "SELECT c FROM Class0 c WHERE c.prop_int64 = ?";

                //query = "SELECT c FROM Class0 c WHERE c.prop_single = ?";
                //query = "SELECT c FROM Class0 c WHERE c.prop_double = ?";
            }
            else if (queryType == 1)
            {
                query = "SELECT c FROM Class0 c WHERE c.prop_string = ?";
            }
            else if (queryType == 2) // Retrieve whole table.
            {
                query = "SELECT c FROM Class0 c";
            }

            // Printing info if we are running only one scheduler test.
            if (schedId == 255)
                Console.Error.WriteLine("--- Running test for the query: " + query);

            for (Int64 i = 0; i < queriesPerTest; i++)
            {
                //Profiler.Start("Time for Profiler Overhead Estimator.", 13);
                //Profiler.Stop(13);

                using (ISqlEnumerator sqlEnum = (ISqlEnumerator)Db.SQL(query).GetEnumerator())
                {
                    // Selecting what property we would like to fetch.
                    if (queryType == 0)
                    {
                        sqlEnum.SetVariable(0, shuffledArray[i]);

                        //sqlEnum.SetVariable(0, (Double) shuffledArray[i]);
                        //sqlEnum.SetVariable(0, (Single) shuffledArray[i]);
                    }
                    else if (queryType == 1)
                    {
                        sqlEnum.SetVariable(0, shuffledArrayString[i]);
                    }

                    // Fetching only the first result and getting the object checksum.
                    if (sqlEnum.MoveNext())
                    {
                        Class0 curObj = sqlEnum.Current as Class0;

                        // Calculating object's checksum.
                        calcChecksum += curObj.GetCheckSum();

                        /*
                        // Updating the object.
                        curObj.SetValue(i + (UInt64)DateTime.Now.Ticks);

                        // Cloning the instance.
                        Class0 newObj = curObj.Clone();

                        // Deleting the old one.
                        curObj.Delete();
                        */
                    }
                }

                // Calculating artificially correct checksum and checking if both are the same.
                artChecksum += shuffledArray[i];

                if ((artChecksum != calcChecksum) && (queryType < 2))
                {
                    Console.Error.WriteLine("!!! INCONSISTENT checksums ({0}, {1}), quiting...", artChecksum, calcChecksum);
                    break;
                }
            }

            if (queryType < 2) // "One hit" test. 
            {
                if (calcChecksum == artChecksum)
                {
                    if (schedId == 255)
                        Console.Error.WriteLine("   Identical checksums [{0}]", calcChecksum);
                }
                else
                {
                    Console.Error.WriteLine("!!! INCONSISTENT checksums [{0}, {1}], quiting...", artChecksum, calcChecksum);
                }
            }

            // Printing profile results at the end of each test.
            //if (schedId == 255)
            //    Profiler.DrawResults();
        }
    }

    /// <summary>
    /// Used to track created object identifier.
    /// </summary>
    static Int64 globalObjectID = 0;

    /// <summary>
    /// Creates the sample data.
    /// </summary>
    public static void CreateExampleData(UInt64 startingIndex, UInt64 numberOfInstances)
    {
        using(Transaction transaction = Transaction.NewCurrent())
        {
            try
            {
                for (UInt64 i = startingIndex; i < (startingIndex + numberOfInstances); i++)
                {
                    Byte[] binary_data = new Byte[1];
                    binary_data[0] = 0;
                    Class0 class0_instance = new Class0(true,
                                                        (Nullable<SByte>)    (globalObjectID % 127),
                                                        (Nullable<Byte>)     (globalObjectID % 255),
                                                        (Nullable<Int16>)    (globalObjectID % 32767),
                                                        (Nullable<UInt16>)   (globalObjectID % 65535),
                                                        (Nullable<Int32>)    (globalObjectID),
                                                        (Nullable<UInt32>)   (globalObjectID),
                                                        (Nullable<Int64>)    (globalObjectID),
                                                        (Nullable<UInt64>)   (globalObjectID),
                                                        (Nullable<Decimal>)  (globalObjectID * 1.0M),
                                                        (Nullable<Double>)   (globalObjectID * 1.0D),
                                                        (Nullable<Single>)   (globalObjectID * 1.0F),
                                                        new DateTime(globalObjectID * 1L),
                                                        new Binary(binary_data),
                                                        new LargeBinary(binary_data),
                                                        globalObjectID.ToString()
                                                       );

                    // Chopping the data by unique blocks.
                    if ((Startup.NumOfHitsPerQuery == 1) || (((i + 1) % Startup.NumOfHitsPerQuery) == 0))
                        globalObjectID++;
                }
            }
            finally
            {
                transaction.Commit();
            }
        }
    }
}
}