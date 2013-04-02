using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Poleposition.Framework;

namespace Starcounter.Poleposition.Circuits.Bahrain
{
[Database]
public class Pilot : ICheckSummable
{
    public int Id;
    public string Name;
    public string FirstName;
    public int Points;
    public int LicenseId;

    public static string GetName(int i)
    {
        return "Pilot_" + i;
    }
    public static string GetFirstName(int i)
    {
        return "Jonny_" + i;
    }

    #region ICheckSummable Members

    public long GetCheckSum()
    {
        return Points;
    }

    #endregion
}
}
