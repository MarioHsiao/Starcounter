using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.Poleposition.Framework
{
public interface ICheckSummable
{
    /// <summary>
    /// Gets a check sum value for this item.
    /// </summary>
    long GetCheckSum();
}
}
