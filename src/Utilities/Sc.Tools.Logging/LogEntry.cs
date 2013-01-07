
using System;

namespace Sc.Tools.Logging
{

/// <summary>
/// Structure representing a log entry.
/// </summary>
public sealed class LogEntry : Object
{

    /// <summary>
    /// Type of log entry.
    /// </summary>
    public readonly EntryType Type;

    /// <summary>
    /// The time the log entry was generated.
    /// </summary>
    public readonly DateTime DateTime;

    /// <summary>
    /// Server unique identifier for the current activity when the log
    /// entry was generated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A Server unigue identifier is a unqiue identifier for the a
    /// specific server and is not to be reused (not reused even if the
    /// process restarts). A server is here defined as a named server on a
    /// specific machine.
    /// </para>
    /// <para>
    /// Not supported in 32-bits server versions. When the log is issued by
    /// a 32-bits server the value is always set to 0.
    /// </para>
    /// </remarks>
    public readonly Int64 ActivityID;

    /// <summary>
    /// Name of the machine where the log entry was generated.
    /// </summary>
    public readonly String MachineName;

    /// <summary>
    /// Name of the server application where the log entry was generated.
    /// </summary>
    public readonly String ServerName;

    /// <summary>
    /// Name of the source that generated the log entry. Usually a
    /// designation of a component or subsystem.
    /// </summary>
    public readonly String Source;

    /// <summary>
    /// Log entry category.
    /// </summary>
    public readonly String Category;

    /// <summary>
    /// Identifier of the current user when the log entry was generated.
    /// Null if the log entry was generated while no user was registered.
    /// </summary>
    public readonly String UserName;

	/// <summary>
	/// 
	/// </summary>
	private UInt64 _number;

    /// <summary>
    /// Log entry message.
    /// </summary>
    public readonly String Message;

    internal LogEntry(
        EntryType type,
        DateTime dateTime,
        Int64 activityID,
        String machineName,
        String serverName,
        String source,
        String category,
        String userName,
        String message
    ) : base()
    {
        Type = type;
        DateTime = dateTime;
        ActivityID = activityID;
        MachineName = machineName;
        ServerName = serverName;
        Source = source;
        Category = category;
        UserName = userName;
        Message = message;
    }

	public UInt64 Number
	{
		get { return _number; }
		internal set { _number = value; }
	}
}
}
