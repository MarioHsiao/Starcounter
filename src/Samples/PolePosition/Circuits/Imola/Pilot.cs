using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Poleposition.Framework;

namespace Starcounter.Poleposition.Circuits.Imola
{
[Database]
public class Pilot : ICheckSummable
{
    public string Name;
    public string FirstName;
    public int Points;
    public int LicenseId;

    #region ICheckSummable Members

    public long GetCheckSum()
    {
        return Points;
    }

    #endregion
}
}
