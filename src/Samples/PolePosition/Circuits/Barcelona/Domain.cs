using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Poleposition.Framework;

namespace Starcounter.Poleposition.Circuits.Barcelona
{

[Database]
public abstract class Barcelona0
{
    public int Field0;
    public virtual void SetAll(int i)
    {
        Field0 = i;
    }
}
public abstract class Barcelona1 : Barcelona0
{
    public int Field1;
    public override void SetAll(int i)
    {
        base.SetAll(i);
        Field1 = i;
    }
}
public abstract class Barcelona2 : Barcelona1
{
    public int Field2;
    public override void SetAll(int i)
    {
        base.SetAll(i);
        Field2 = i;
    }
}
public abstract class Barcelona3 : Barcelona2
{
    public int Field3;
    public override void SetAll(int i)
    {
        base.SetAll(i);
        Field3 = i;
    }
}
public class Barcelona4 : Barcelona3, ICheckSummable
{
    public int Field4;
    public override void SetAll(int i)
    {
        base.SetAll(i);
        Field4 = i;
    }

    #region ICheckSummable Members

    public long GetCheckSum()
    {
        return Field4;
    }

    #endregion
}
}
