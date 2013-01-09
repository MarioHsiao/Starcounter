
using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Sc.Tools.Logging
{

internal delegate LogEntry ReadLogEntry(BinaryReader reader, UInt32 length);

/// <summary>
/// Class used to read log entries from a Starcounter log file.
/// </summary>
public sealed class LogReader : Object
{
	private UInt64 _logEntryCount;

    /// <summary>
    /// Default size of memory buffer used when reading from log file in
    /// bytes.
    /// </summary>
    public const Int32 DEFAULT_BUFFER_SIZE = (4096 * 2);

    private const Int32 NOT_READING = 0;
    private const Int32 READING = 1;
    private const Int32 READ_CANCELED = -1;

    private static readonly ReadLogEntry[] _delegates;

	

    static LogReader()
    {
        _delegates = new ReadLogEntry[]
        {
            new ReadLogEntry(ReadLogEntry_0)
        };
    }

    private static LogEntry ReadLogEntry_0(BinaryReader reader, UInt32 length)
    {
        Byte bv;
        EntryType type;
        Int64 fileTime;
        Int64 activityID;
        String machineName;
        String serverName;
        String source;
        String category;
        String userName;
        String message;
        if (length < (1 + 8 + 8))
        {
            goto e_format;
        }
        length -= (1 + 8 + 8);
        bv = reader.ReadByte();
        type = (EntryType)bv;
        fileTime = reader.ReadInt64();
        activityID = reader.ReadInt64();
        machineName = ReadString_0(reader, ref length);
        serverName = ReadString_0(reader, ref length);
        source = ReadString_0(reader, ref length);
        category = ReadString_0(reader, ref length);
        userName = ReadString_0(reader, ref length);
        message = ReadString_0(reader, ref length);
        if (length != 0)
        {
            goto e_format;
        }
        return new LogEntry(
                   type,
                   DateTime.FromFileTime(fileTime),
                   activityID,
                   machineName,
                   serverName,
                   source,
                   category,
                   userName,
                   message
               );
        e_format:
        throw new NotALogFileException("Log entry of invalid format detected.");
    }

    private static String ReadString_0(BinaryReader reader, ref UInt32 length)
    {
        UInt16 wv;
        Byte[] rawString;
        if (length < 2)
        {
            goto e_format;
        }
        length -= 2;
        wv = reader.ReadUInt16();
        if (wv == 0)
        {
            return null;
        }
        if (wv < 2)
        {
            goto e_format;
        }
        wv -= 2;
        if (length < wv)
        {
            goto e_format;
        }
        length -= wv;
        rawString = reader.ReadBytes(wv);
        if (rawString.Length != wv)
        {
            goto e_endofstr;
        }
        return Encoding.UTF8.GetString(rawString);
        e_format:
        throw new NotALogFileException("String of invalid format detected.");
        e_endofstr:
        throw new EndOfStreamException();
    }

    private readonly Object _syncRoot;

    private readonly LogDirectory _directory;

    private readonly LogFilter _filterBase;

    private readonly Int32 _bufferSize;

    private Boolean _open;

    private BinaryReader _reader;

    private LogWatcher _watcher;

    private LogFilter _filter;

    private Boolean _readNoMore;

    private Int32 _readState;

    /// <summary>
    /// Creates a log reader for the specified log directory.
    /// </summary>
    /// <param name="directory">
    /// The path of the directory where to look for log files, relative or
    /// full path supported.
    /// </param>
    public LogReader(String directory) : this(directory, null, DEFAULT_BUFFER_SIZE) { }

    /// <summary>
    /// Creates a log reader for the specified log directory with the
    /// specified filter.
    /// </summary>
    /// <param name="directory">
    /// The path of the directory where to look for log files, relative or
    /// full path supported.
    /// </param>
    /// <param name="filter">
    /// Filter used to filter log entries found in the file. A snapshot of
    /// the filter is created on creation. No changes made to the filter
    /// after creation is applied.
    /// </param>
    public LogReader(String directory, LogFilter filter) : this(directory, filter, DEFAULT_BUFFER_SIZE) { }

    /// <summary>
    /// Creates a log reader for the specified log directory with the
    /// specified buffer size.
    /// </summary>
    /// <param name="directory">
    /// The path of the directory where to look for log files, relative or
    /// full path supported.
    /// </param>
    /// <param name="bufferSize">
    /// File buffer used when reading the log file. A larger buffer will in
    /// general make the log reader perform better.
    /// </param>
    public LogReader(String directory, Int32 bufferSize) : this(directory, null, bufferSize) { }

    /// <summary>
    /// Creates a log reader for the specified log directory with the
    /// specified filter and buffer size.
    /// </summary>
    /// <param name="directory">
    /// The path of the directory where to look for log files, relative or
    /// full path supported.
    /// </param>
    /// <param name="filter">
    /// Filter used to filter log entries found in the file. A snapshot of
    /// the filter is created on creation. No changes made to the filter
    /// after creation is applied.
    /// </param>
    /// <param name="bufferSize">
    /// File buffer used when reading the log file. A larger buffer will in
    /// general make the log reader perform better.
    /// </param>
    public LogReader(String directory, LogFilter filter, Int32 bufferSize) : base()
    {
        DirectoryInfo directoryInfo;
        if (directory == null)
        {
            throw new ArgumentNullException("directory");
        }
        directoryInfo = new DirectoryInfo(directory);
        if (!directoryInfo.Exists)
            throw new ArgumentException(
                "Specified directory does not exist.",
                "directory"
            );
        _syncRoot = new Object();
        _directory = new LogDirectory(directoryInfo);
        _filterBase = filter;
        _bufferSize = bufferSize;
        _open = false;
        _reader = null;
        _watcher = null;
        _filter = null;
        _readNoMore = false;
        _readState = NOT_READING;
		_logEntryCount = 0;
    }

    /// <summary>
    /// Opens the log reader.
    /// </summary>
    /// <remarks>
    /// The underlying file is not opened until the first log entry is read
    /// from the file. This way; the log reader can be opened before the
    /// actual log file exists.
    /// </remarks>
    public void Open()
    {
        lock (_syncRoot)
        {
            if (_open)
            {
                throw new InvalidOperationException("The log reader is already open.");
            }
            _watcher = new LogWatcher(_directory);
            _open = true;
            if (_filterBase != null)
            {
                _filter = _filterBase.Clone();
            }
            _readNoMore = false;
        }
    }

    /// <summary>
    /// Closes the log reader and the underlying file stream.
    /// </summary>
    /// <returns>
    /// Returns true if the log reader was closed, false if the log reader
    /// already was closed.
    /// </returns>
    public Boolean Close()
    {
        lock (_syncRoot)
        {
            return DoClose(true);
        }
    }

    /// <summary>
    /// Resets an open log reader. The filter is re-evaluated and the
    /// underlying file stream is reset so that the next read starts from
    /// the beginning. The log filter is also re-evaluated, any changes
    /// made to the filter is applied by the reader.
    /// </summary>
    public void Reset()
    {
        lock (_syncRoot)
        {
            VerifyOpen();
            try
            {
                if (_reader != null)
                {
                    _reader.Close();
                    _reader = null;
                }
                if (_filterBase != null)
                {
                    _filter = _filterBase.Clone();
                }
                _readNoMore = false;
                _directory.Reset();
                _watcher.Reset();
				_logEntryCount = 0;
            }
            catch
            {
                DoClose(false);
                throw;
            }
        }
    }

    /// <summary>
    /// Reads a single log entry from the log file.
    /// </summary>
    /// <param name="wait">
    /// Indicates if thread should block and wait for log entries to become
    /// available if end of file was reached.
    /// </param>
    /// <returns>
    /// <para>
    /// A log entry, or null if no more log entries fitting the specified
    /// filter (is any) could be read from the log file.
    /// </para>
    /// <para>
    /// If a blocking read then the method blocks until more log entries
    /// are written to the file. This as long as the possibility exists
    /// that entries that fits within the specified time range could pop in
    /// to the file. If not then null is returned.
    /// </para>
    /// </returns>
    public LogEntry Read(Boolean wait)
    {
        return Read(wait ? Timeout.Infinite : 0);
    }

    /// <summary>
    /// Reads a single log entry from the log file.
    /// </summary>
    /// <param name="millisecondsTimeout">
    /// Indicates timeout in milliseconds before thread returns null if no
    /// log entry becomes available.
    /// </param>
    /// <returns>
    /// <para>
    /// A log entry, or null if no more log entries fitting the specified
    /// filter (is any) could be read from the log file.
    /// </para>
    /// <para>
    /// If a blocking read then the method blocks until more log entries
    /// are written to the file. This as long as the possibility exists
    /// that entries that fits within the specified time range could pop in
    /// to the file. If not then null is returned.
    /// </para>
    /// </returns>
    public LogEntry Read(Int32 millisecondsTimeout)
    {
        Int32 readState;
        DateTime startWaitTime;
        Object syncRoot;
        LogFilter filter;
        LogEntry le;
        TimeFilterResult tfr;
        Boolean br;
        DateTime now;
        Int32 millisecondsWaited;
        // Register reader. If already a reader we abort operation, only a
        // single treading thread is permitted.
        readState = Interlocked.CompareExchange(ref _readState, READING, NOT_READING);
        if (readState != NOT_READING)
        {
            throw new InvalidOperationException("Only a single reading thread permitted.");
        }
        try
        {
            startWaitTime = DateTime.MinValue;
            syncRoot = _syncRoot;
            loop_0:
            lock (syncRoot)
            {
                VerifyOpen();
                // Check if read is to be canceled. If so we simply abort
                // read.
                readState = Thread.VolatileRead(ref _readState);
                if (Thread.VolatileRead(ref _readState) == READ_CANCELED)
                {
                    return null;
                }
                filter = _filter;
                loop_1:
                if (_readNoMore)
                {
                    return null;
                }
                if (_reader != null)
                {
                    le = DoRead(_reader);
                    if (le != null)
                    {
						le.Number = ++_logEntryCount;
                        if (filter != null)
                        {
                            tfr = filter.CheckTimeFilter(le);
                            if (tfr != TimeFilterResult.Within)
                            {
                                if (tfr == TimeFilterResult.LongAfter)
                                {
                                    _readNoMore = true;
                                }
                                goto loop_1;
                            }
                            br = filter.CheckOtherFilters(le);
                            if (!br)
                            {
                                goto loop_1;
                            }
                        }
                        return le;
                    }
                    else
                    {
                        if (_directory.MoreFilesAvailable())
                        {
                            try
                            {
                                _reader.Close();
                            }
                            finally
                            {
                                _reader = null;
                            }
                            goto loop_1;
                        }
                    }
                }
                else
                {
                    // No reader. The file has not been opened yet. We
                    // attempt to open the file.
                    br = DoOpen();
                    if (br)
                    {
                        goto loop_1;
                    }
                }
            }
            if (millisecondsTimeout == 0)
            {
                return null;
            }
            // Wait for log file to change or the wait to timeout and
            // attempt to read again.
            //
            // Note that just because the log file has changed it doesn't
            // mean that there's additional log entries in the file. It
            // could for example mean that half a log entry has been
            // flushed and it could could also mean that the log file was
            // changed but we already dealt with the change. In other
            // words, we could get a false positive here. This isn't much
            // of a problem however; after we failed to read again the
            // thread will be blocked.
            if (millisecondsTimeout != -1)
            {
                // Store away when we started waiting and and if spent time
                // waiting before we adjust the remaining timeout with the
                // time spent waiting.
                now = DateTime.Now;
                if (startWaitTime != DateTime.MinValue)
                {
                    millisecondsWaited = (Int32)(now - startWaitTime).TotalMilliseconds;
                    millisecondsTimeout -= millisecondsWaited;
                    if (millisecondsTimeout <= 0)
                    {
                        return null;
                    }
                }
                startWaitTime = now;
            }
            br = _watcher.WaitForChanged(millisecondsTimeout);
            if (br)
            {
                goto loop_0;
            }
            return null;
        }
        finally
        {
            // Deregister reader before method returns.
            Thread.VolatileWrite(ref _readState, NOT_READING);
        }
    }

    public void CancelRead()
    {
        Int32 readState;
        // NOTE:
        // Previous implementation freed next read regardless of if there
        // was a reader or not. The current implementation only cancels
        // pending read however, not reads not yet published.
        VerifyOpen();
        // If a reader, set read state to read canceled. If no reader we
        // simply abort.
        readState = Interlocked.CompareExchange(ref _readState, READ_CANCELED, READING);
        if (readState != READING)
        {
            return;
        }
        lock (_syncRoot)
        {
            // We've checked if open once but then we where not holding a
            // lock. So we have to check this again before resetting event
            // in watcher.
            //
            // If reader is closed then the thread will be released
            // regardless because the watcher will have been disposed.
            VerifyOpen();
            _watcher.Reset();
        }
    }

    private Boolean DoOpen()
    {
        FileStream fs;
        BinaryReader br;
        try
        {
            fs = _directory.OpenNextLogFile(_bufferSize);
            if (fs == null)
            {
                return false;
            }
            br = new BinaryReader(fs);
        }
        catch
        {
            DoClose(false);
            throw;
        }
        _reader = br;
        return true;
    }

    private LogEntry DoRead(BinaryReader reader)
    {
        Stream stream;
        Int64 initPos;
        UInt32 len;
        Byte ver;
        stream = reader.BaseStream;
        initPos = stream.Position;
        try
        {
#if true
            // Something Gael put in here. Presumably to handle if length
            // and version isn't available. However, the
            // EndOfStreamException catch does this as well. So not sure
            // what the point is. Probably to improve performance.
            if (reader.BaseStream.Position + 5 >= reader.BaseStream.Length)
            {
                return null;
            }
#endif
            len = reader.ReadUInt32();
            ver = reader.ReadByte();
            try
            {
                return _delegates[ver](reader, (len - 5));
            }
            catch (IndexOutOfRangeException)
            {
                throw new NotALogFileException("Log entry with an incompatible version detected.");
            }
        }
        catch (EndOfStreamException)
        {
            // End of stream was reached. The entire log entry was not
            // available in the stream.
            //
            // We restore the read position to the beginning of the log
            // entry so that when more data becomes available, we can try
            // reading the log entry again.
            stream.Position = initPos;
            return null;
        }
        catch (Exception)
        {
            // An exception we can't handle was detected.
            //
            // We close that reader since it can be in an incosistent state
            // and rethrow the exception.
            DoClose(false);
            throw;
        }
    }

    private Boolean DoClose(Boolean rethrow)
    {
        BinaryReader reader;
        LogWatcher watcher;
        if (!_open)
        {
            return false;
        }
        reader = _reader;
        watcher = _watcher;
        _reader = null;
        _watcher = null;
        watcher.Dispose();
        if (reader != null)
        {
            try
            {
                reader.Close();
            }
            catch
            {
                if (rethrow)
                {
                    throw;
                }
            }
        }
        return true;
    }

    private void VerifyOpen()
    {
        if (!_open)
        {
            throw new InvalidOperationException("Log reader isn't open.");
        }
    }
}
}
