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
        void RunAsync(Action action, Byte schedId = StarcounterEnvironment.InvalidSchedulerId);

        /// <summary>
        /// Runs a task asynchronously on current scheduler.
        /// </summary>
        void RunSync(Action action, Byte schedId = StarcounterEnvironment.InvalidSchedulerId);
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
        /// Runs the task represented by the action delegate asynchronously.
        /// </summary>
        /// <param name="action">Action to be run on scheduler.</param>
        /// <param name="waitForCompletion">Should we wait for the task to be completed.</param>
        /// <param name="schedId">Scheduler ID to run on.</param>
        public static void ScheduleTask(
            Action action,
            Boolean waitForCompletion = false, 
            Byte schedId = StarcounterEnvironment.InvalidSchedulerId) {

            if (waitForCompletion) {
                _dbSession.RunSync(action, schedId);
            } else {
                _dbSession.RunAsync(action, schedId);
            }
        }
    }
}