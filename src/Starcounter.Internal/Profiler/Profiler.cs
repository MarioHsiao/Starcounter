// ***********************************************************************
// <copyright file="Profiler.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Threading;
using Starcounter;
using Starcounter.Internal;
using System.Text;
using System.Reflection;

namespace Starcounter
{
    /// <summary>
    /// Globally defined profilers.
    /// </summary>
    public enum ProfilerNames {
        [Description("EMPTY PROFILER")]
        Empty,

        [Description("GetUriHandlersManager")]
        GetUriHandlersManager,

        [Description("Profiler for retrieving preferred mimetype from the request.")]
        GetPreferredMimeType,

        [Description("Profiler for converting a jsonobject to the preferred mimetype for a response.")]
        JsonMimeConverter,

        [Description("Profiler for returning first result from a query.")]
        DbSQLFirst,
    }

    /// <summary>
    /// Globally defined checkpoints.
    /// </summary>
    public enum CheckpointNames {
        [Description("EMPTY CHECKPOINT")]
        Empty
    }

    /// <summary>
    /// Class PreciseTimer
    /// </summary>
    public class PreciseTimer
    {
        [DllImport("Kernel32.dll")]
        static extern bool QueryPerformanceCounter(out Int64 lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        static extern bool QueryPerformanceFrequency(out Int64 lpFrequency);

        Int64 startTicks_, stopTicks_, elapsedTicks_, startedCount_;

        static Double freqMs_ = 0, freqMcs_ = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreciseTimer" /> class.
        /// </summary>
        public PreciseTimer()
        {
            startTicks_ = 0;
            stopTicks_ = 0;
            elapsedTicks_ = 0;
            startedCount_ = 0;

            // Checking if we already initialized the timer.
            if (freqMs_ == 0)
            {
                Int64 freqTempVar;
                if (QueryPerformanceFrequency(out freqTempVar) == false)
                {
                    // High-performance counter not supported
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "High-performance counter not supported.");
                }

                freqMs_ = freqTempVar / 1000.0; // In milliseconds.
                freqMcs_ = freqTempVar / 1000000.0; // In microseconds.
            }
        }

        // Start the timer.
        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
        {
            startedCount_++;
            QueryPerformanceCounter(out startTicks_);
        }

        // Stop the timer.
        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            // Checking if profiler was started or just was reseted.
            if (startTicks_ == 0)
                return;

            QueryPerformanceCounter(out stopTicks_);
            elapsedTicks_ += (stopTicks_ - startTicks_);
        }

        // Reset all counters.
        /// <summary>
        /// Resets this instance.
        /// </summary>
        public void Reset()
        {
            startTicks_ = 0;
            stopTicks_ = 0;
            elapsedTicks_ = 0;
            startedCount_ = 0;
        }

        // Returns the number of times the timer was started.
        /// <summary>
        /// Gets the start count.
        /// </summary>
        /// <value>The start count.</value>
        public Int64 StartCount
        {
            get
            {
                return startedCount_;
            }
        }

        // Returns the duration of the timer (in milliseconds).
        /// <summary>
        /// Gets the duration.
        /// </summary>
        /// <value>The duration.</value>
        public Double Duration
        {
            get
            {
                return elapsedTicks_ / freqMs_;
            }
        }

