using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Poleposition.Framework;
using Starcounter.Poleposition.Util;

namespace Starcounter.Poleposition.Circuits.Sepang
{
/// <summary>
/// PP description: "writes, reads and then deletes an object tree"
/// </summary>
[Driver("Sepang")]
public class SepangDriver : Driver
{
    public SepangDriver(Setup s) : base(s)
    {
    }

    public override void TakeSeatIn()
    {
        using (Transaction transaction = Transaction.NewCurrent())
        {
            TypeDeleter.DeleteAllOfType<Tree>();
            RootHolder.Root = null;
            transaction.Commit();
        }
    }

    [Lap("Write")]
    public void LapWrite()
    {
        using (Transaction transaction = Transaction.NewCurrent())
        {
            RootHolder.Root = Tree.Create(Setup.TreeDepth);
            transaction.Commit();
        }
    }

    [Lap("Read_hot")]
    public void LapReadHot()
    {
        using (Transaction transaction = Transaction.NewCurrent())
        {
            RootHolder.Root.Traverse(t => AddToCheckSum(t));
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
        using (Transaction transaction = Transaction.NewCurrent())
        {
            RootHolder.Root.Traverse(t => t.Delete());
            RootHolder.Root = null;
            transaction.Commit();
        }
    }

    /// <summary>
    /// Encapsulates the root reference, to avoid holding it across transactions.
    /// </summary>
    private static class RootHolder
    {
        private static ulong? root;
        public static Tree Root
        {
            get
            {
                return root.HasValue ? (Tree)DbHelper.FromID(root.Value) : null;
            }

            set
            {
                root = (value == null) ? null : new ulong?(DbHelper.GetObjectID(value));
            }
        }
    }

}
}
