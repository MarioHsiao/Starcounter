
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Sc.Tools.Logging
{

internal sealed class LogDirectory : Object
{

    internal const String FILE_NAME_FILTER = "starcounter.??????????.log";

    private const Int32 REFRESH_STATE_PENDING = 0;
    private const Int32 REFRESH_STATE_RUNNING = 1;
    private const Int32 REFRESH_STATE_WAITING = 2;

    private static Int32 CompareLogFileInfos(FileInfo a, FileInfo b)
    {
        return String.CompareOrdinal(a.Name, b.Name);
    }

    private readonly DirectoryInfo _directory;
    private readonly LinkedList<FileInfo> _files;
    private Int32 _refreshState;

    //
    // Number of last file loaded, -1 if no files has yet been loaded.
    //
    private Int32 _lastFileNum;

    internal LogDirectory(DirectoryInfo directory)
    {
        _directory = directory;
        _files = new LinkedList<FileInfo>();
        Init();
    }

    internal String FileNameFilter
    {
        get
        {
            return FILE_NAME_FILTER;
        }
    }

    internal String FullName
    {
        get
        {
            return _directory.FullName;
        }
    }

    internal void Reset()
    {
        _files.Clear();
        Init();
    }

    internal FileStream OpenNextLogFile(Int32 bufferSize)
    {
        FileInfo fi;
        FileStream fs;
        // This function is thread-safe because if can only be called by
        // the thread calling LogReader.Read and only one thread can call
        // LogReader.Read at any given time.
        // TODO:
        // If the file is being copied into the directory it doesn't allow
        // the file to be opened for read. In this case we we fail to open
        // the file even if it exists: An I/O exception will be thrown and
        // the reader will be closed. What we would want to do is to wait
        // until the file has been copied and then open it but we can't
        // figure out a good way to do this:
        //
        // 1. There's nothing on the I/O exception other then the message
        //    to indicate that the file can't be opened for read. We don't
        //    want to parse the message of the exception to determine this.
        //
        // 2. Can't figure out any other way to determine if the file is
        //    open by another process that won't share read access.
        //
        // The only thing we can do, it seems, is to return false if an I/O
        // exception is  detected and let the reader try again the next
        // time the file has been changed. It seems to work but seems to be
        // a rather incomplete solution. What if the operation fails for
        // some other reason? We would also perhaps need some kind of
        // timeout so that we can determine when to give up.
        for (; ;)
        {
            if (_files.Count == 0)
            {
                RefreshFileList();
            }
            if (_files.Count == 0)
            {
                return null;
            }
            fi = _files.First.Value;
            _files.RemoveFirst();
            try
            {
                fs = new FileStream(
                    fi.FullName,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite,
                    bufferSize
                );
                _lastFileNum = GetFileNumber(fi);
                return fs;
            }
            catch (FileNotFoundException)
            {
                // File has been removed after file list was created.
                // Ignore and move on to the next file in the list.
            }
        }
    }

    internal Boolean MoreFilesAvailable()
    {
        // This function is thread-safe because if can only be called by
        // the thread calling LogReader.Read and only one thread can call
        // LogReader.Read at any given time.
        if (_files.Count == 0)
        {
            RefreshFileList();
        }
        return (_files.Count != 0);
    }

    internal void NotifyFilesAddedToDirectory()
    {
        Thread.VolatileWrite(ref _refreshState, REFRESH_STATE_PENDING);
    }

    private Int32 GetFileNumber(FileInfo fi)
    {
        String str;
        str = fi.Name;
        str = str.Substring(str.Length - 14, 10);
        return Int32.Parse(str);
    }

    private void Init()
    {
        _lastFileNum = -1;
        _refreshState = REFRESH_STATE_PENDING;
        LoadFileList();
    }

    private void RefreshFileList()
    {
        Int32 refreshState;
        // This function is thread-safe because if can only be called by
        // the thread calling LogReader.Read and only one thread can call
        // LogReader.Read at any given time.
        refreshState = Interlocked.CompareExchange(
                           ref _refreshState,
                           REFRESH_STATE_RUNNING,
                           REFRESH_STATE_PENDING
                       );
        if (refreshState == REFRESH_STATE_WAITING)
        {
            return;
        }
        LoadFileList();
        // Mark the state as waiting if not marked again as pending while
        // refreshing the list. This assures that list will be checked
        // again if a new file is added while refreshing the file list.
        //
        // We don't have to do this now however. The log watcher event will
        // also be signaled so the reading thread will not be blocked
        // waiting for new logs.
        Interlocked.CompareExchange(
            ref _refreshState,
            REFRESH_STATE_WAITING,
            REFRESH_STATE_RUNNING
        );
    }

    private void LoadFileList()
    {
        FileInfo[] files;
        Boolean filtering;
        Int32 i;
        FileInfo file;
        Int32 fileNum;
        files = _directory.GetFiles(FILE_NAME_FILTER);
        Array.Sort<FileInfo>(files, new Comparison<FileInfo>(CompareLogFileInfos));
        filtering = (_lastFileNum >= 0);
        for (i = 0; i < files.Length; i++)
        {
            file = files[i];
            if (filtering)
            {
                fileNum = GetFileNumber(file);
                if (fileNum <= _lastFileNum)
                {
                    continue;
                }
                filtering = false;
            }
            _files.AddLast(files[i]);
        }
    }
}
}
