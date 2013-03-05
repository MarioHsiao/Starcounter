using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Poleposition.Framework;
using Starcounter.Poleposition.Entrances;

namespace Starcounter.Poleposition.Circuits.Bahrain
{
/// <summary>
/// PP description: "write, query, update and delete simple flat objects individually"
/// </summary>
[Driver("Bahrain")]
public class BahrainDriver : Driver
{
    private static readonly string SelectAllPilots =
        "SELECT p FROM " + typeof(Pilot).FullName + " p";

    private static readonly string SelectPilotByName =
        "SELECT p FROM " + typeof(Pilot).FullName + " p WHERE p.Name = ?";

    private static readonly string SelectPilotByFirstName =
        "SELECT p FROM " + typeof(Pilot).FullName + " p WHERE p.FirstName = ?";

    private static readonly string SelectPilotByLicenseId =
        "SELECT p FROM " + typeof(Pilot).FullName + " p WHERE p.LicenseId = ?";

    private static readonly string SelectPilotByPoints =
        "SELECT p FROM " + typeof(Pilot).FullName + " p WHERE p.Points = ?";

    public BahrainDriver(Setup s)
    : base(s)
    {
    }

    #region Common driver stuff

    public override void TakeSeatIn()
    {
        using (Transaction transaction = Transaction.NewCurrent())
        {
            Starcounter.Poleposition.Util.TypeDeleter.DeleteAllOfType<Pilot>();
            transaction.Commit();
        }
    }

    #endregion

    #region Laps

    [Lap("Write")]
    public void LapWrite()
    {
        int objectCount = Setup.ObjectCount;
        int commitInterval = Setup.CommitInterval;
        using(Transaction transaction = Transaction.NewCurrent())
        {
            for (int i = 1; i <= objectCount; ++i)
            {
                Pilot p = new Pilot();
                p.Id = i;
                p.Name = Pilot.GetName(i);
                p.FirstName = Pilot.GetFirstName(i);
                p.Points = i;
                p.LicenseId = i;
                if (this.Setup.IsCommitPoint(i))
                {
                    transaction.Commit();
                }
                AddToCheckSum(i);
            }
            transaction.Commit();
        } // end transaction
    }

    [Lap("Query_indexed_string")]
    public void LapQueryIndexedString()
    {
        using (Transaction transaction = Transaction.NewCurrent())
        {
            int selectCount = Setup.SelectCount;
            for (int i = 1; i <= selectCount; ++i)
            {
                int taken = 0;
                using (SqlEnumerator<Object> sqlResult = (SqlEnumerator<Object>)Db.SQL(SelectPilotByName, Pilot.GetName(i)).GetEnumerator())
                {
                    taken = AddResultChecksums(sqlResult);
                    if (taken != 1)
                    {
                        PolePositionEntrance.LogEvent("Duplicates/absences found, Pilot.Name = " + i + ", hits = " + taken);
                    }
                }

                if (taken != 1)   // Re-trying...
                {
                    using (SqlEnumerator<Object> sqlResult = (SqlEnumerator<Object>)Db.SQL(SelectPilotByName, Pilot.GetName(i)).GetEnumerator())
                    {
                        taken = AddResultChecksums(sqlResult);
                        PolePositionEntrance.LogEvent("Hits during the re-try = " + taken);
                    }
                }
            }
        }
    }

