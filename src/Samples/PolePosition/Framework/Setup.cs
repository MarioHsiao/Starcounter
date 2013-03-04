using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.Poleposition.Framework
{
public class Setup
{
    public Setup()
    {
        checkSum       = 0;
        commitInterval = 0;
        objectCount    = 0;
        selectCount    = 0;
        updateCount    = 0;
        treeWidth      = 0;
        treeDepth      = 0;
        objectSize     = 0;
    }

    private long checkSum;

    public long CheckSum
    {
        get
        {
            return checkSum;
        }
        set
        {
            checkSum = value;
        }
    }

    private int commitInterval;

    public int CommitInterval
    {
        get
        {
            return commitInterval;
        }
        set
        {
            commitInterval = value;
        }
    }

    private int objectCount;

    public int ObjectCount
    {
        get
        {
            return objectCount;
        }
        set
        {
            objectCount = value;
        }
    }

    private int selectCount;

    public int SelectCount
    {
        get
        {
            return selectCount;
        }
        set
        {
            selectCount = value;
        }
    }

    private int updateCount;

    public int UpdateCount
    {
        get
        {
            return updateCount;
        }
        set
        {
            updateCount = value;
        }
    }

    private int treeWidth;

    public int TreeWidth
    {
        get
        {
            return treeWidth;
        }
        set
        {
            treeWidth = value;
        }
    }

    private int treeDepth;

    public int TreeDepth
    {
        get
        {
            return treeDepth;
        }
        set
        {
            treeDepth = value;
        }
    }

    private int objectSize;

    public int ObjectSize
    {
        get
        {
            return objectSize;
        }
        set
        {
            objectSize = value;
        }
    }

    public void AddToCheckSum(long l)
    {
        checkSum += l;
    }

    /// <summary>
    /// Returns true if we should commit after the specified, index-1 based, index.
    /// </summary>
    public bool IsCommitPoint(int i)
    {
        return CommitInterval != 0 && i % CommitInterval == 0 && i != 0;
    }
}
}
