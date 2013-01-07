
using System;

namespace Sc.Tools.Logging
{

/// <summary>
/// Class providing filter functionality for the log reader.
/// </summary>
/// <example>
/// Creates a reader that only reads error log entries created today:
///
/// LogFilter lf = new LogFilter();
/// lf.Type = EntryType.Error;
/// lf.FromDateTime = DateTime.Today;
/// lf.ToDateTime = DateTime.Today.AddDays(1);
/// LogReader lr = new LogReader("starcounter.log", lf);
/// </example>
public sealed class LogFilter
{

    private FiltersApplied _filtersApplied;
    private EntryType _type;
    private Int64 _activityID;
    private DateTime _fromDateTime;
    private DateTime _toDateTime;
    private String _machineName;
    private String _serverName;
    private String _source;
    private String _category;

    /// <summary>
    /// Creates a log filter with no filters applied.
    /// </summary>
    public LogFilter() : base()
    {
        _filtersApplied = FiltersApplied.None;
    }

    /// <summary>
    /// Sets a filter on entry type. Only log entries with the specified
    /// type will be read by the reader, all other log entries will be
    /// excluded.
    /// </summary>
    public EntryType Type
    {
        set
        {
            _type = value;
            _filtersApplied |= FiltersApplied.Type;
        }
    }

    /// <summary>
    /// Sets a filter on the time when log entries where created. Only log
    /// entries created at or after the specified time will be read by the
    /// reader. All log entries created before the specified time will be
    /// excluded. Can be combined with <c>ToDateTime</c> to create a time
    /// range.
    /// </summary>
    public DateTime FromDateTime
    {
        set
        {
            _fromDateTime = value;
            _filtersApplied |= FiltersApplied.FromDateTime;
        }
    }

    /// <summary>
    /// Sets a filter on the time when log entries where created. Only log
    /// entries created at or before the specified time will be read by the
    /// reader. All log entries created after the specified time will be
    /// excluded. Can be combined with <c>FromDateTime</c> to create a time
    /// range.
    /// </summary>
    public DateTime ToDateTime
    {
        set
        {
            _toDateTime = value;
            _filtersApplied |= FiltersApplied.ToDateTime;
        }
    }

    /// <summary>
    /// Sets a filter on activity identifier. Only log entries that was
    /// created within the context of the activity with the specified
    /// identifier will be read by the reader. All other log entries will
    /// be excluded.
    /// </summary>
    /// <remarks>
    /// Note that the activity identifier is only unique for a specific
    /// server. It may be that an activity on a server is assigned the same
    /// same identifier as an activity on another server. To the make sure
    /// that the reader only reads the log entries of a specific activity
    /// the log entries has to be filtered on server as well.
    /// </remarks>
    public Int64 ActivityID
    {
        set
        {
            _activityID = value;
            _filtersApplied |= FiltersApplied.ActivityID;
        }
    }

    /// <summary>
    /// Sets a filter on machine name. Only log entries that was created on
    /// the machine with the specified name will be read by the reader. All
    /// other log entries will be excluded.
    /// </summary>
    public String MachineName
    {
        set
        {
            _machineName = value;
            _filtersApplied |= FiltersApplied.MachineName;
        }
    }

    /// <summary>
    /// Sets a filter on server name. Only log entries that was created by
    /// the server with the specified name will be read by the reader. All
    /// other log entries will be excluded.
    /// </summary>
    /// <remarks>
    /// Note that the server name is only unique on a specific machine.
    /// Multiple servers with the same name may exist on multiple machines.
    /// To make sure you only get log entries from a specific server you'll
    /// have to filter on machine as well.
    /// </remarks>
    public String ServerName
    {
        set
        {
            _serverName = value;
            _filtersApplied |= FiltersApplied.ServerName;
        }
    }

    /// <summary>
    /// Sets a filter on a log source. Only log entries that was created by
    /// the log source with the specified name will be read by the reader.
    /// All other log entries will be excluded.
    /// </summary>
    public String Source
    {
        set
        {
            _source = value;
            _filtersApplied |= FiltersApplied.Source;
        }
    }

    /// <summary>
    /// Sets a filter on a category. Only log entries that was created with
    /// the specified category will be read by the reader. All other log
    /// entries will be excluded.
    /// </summary>
    public String Category
    {
        set
        {
            _category = value;
            _filtersApplied |= FiltersApplied.Category;
        }
    }

    /// <summary>
    /// Resets all applied filters. After the log filter has been reset no
    /// log entries will be filtered by it.
    /// </summary>
    public void Reset()
    {
        _filtersApplied = FiltersApplied.None;
    }

    internal LogFilter Clone()
    {
        LogFilter ret;
        ret = new LogFilter();
        ret._filtersApplied = _filtersApplied;
        ret._type = _type;
        ret._activityID = _activityID;
        ret._fromDateTime = _fromDateTime;
        ret._toDateTime = _toDateTime;
        ret._machineName = _machineName;
        ret._serverName = _serverName;
        ret._source = _source;
        ret._category = _category;
        return ret;
    }

    internal TimeFilterResult CheckTimeFilter(LogEntry le)
    {
        FiltersApplied filtersApplied;
        filtersApplied = _filtersApplied;
        if ((filtersApplied & FiltersApplied.FromDateTime) != 0)
        {
            if (le.DateTime < _fromDateTime)
            {
                return TimeFilterResult.Before;
            }
        }
        if ((filtersApplied & FiltersApplied.ToDateTime) != 0)
        {
            if (le.DateTime > _toDateTime)
            {
                if (le.DateTime > _toDateTime.AddHours(2))
                {
                    return TimeFilterResult.LongAfter;
                }
                return TimeFilterResult.After;
            }
        }
        return TimeFilterResult.Within;
    }

    internal Boolean CheckOtherFilters(LogEntry le)
    {
        FiltersApplied filtersApplied;
        filtersApplied = _filtersApplied;
        if ((filtersApplied & FiltersApplied.Type) != 0)
        {
            if (le.Type != _type)
            {
                return false;
            }
        }
        if ((filtersApplied & FiltersApplied.ActivityID) != 0)
        {
            if (le.ActivityID != _activityID)
            {
                return false;
            }
        }
        if ((filtersApplied & FiltersApplied.MachineName) != 0)
        {
            if (!CompareStrings(le.MachineName, _machineName))
            {
                return false;
            }
        }
        if ((filtersApplied & FiltersApplied.ServerName) != 0)
        {
            if (!CompareStrings(le.ServerName, _serverName))
            {
                return false;
            }
        }
        if ((filtersApplied & FiltersApplied.Source) != 0)
        {
            if (!CompareStrings(le.Source, _source))
            {
                return false;
            }
        }
        if ((filtersApplied & FiltersApplied.Category) != 0)
        {
            if (!CompareStrings(le.Category, _category))
            {
                return false;
            }
        }
        return true;
    }

    private Boolean CompareStrings(String str0, String str1)
    {
        return (str0 == str1);
    }
}
}
