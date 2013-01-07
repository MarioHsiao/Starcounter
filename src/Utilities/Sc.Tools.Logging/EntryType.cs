
using Starcounter.Internal;
namespace Sc.Tools.Logging
{

/// <summary>
/// Specifies the type of a log entry.
/// </summary>
public enum EntryType : uint
{

    /// <summary>
    /// Debug log entry.
    /// </summary>
    Debug = sccorelog.SC_ENTRY_DEBUG,

    /// <summary>
    /// Success audit log entry. Indicates that an audited security event
    /// was completed successfully (for example if a user logged on to the
    /// server successfully).
    /// </summary>
    SuccessAudit = sccorelog.SC_ENTRY_SUCCESS_AUDIT,

    /// <summary>
    /// Failure audit log entry. Indicates that an audited security event
    /// failed to complete (for example if a user
    /// </summary>
    FailureAudit = sccorelog.SC_ENTRY_FAILURE_AUDIT,

    /// <summary>
    /// Information log entry. Usually indicates that a significant
    /// operation has completed successfully.
    /// </summary>
    Notice = sccorelog.SC_ENTRY_NOTICE,

    /// <summary>
    /// Warning log entry. Can indicate anything from performance warnings
    /// to invalid configuration that can be bypassed.
    /// </summary>
    Warning = sccorelog.SC_ENTRY_WARNING,

    /// <summary>
    /// Error log entry. Indicates a serious error that usually is caused
    /// by a bug or a problem with the environment.
    /// </summary>
    Error = sccorelog.SC_ENTRY_ERROR,

    /// <summary>
    /// Critical failure log entry. Indicates a critical failure that
    /// usually results in a server shutdown.
    /// </summary>
    Critical = sccorelog.SC_ENTRY_CRITICAL
}
}
