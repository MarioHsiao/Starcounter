using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Starcounter.Poleposition.Entrances;

namespace Starcounter.Poleposition.Framework
{
public abstract class Driver
{
    private readonly Setup setup;

    public Setup Setup
    {
        get
        {
            return setup;
        }
    }

    public void AddToCheckSum(long l)
    {
        setup.AddToCheckSum(l);
    }

    public void AddToCheckSum(ICheckSummable obj)
    {
        AddToCheckSum(obj.GetCheckSum());
    }

    /// <summary>
    /// Creates a new driver, with a given setup.
    /// </summary>
    /// <param name="setup">
    /// The setup for this driver. Subclasses are obliged to have
    /// a constructor taking a <see cref="Setup"/> object and pass
    /// it along upwards in the constructor hierarchy.
    /// </param>
    protected Driver(Setup setup)
    {
        if (setup == null)
        {
            throw new ArgumentNullException("setup");
        }
        this.setup = setup;
    }

    /// <summary>
    /// Prepare for a new set of laps.
    /// </summary>
    [Lap("TakeSeatIn")]
    public virtual void TakeSeatIn() { }

    /// <summary>
    /// Prepare for a lap. Runs a garbage collection and a checkpoint.
    /// </summary>
    [Lap("Prepare")]
    public void Prepare()
    {
    }

    /// <summary>
    /// Cleanup after a lap.
    /// </summary>
    [Lap("BackToPit")]
    public void BackToPit()
    {
        Console.WriteLine("Checksum: " + setup.CheckSum);
    }

    #region Utilities

    /// <summary>
    /// Iterates over an enumerator of <c>ICheckSummable</c> objects and
    /// adds their checksum value to the total checksum.
    /// </summary>
	/// <param name="sqlResult">A query result, where every "row" is an
    /// <c>ICheckSummable</c> instance.</param>
    /// <returns>The number of records iterated over.</returns>
    protected int AddResultChecksums(SqlEnumerator<Object> sqlResult)
    {
        int count = 0;
        while (sqlResult.MoveNext())
        {
            ++count;
            AddToCheckSum(sqlResult.Current as ICheckSummable);
        }
        return count;
    }

    protected void AddSingleResultChecksum(SqlEnumerator<Object> sqlResult)
    {
        if (!sqlResult.MoveNext())
        {
            throw new PolePositionException("Expected exactly one result from query '" + sqlResult.Query + "', found none");
        }

        AddToCheckSum(sqlResult.Current as ICheckSummable);

        if (sqlResult.MoveNext())
        {
            int count = 2;
            while (sqlResult.MoveNext())
            {
                ++count;
            }
            throw new PolePositionException("Expected exactly one result from query '" + sqlResult.Query + "', found " + count);
        }
    }

    #endregion

}
}
