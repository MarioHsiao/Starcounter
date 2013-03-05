using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Poleposition.Framework;

namespace Starcounter.Poleposition.Circuits.Imola
{
/// <summary>
/// PP description: "retrieves objects by native id"
/// </summary>
[Driver("Imola")]
public class ImolaDriver : Driver
{

    private static ulong[] objectIds;

    public ImolaDriver(Setup s) : base(s)
    {
    }

    public override void TakeSeatIn()
    {
        using (Transaction transaction = Transaction.NewCurrent())
        {
            objectIds = null;
            Starcounter.Poleposition.Util.TypeDeleter.DeleteAllOfType<Pilot>();
            transaction.Commit();
        }
    }

    [Lap("Store")]
    public void LapStore()
    {
        using(Transaction transaction = Transaction.NewCurrent())
        {
            objectIds = new ulong[Setup.SelectCount];
            for (int i = 1; i <= Setup.ObjectCount; ++i)
            {
                Pilot p = new Pilot();
                p.Name = "Pilot_" + i;
                p.FirstName = "Jonny_" + i;
                p.Points = i;
                p.LicenseId = i;
                if (i <= Setup.SelectCount)
                {
                    objectIds[i - 1] = DbHelper.GetObjectID(p);
                }
                if (Setup.IsCommitPoint(i))
                {
                    transaction.Commit();
                }
            }
            transaction.Commit();
        }
    }

    [Lap("Retrieve")]
    public void LapRetrieve()
    {
        using (Transaction transaction = Transaction.NewCurrent())
        {
            foreach (ulong oid in objectIds)
            {
                Pilot p = (Pilot)DbHelper.FromID(oid);
                AddToCheckSum(p);
            }
            transaction.Commit();
        }
    }
}
}