        // Returns the duration of the timer (in microseconds).
        /// <summary>
        /// Gets the duration MCS.
        /// </summary>
        /// <value>The duration MCS.</value>
        public Double DurationMcs
        {
            get
            {
                return elapsedTicks_ / freqMcs_;
            }
        }
    }

    /// <summary>
    /// To be able to add description to a enum.
    /// </summary>
    public static class DescriptionExtensions {
        public static string GetDescriptionValue(this Enum value) {
            // Get the type
            Type type = value.GetType();

            // Get field info for this type
            FieldInfo fieldInfo = type.GetField(value.ToString());

            // Get the string value attributes
            DescriptionAttribute[] attribs = fieldInfo.GetCustomAttributes(
                typeof(DescriptionAttribute), false) as DescriptionAttribute[];

            // Return the first if there was a match.
            return attribs.Length > 0 ? attribs[0].Description : null;
        }
    }

    /// <summary>
    /// Class Profiler
    /// </summary>
    public class Profiler
    {
        // Each profiler has a descriptive name.
        PreciseTimer[] profilers_ = null;

        // Each checkpoint has a descriptive name.
        Int64[] checkpoints_ = null;

        /// <summary>
        /// All static profilers in the system.
        /// </summary>
        static Profiler[] schedulersProfilers_;

        /// <summary>
        /// Indicates if running non hosted unit tests.
        /// </summary>
        static Boolean unitTests_;

        /// <summary>
        /// Setting up the profilers.
        /// </summary>
        public static void Init(Boolean unitTests = false) {

            unitTests_ = unitTests;

            if (unitTests) {
                schedulersProfilers_ = new Profiler[1];
            } else {
                schedulersProfilers_ = new Profiler[StarcounterEnvironment.SchedulerCount];
                bmx.sc_init_profilers(StarcounterEnvironment.SchedulerCount);
            }

            for (Int32 i = 0; i < schedulersProfilers_.Length; i++) {
                schedulersProfilers_[i] = new Profiler();
            }
        }

        /// <summary>
        /// Setting up the profilers.
        /// </summary>
        public static void SetupHandler(UInt16 defaultSystemHttpPort, String dbName) {

            Handle.GET(defaultSystemHttpPort, "/profiler/" + dbName, () => {

                String[] allSchedProfilerResults = new String[schedulersProfilers_.Length];

                CountdownEvent countdownEvent = new CountdownEvent(schedulersProfilers_.Length);

                // For each scheduler.
                for (Byte i = 0; i < schedulersProfilers_.Length; i++) {

                    Byte schedId = i;

                    // Running asynchronous task.
                    Scheduling.ScheduleTask(() => {

                        Byte currentSchedId = StarcounterEnvironment.CurrentSchedulerId;
                        allSchedProfilerResults[currentSchedId] = Profiler.Current.GetResultsInJson(false);

                        String s;
                        Byte[] tempBuf = new Byte[4096];
                        unsafe {
                            fixed (Byte* p = tempBuf) {
                                UInt32 err = bmx.sc_profiler_get_results_in_json(currentSchedId, p, 4096);
                                if (0 != err)
                                    s = "{}";
                                else
                                    s = Marshal.PtrToStringAnsi(new IntPtr(p));
                            }
                        }
                         
                        allSchedProfilerResults[currentSchedId] += "," + s;

                        countdownEvent.Signal();

                    }, schedId);
                }

                countdownEvent.Wait();

                String allResults = "{\"profilers\":[";

                for (Int32 i = 0; i < allSchedProfilerResults.Length; i++) {
                    allResults += "{\"schedulerId\":" + i + "," + allSchedProfilerResults[i] + "}";

                    if (i < (allSchedProfilerResults.Length - 1))
                        allResults += ",";
                }

                allResults += "]}";

                return allResults;
            });

            Handle.DELETE(defaultSystemHttpPort, "/profiler/" + dbName, () => {

                CountdownEvent countdownEvent = new CountdownEvent(schedulersProfilers_.Length);

                // For each scheduler.
                for (Byte i = 0; i < schedulersProfilers_.Length; i++) {

                    Byte schedId = i;

                    // Running asynchronous task.
                    Scheduling.ScheduleTask(() => {

                        Profiler.Current.ResetAll();
                        bmx.sc_profiler_reset(StarcounterEnvironment.CurrentSchedulerId);

                        countdownEvent.Signal();

                    }, schedId);
                }

                countdownEvent.Wait();

                return 200;
            });
        }

        /// <summary>
        /// Current (belonging to scheduler) profiler.
        /// </summary>
        public static Profiler Current {
            get
            {
#if (PROFILING)
                if (unitTests_)
                    return schedulersProfilers_[0];
                else
                    return schedulersProfilers_[StarcounterEnvironment.CurrentSchedulerId];
#else
                return null;
#endif
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Profiler" /> class.
        /// </summary>
        public Profiler()
        {
            Int32 numProfilers = Enum.GetNames(typeof(ProfilerNames)).Length;

            profilers_ = new PreciseTimer[numProfilers];
            for (Int32 i = 0; i < numProfilers; i++) {
                profilers_[i] = new PreciseTimer();
            }

            Int32 numCheckpoints = Enum.GetNames(typeof(CheckpointNames)).Length;

            checkpoints_ = new Int64[numCheckpoints];
            for (Int32 i = 0; i < numCheckpoints; i++) {
                checkpoints_[i] = 0;
            }
        }

        /// <summary>
        /// Used to count how many times execution went through this checkpoint.
        /// </summary>
        /// <example>
        /// Profiler.Current.Checkpoint(CheckpointNames.Empty);
        /// </example>
        [Conditional("PROFILING")]
        public void Checkpoint(CheckpointNames id)
        {
            checkpoints_[(int)id]++;
        }

        /// <summary>
        /// Starts specified profiler.
        /// </summary>
        /// <example>
        /// Profiler.Current.Start(ProfilerNames.Empty);
        /// </example>
        [Conditional("PROFILING")]
        public void Start(ProfilerNames id)
        {
            profilers_[(int)id].Start();
        }

        /// <summary>
        /// Stops specified profiler.
        /// </summary>
        /// <example>
        /// Profiler.Current.Stop(ProfilerNames.Empty);
        /// </example>
        [Conditional("PROFILING")]
        public void Stop(ProfilerNames id)
        {
            profilers_[(int)id].Stop();
        }

        /// <summary>
        /// Resets all profilers.
        /// </summary>
        public void ResetAll()
        {
            foreach (ProfilerNames id in (ProfilerNames[])Enum.GetValues(typeof(ProfilerNames))) {
                profilers_[(int)id].Reset();
            }

            foreach (CheckpointNames id in (CheckpointNames[])Enum.GetValues(typeof(CheckpointNames))) {
                checkpoints_[(int)id] = 0;
            }
        }

        /// <summary>
        /// Gets profiling results when specified timer reaches certain start count.
        /// </summary>
        public String GetResultsInJson(ProfilerNames id, Int32 startCount, Boolean reset)
        {
            if (profilers_[(int)id].StartCount >= startCount)
                return GetResultsInJson(reset);

            return null;
        }

        /// <summary>
        /// Prints profiler results to the console.
        /// </summary>
        public void DrawResults()
        {
            Console.WriteLine(GetResultsInJson(true));
        }

        /// <summary>
        /// Fetches all profiler results.
        /// </summary>
        public String GetResultsInJson(Boolean reset)
        {
            // Assuming that all timers stopped at this moment.
            String outString = "\"managedProfilers\":[";
            Boolean comma = false;

            foreach (ProfilerNames id in (ProfilerNames[])Enum.GetValues(typeof(ProfilerNames)))
            {
                Int32 i = (Int32) id;

                if (comma)
                    outString += ",";
                else                    
                    comma = true;
                        
                outString += "{" + String.Format("\"profilerId\":\"{0}\",\"profilerName\":\"{1}\",\"totallyTookMs\":\"{2}\",\"startedTimes\":\"{3}\"",
                    id.ToString(),
                    id.GetDescriptionValue(),
                    profilers_[i].Duration.ToString(),
                    profilers_[i].StartCount) + "}";

                if (reset)
                    profilers_[i].Reset();
            }

            outString += "],";

            // Printing all checkpoints.
            comma = false;
            outString += "\"managedCheckpoints\":[";
            foreach (CheckpointNames id in (CheckpointNames[])Enum.GetValues(typeof(CheckpointNames)))
            {
                Int32 i = (Int32) id;

                if (comma)
                    outString += ",";
                else
                    comma = true;

                outString += "{" + String.Format("\"checkpointId\":\"{0}\",\"checkpointName\":\"{1}\",\"crossTimes\":\"{2}\"",
                    id.ToString(),
                    id.GetDescriptionValue(),
                    checkpoints_[i]) + "}";

                if (reset)
                    checkpoints_[i] = 0;
            }

            outString += "]";

            return outString;
        }

        /// <summary>
        /// Gets specific profiler results and resets its counter.
        /// </summary>
        public String GetSpecific(ProfilerNames id, Boolean reset)
        {
            String outString = null;

            Int32 i = (Int32) id;

            profilers_[i].Stop();

            outString += String.Format("Profiler #{0} \"{1}\" totally took {2} ms and was started {3} times.",
                i,
                id.GetDescriptionValue(),
                profilers_[i].Duration.ToString(),
                profilers_[i].StartCount) + Environment.NewLine;

            if (reset)
                profilers_[i].Reset();

            return outString;
        }
    }
}
