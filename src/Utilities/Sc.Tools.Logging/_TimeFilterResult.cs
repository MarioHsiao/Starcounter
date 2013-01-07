
namespace Sc.Tools.Logging
{

internal enum TimeFilterResult
{
    /// <summary>
    /// Indicates that the log entry was created before the specified time
    /// range.
    /// </summary>
    Before,

    /// <summary>
    /// Indicates that the log entry was created within the specified time
    /// range.
    /// </summary>
    Within,

    /// <summary>
    /// Indicates that the log entry was created after the specified time
    /// range, but not as long after that we can exclude the possibility of
    /// more log entries within the file that is within the specified time
    /// range.
    /// </summary>
    After,

    /// <summary>
    /// Indicates that the log entry was created after the specified time
    /// range and also so long after that we with a very high propability
    /// can exclude the possibility of more log entries within the file.
    /// </summary>
    LongAfter
}
}
