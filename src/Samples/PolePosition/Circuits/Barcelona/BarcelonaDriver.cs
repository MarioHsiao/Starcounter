using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Poleposition.Framework;
using System.Diagnostics;

namespace Starcounter.Poleposition.Circuits.Barcelona
{
/// <summary>
/// PP description: "writes, reads, queries and deletes objects with a 5 level inheritance structure"
/// </summary>
[Driver("Barcelona")]
public class BarcelonaDriver : Driver
{
    private static readonly string SelectAllBarcelona4 = "SELECT b FROM " + typeof(Barcelona4).FullName + " b";

    private static readonly string SelectBarcelona4ByField2 =
        "SELECT b FROM " + typeof(Barcelona4).FullName + " b WHERE b.Field2 = ?";

    public BarcelonaDriver(Setup s) : base(s)
    {
    }

    #region Common driver stuff

    public override void TakeSeatIn()
    {
        using (Transaction transaction = Transaction.NewCurrent())
        {
            // NOTE:
            //
            // If we were to DeleteAllOfType<Barcelona0>(), we get a DbException
            // because Barcelona0 doesn't have any fields indexed. Since Barcelona4
            // is the only type actually instantiated (indeed, the only one which
            // CAN be instantiated as the supertypes are abstract -- a change made
            // when adding the h4x indices), DeleteAllOfType<Barcelona2>() is
            // sufficient (even though it hurts a little knowing that someone COULD
            // actually add a new, concrete subclass somewhere...).
            Starcounter.Poleposition.Util.TypeDeleter.DeleteAllOfType<Barcelona2>();
            transaction.Commit();
        }
    }

    #endregion

    #region Laps

    [Lap("Write")]
    public void LapWrite()
    {
        using (Transaction transaction = Transaction.NewCurrent())
        {
            int objectCount = Setup.ObjectCount;
            for (int i = 1; i <= objectCount; ++i)
            {
                Barcelona4 b4 = new Barcelona4();
                b4.SetAll(i);
            }
            transaction.Commit();
        }
    }

    [Lap("Read")]
    public void LapRead()
    {
        using (Transaction transaction = Transaction.NewCurrent())
        {
            using (SqlEnumerator<Object> sqlResult = (SqlEnumerator<Object>)Db.SQL(SelectAllBarcelona4).GetEnumerator())
            {
                int read = AddResultChecksums(sqlResult);
            }
        }
    }

    [Lap("Query")]
    public void LapQuery()
    {
        using (Transaction transaction = Transaction.NewCurrent())
        {
            int selectCount = Setup.SelectCount;
            for (int i = 1; i <= selectCount; ++i)
            {
                using (SqlEnumerator<Object> sqlResult = (SqlEnumerator<Object>)Db.SQL(SelectBarcelona4ByField2, i).GetEnumerator())
                {
                    AddSingleResultChecksum(sqlResult);
                }
            }
        }
    }

    [Lap("Delete")]
    public void LapDelete()
    {
        using (Transaction transaction = Transaction.NewCurrent())
        {
            using (SqlEnumerator<Object> sqlResult = (SqlEnumerator<Object>)Db.SQL(SelectAllBarcelona4).GetEnumerator())
            {
                int deleted = 0;
                while (sqlResult.MoveNext())
                {
                    ++deleted;
                    (sqlResult.Current as Barcelona4).Delete();
                    AddToCheckSum(5);
                }
            }
            transaction.Commit();
        }
    }

    #endregion

}
}
