using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Poleposition.Framework;
using Starcounter.Poleposition.Util;

namespace Starcounter.Poleposition.Circuits.Melbourne
{
/// <summary>
/// PP description: "writes, reads and deletes unstructured flat objects of one kind in bulk mode"
/// </summary>
[Driver("Melbourne")]
public class MelbourneDriver : Driver
{
    private static readonly string SelectAllPilots = "SELECT p FROM " + typeof(Pilot).FullName + " p";

    public MelbourneDriver(Setup s) : base(s)
    {
    }

    public override void TakeSeatIn()
    {
        using (Transaction transaction = Transaction.NewCurrent())
        {
            TypeDeleter.DeleteAllOfType<Pilot>();
            transaction.Commit();
        }
    }

    [Lap("Write")]
    public void LapWrite()
    {
        using(Transaction transaction = Transaction.NewCurrent())
        {
            for (int i = 1; i <= Setup.ObjectCount; ++i)
            {
                Pilot p = new Pilot();
                p.Name = "Pilot_" + i;
                p.FirstName = "Herkules";
                p.Points = i;
                p.LicenseId = i;
                AddToCheckSum(p);
                if (Setup.IsCommitPoint(i))
                {
                    transaction.Commit();
                }
            }
            transaction.Commit();
        }
    }

    [Lap("Read_hot")]
    public void LapReadHot()
    {
        using (Transaction transaction = Transaction.NewCurrent())
        {
            using (var se = (SqlEnumerator<Object>)Db.SQL(SelectAllPilots).GetEnumerator())
            {
                AddResultChecksums(se);
            }
        }
    }

    [Lap("Read")]
    public void LapRead()
    {
        LapReadHot();
    }

    [Lap("Delete")]
    public void LapDelete()
    {
        while (DoDelete()); // Repeat until everything is deleted.
    }

    /// <summary>
    /// Deletes <c>this.Setup.CommitInterval</c> <see cref="Pilot"/>s.
    /// </summary>
    /// <returns>
    /// <c>true</c> if there are more <see cref="Pilot"/>s left to be deleted;
    /// <c>false</c> if all <see cref="Pilot"/>s have been deleted.
    /// </returns>
    private bool DoDelete()
    {
        using (Transaction transaction = Transaction.NewCurrent())
        {
            try
            {
                using (SqlEnumerator<Object> sqlResult = (SqlEnumerator<Object>)Db.SQL(SelectAllPilots).GetEnumerator())
                {
                    int i = 1;
                    while (sqlResult.MoveNext())
                    {
                        (sqlResult.Current as Pilot).Delete();
                        if (this.Setup.IsCommitPoint(i++))
                        {
                            // If we reach the commit point, and there are more
                            // objects, we want the caller to continue deleting.
                            return sqlResult.MoveNext();
                        }
                    }
                    return false; // Reached end before commit point; no more objects to delete
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
