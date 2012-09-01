using Sc.Server.Internal;
using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Threading;
using Starcounter;
//using Starcounter.Query.Execution;

namespace Starcounter
{
public class PreciseTimer
{
    [DllImport("Kernel32.dll")]
    static extern bool QueryPerformanceCounter(out Int64 lpPerformanceCount);

    [DllImport("Kernel32.dll")]
    static extern bool QueryPerformanceFrequency(out Int64 lpFrequency);

    Int64 startTicks, stopTicks, elapsedTicks, startedCount;

    static Double freqMs = 0, freqMcs = 0;

    public PreciseTimer()
    {
        startTicks = 0;
        stopTicks = 0;
        elapsedTicks = 0;
        startedCount = 0;

        // Checking if we already initialized the timer.
        if (freqMs == 0)
        {
            Int64 freqTempVar;
            if (QueryPerformanceFrequency(out freqTempVar) == false)
            {
                // High-performance counter not supported
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "High-performance counter not supported.");
            }

            freqMs = freqTempVar / 1000.0; // In milliseconds.
            freqMcs = freqTempVar / 1000000.0; // In microseconds.
        }
    }

    // Start the timer.
    public void Start()
    {
        startedCount++;
        QueryPerformanceCounter(out startTicks);
    }

    // Stop the timer.
    public void Stop()
    {
        QueryPerformanceCounter(out stopTicks);
        elapsedTicks += (stopTicks - startTicks);
    }

    // Reset all counters.
    public void Reset()
    {
        startTicks = 0;
        stopTicks = 0;
        elapsedTicks = 0;
        startedCount = 0;
    }

    // Returns the number of times the timer was started.
    public Int64 StartCount
    {
        get
        {
            return startedCount;
        }
    }

    // Returns the duration of the timer (in milliseconds).
    public Double Duration
    {
        get
        {
            return elapsedTicks / freqMs;
        }
    }

    // Returns the duration of the timer (in microseconds).
    public Double DurationMcs
    {
        get
        {
            return elapsedTicks / freqMcs;
        }
    }
}

public class Profiler
{
    // Each profiler has a descriptive name.
    String[] profilerNames = null;
    PreciseTimer[] stopwatches = null;

    // Each checkpoint has a descriptive name.
    String[] checkpointNames = null;
    Int64[] checkpointTicks = null;

    // Maximum amount of profilers in the system.
    const Int32 maxTimersNum = 64;

    public Profiler()
    {
        profilerNames = new String[maxTimersNum];
        stopwatches = new PreciseTimer[maxTimersNum];

        checkpointNames = new String[maxTimersNum];
        checkpointTicks = new Int64[maxTimersNum];

        for (Int32 i = 0; i < maxTimersNum; i++)
        {
            profilerNames[i] = null;
            stopwatches[i] = new PreciseTimer();

            checkpointNames[i] = null;
            checkpointTicks[i] = 0;
        }
    }

    /// <summary>
    /// Used to count how many times execution went through this checkpoint.
    /// </summary>
    public void Checkpoint(String name, Int32 checkpointId) // Example: Application.Profiler.Checkpoint("Number of socket->send calls", 0);
    {
        if (checkpointNames[checkpointId] == null) // Assign description only on the first time.
            checkpointNames[checkpointId] = name;

        checkpointTicks[checkpointId]++;
    }

    /// <summary>
    /// Starts specified profiler.
    /// </summary>
    public void Start(String name, Int32 timerId) // Example: Application.Profiler.Start("Time used for matrix multiplication", 0);
    {
        if (profilerNames[timerId] == null) // Assign description only on the first time.
            profilerNames[timerId] = name;

        stopwatches[timerId].Start();
    }

    /// <summary>
    /// Stops specified profiler.
    /// </summary>
    public void Stop(Int32 timerIndex) // Example: Application.Profiler.Stop(0);
    {
        stopwatches[timerIndex].Stop();
    }

    /// <summary>
    /// Gets profiling results when specified timer reaches certain start count.
    /// </summary>
    public String GetResults(Int32 timerIndex, Int32 startCount)
    {
        if (stopwatches[timerIndex].StartCount >= startCount)
            return GetResults();

        return null;
    }

    /// <summary>
    /// Prints profiler results to the console.
    /// </summary>
    public void DrawResults()
    {
        Console.WriteLine(GetResults());
    }

    /// <summary>
    /// Fetches all profiler results and resets all counters.
    /// </summary>
    public String GetResults()
    {
        // Assuming that all timers stopped at this moment.
        String outString = "==== Profiling results ====" + Environment.NewLine;
        for (Int32 i = 0; i < maxTimersNum; i++)
        {
            if (profilerNames[i] != null)
            {
                outString += String.Format("  #{0} \"{1}\" took {2} ms and was started {3} times.",
                    i,
                    profilerNames[i],
                    stopwatches[i].Duration.ToString(),
                    stopwatches[i].StartCount) + Environment.NewLine;

                stopwatches[i].Reset();
            }
        }
        outString += "==== End of Profiling results ====" + Environment.NewLine;

        // Printing all checkpoints.
        outString += "==== Checkpoint results ====" + Environment.NewLine;
        for (Int32 i = 0; i < maxTimersNum; i++)
        {
            if (checkpointNames[i] != null)
            {
                outString += String.Format("  #{0} \"{1}\" was called {2} times.",
                    i,
                    checkpointNames[i],
                    checkpointTicks[i]) + Environment.NewLine;

                checkpointTicks[i] = 0;
            }
        }

        outString += "==== End of Checkpoint results ====" + Environment.NewLine;
        return outString;
    }

    /// <summary>
    /// Gets specific profiler results and resets its counter.
    /// </summary>
    public String GetSpecific(Int32 timerIndex)
    {
        String outString = null;

        if (profilerNames[timerIndex] != null)
        {
            stopwatches[timerIndex].Stop();

            outString += String.Format("Profiler #{0} \"{1}\" took {2} ms and was started {3} times.",
                timerIndex,
                profilerNames[timerIndex],
                stopwatches[timerIndex].Duration.ToString(),
                stopwatches[timerIndex].StartCount) + Environment.NewLine;

            stopwatches[timerIndex].Reset();
        }

        return outString;
    }

    /// <summary>
    /// Asks server to return all profiling results and reset all counters.
    /// </summary>
    public void DrawResultsFromServer()
    {
        Console.WriteLine(SqlConnectivity.GetServerProfilingString());
    }
}
}
