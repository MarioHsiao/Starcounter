// ***********************************************************************
// <copyright file="TaskDuration.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Server.PublicModel {
    
    /// <summary>
    /// Denotes a duration of a task.
    /// </summary>
    public enum TaskDuration {
        /// <summary>
        /// The duration can't even be estimated.
        /// </summary>
        Unknown,

        /// <summary>
        /// Indicates the duration is currently hard to predict or we have
        /// no reason to try. But we can still report progress, for example
        /// a ticker (e.g. updated every tenth of a second).
        /// </summary>
        UnknownWithProgress,

        /// <summary>
        /// Short and indeterminate task. Typically finished within
        /// a second.
        /// </summary>
        ShortIndeterminate,

        /// <summary>
        /// Normmal indeterminate task. A normal duration of a task
        /// executing as part of a command is typically running in a
        /// couple of seconds.
        /// </summary>
        NormalIndeterminate,

        /// <summary>
        /// A long inderminate task.
        /// </summary>
        LongIndeterminate,

        /// <summary>
        /// A long task, that eventually will report progress. The
        /// ending of the task this duration applies to is indeterminate
        /// though.
        /// </summary>
        LongWithProgress,

        /// <summary>
        /// A long task, with fixed progress, i.e. we can provide a
        /// max value.
        /// </summary>
        LongWithFixedProgress
    }

    internal static class TaskDurationExtensions {

        /// <summary>
        /// Returns a value indicating if the current duration is one
        /// that denotes a task that reports progress.
        /// </summary>
        /// <param name="duration">The duration</param>
        /// <returns></returns>
        public static bool IsProgressing(this TaskDuration duration) {
            return duration == TaskDuration.LongWithProgress ||
                duration == TaskDuration.LongWithFixedProgress ||
                duration == TaskDuration.UnknownWithProgress;
        }

        /// <summary>
        /// Gets a value indicating if the given duration is determinate,
        /// i.e. it has a determined and specified maximum value.
        /// </summary>
        /// <param name="duration">The duration</param>
        /// <returns></returns>
        public static bool IsDeterminate(this TaskDuration duration) {
            return duration == TaskDuration.LongWithFixedProgress;
        }
    }
}