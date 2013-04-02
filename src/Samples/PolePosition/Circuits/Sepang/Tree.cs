using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Poleposition.Framework;

namespace Starcounter.Poleposition.Circuits.Sepang
{

[Database]
public class Tree : ICheckSummable
{
    public int Depth;

    public string Name;

    public Tree Next;

    public Tree Prev;

    public void Traverse(Action<Tree> visitor)
    {
        Tree prev = Prev;
        Tree next = Next;
        if (prev != null)
        {
            prev.Traverse(visitor);
        }
        if (next != null)
        {
            next.Traverse(visitor);
        }
        visitor(this);
    }

    #region ICheckSummable Members

    public long GetCheckSum()
    {
        return Depth;
    }

    #endregion

    public static Tree Create(int depth)
    {
        return DoCreate(depth, 0);
    }

    private static Tree DoCreate(int maxdepth, int currdepth)
    {
        if (maxdepth == 0)
        {
            return null;
        }
        Tree t = new Tree();
        t.Name = (currdepth == 0) ? "root" : "node at depth " + currdepth;
        t.Depth = currdepth;
        t.Prev = DoCreate(maxdepth - 1, currdepth + 1);
        t.Next = DoCreate(maxdepth - 1, currdepth + 1);
        return t;
    }

}
}