    [Lap("Query_string")]
    public void LapQueryString()
    {
        using (Transaction transaction = Transaction.NewCurrent())
        {
            int selectCount = Setup.SelectCount;
            for (int i = 1; i <= selectCount; ++i)
            {
                int taken = 0;

                using (SqlEnumerator<Object> sqlResult = (SqlEnumerator<Object>)Db.SQL(SelectPilotByFirstName, Pilot.GetFirstName(i)).GetEnumerator())
                {
                    taken = AddResultChecksums(sqlResult);
                    if (taken != 1)
                    {
                        PolePositionEntrance.LogEvent("Duplicates/absences found, Pilot.FirstName = " + i + ", hits = " + taken);
                    }
                }

                if (taken != 1)   // Re-trying...
                {
                    using (SqlEnumerator<Object> sqlResult = (SqlEnumerator<Object>)Db.SQL(SelectPilotByFirstName, Pilot.GetFirstName(i)).GetEnumerator())
                    {
                        taken = AddResultChecksums(sqlResult);
                        PolePositionEntrance.LogEvent("Hits during the re-try = " + taken);
                    }
                }
            }
        }
    }

    [Lap("Query_indexed_int")]
    public void LapQueryIndexedInt()
    {
        using (Transaction transaction = Transaction.NewCurrent())
        {
            int selectCount = Setup.SelectCount;
            for (int i = 1; i <= selectCount; ++i)
            {
                int taken = 0;

                using (SqlEnumerator<Object> sqlResult = (SqlEnumerator<Object>)Db.SQL(SelectPilotByLicenseId, i).GetEnumerator())
                {
                    taken = AddResultChecksums(sqlResult);
                    if (taken != 1)
                    {
                        PolePositionEntrance.LogEvent("Duplicates/absences found, Pilot.LicenseId = " + i + ", hits = " + taken);
                    }
                }

                if (taken != 1)   // Re-trying...
                {
                    using (SqlEnumerator<Object> sqlResult = (SqlEnumerator<Object>)Db.SQL(SelectPilotByLicenseId, i).GetEnumerator())
                    {
                        taken = AddResultChecksums(sqlResult);
                        PolePositionEntrance.LogEvent("Hits during the re-try = " + taken);
                    }
                }
            }
        }
    }

    [Lap("Query_int")]
    public void LapQueryInt()
    {
        using (Transaction transaction = Transaction.NewCurrent())
        {
            int selectCount = Setup.SelectCount;

            for (int i = 1; i <= selectCount; ++i)
            {
                int taken = 0;

                using (SqlEnumerator<Object> sqlResult = (SqlEnumerator<Object>)Db.SQL(SelectPilotByPoints, i).GetEnumerator())
                {
                    taken = AddResultChecksums(sqlResult);
                    if (taken != 1)
                    {
                        PolePositionEntrance.LogEvent("Duplicates/absences found, Pilot.Points = " + i + ", hits = " + taken);
                    }
                }

                if (taken != 1)   // Re-trying...
                {
                    using (SqlEnumerator<Object> sqlResult = (SqlEnumerator<Object>)Db.SQL(SelectPilotByPoints, i).GetEnumerator())
                    {
                        taken = AddResultChecksums(sqlResult);
                        PolePositionEntrance.LogEvent("Hits during the re-try = " + taken);
                    }
                }
            }
        }
    }

    [Lap("Update")]
    public void LapUpdate()
    {
        using (Transaction transaction = Transaction.NewCurrent())
        {
            using (SqlEnumerator<Object> sqlResult = (SqlEnumerator<Object>)Db.SQL(SelectAllPilots).GetEnumerator())
            {
                int remainingUpdates = Setup.UpdateCount;
                while (sqlResult.MoveNext() && remainingUpdates-- > 0)
                {
                    Pilot p = sqlResult.Current as Pilot;
                    p.Name = p.Name.ToUpper();
                    AddToCheckSum(1);
                }
            }
            transaction.Commit();
        }
    }

    [Lap("Delete")]
    public void LapDelete()
    {
        using (Transaction transaction = Transaction.NewCurrent())
        {
            using (SqlEnumerator<Object> sqlResult = (SqlEnumerator<Object>)Db.SQL(SelectAllPilots).GetEnumerator())
            {
                while (sqlResult.MoveNext())
                {
                    (sqlResult.Current as Pilot).Delete();
                    AddToCheckSum(1);
                }
            }
            transaction.Commit();
        }
    }

    #endregion

}
}
