using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Starcounter.Advanced;
using Starcounter.Internal;

namespace Starcounter {

    /// <summary>
    /// Represents a database session.
    /// </summary>
    internal interface IDbSession {
        /// <summary>
        /// Runs a task asynchronously on a given scheduler.
        /// </summary>
        System.Threading.Tasks.Task RunAsync(Action action, Byte schedId = StarcounterEnvironment.InvalidSchedulerId);
    }

    /// <summary>
    /// Starcounter scheduler.
    /// </summary>
    public sealed class Scheduling {

        /// <summary>
        /// Database session interface.
        /// </summary>
        static IDbSession _dbSession;

        /// <summary>
        /// Setting actual database session implementation.
        /// </summary>
        internal static unsafe void SetDbSessionImplementation(IDbSession dbSessionImpl) {
            _dbSession = dbSessionImpl;
        }

        /// <summary>
        /// Schedules given task represented by the action delegate. The task is placed into queue
        /// and is processed by corresponding scheduler when picked from the queue. In case when "schedId" is not 
        /// set to a specific scheduler - processing scheduler is picked in round robin manner.
        /// In case when "waitForCompletion" flag is set, the completion of the task is awaited.
        /// </summary>
        /// <param name="action">Action to be run on scheduler.</param>
        /// <param name="waitForCompletion">Should we wait for the task to be completed.</param>
        /// <param name="schedulerId">Scheduler ID to run on.</param>
        public static void ScheduleTask(
            Action action,
            Boolean waitForCompletion = false, 
            Byte schedulerId = StarcounterEnvironment.InvalidSchedulerId) {

            // Checking for correct scheduler id.
            if ((schedulerId >= StarcounterEnvironment.SchedulerCount) &&
                (schedulerId != StarcounterEnvironment.InvalidSchedulerId)) {

                throw new ArgumentOutOfRangeException("Wrong scheduler ID is supplied.");
            }

            var t = ScheduleTaskAsync(action, schedulerId);

            if (waitForCompletion) {
                t.Wait();
            }
        }

        public static System.Threading.Tasks.Task ScheduleTaskAsync(
            Action action,
            Byte schedulerId = StarcounterEnvironment.InvalidSchedulerId)
        {

            // Checking for correct scheduler id.
            if ((schedulerId >= StarcounterEnvironment.SchedulerCount) &&
                (schedulerId != StarcounterEnvironment.InvalidSchedulerId))
            {

                throw new ArgumentOutOfRangeException("Wrong scheduler ID is supplied.");
            }

            return _dbSession.RunAsync(action, schedulerId);
        }

    }
}