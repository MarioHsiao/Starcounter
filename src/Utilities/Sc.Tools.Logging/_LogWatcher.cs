
using System;
using System.IO;
using System.Threading;

namespace Sc.Tools.Logging
{

internal sealed class LogWatcher : Object
{

    private readonly AutoResetEvent _event;
    private readonly FileSystemWatcher _watcher;
    private readonly LogDirectory _directory;
    private volatile Boolean _disposed;

    internal LogWatcher(LogDirectory directory)
    {
        FileSystemWatcher watcher;
        FileSystemEventHandler eventHandler;
        watcher = new FileSystemWatcher(directory.FullName);
        watcher.Filter = directory.FileNameFilter;
        eventHandler = new FileSystemEventHandler(OnChanged);
        watcher.Changed += eventHandler;
        watcher.Created += eventHandler;
        _event = new AutoResetEvent(true);
        _watcher = watcher;
        _directory = directory;
        _disposed = false;
        watcher.EnableRaisingEvents = true;
    }

    internal void Dispose()
    {
        _disposed = true;
        Thread.MemoryBarrier();
        _event.Set();
        _watcher.Dispose();
    }

    internal void Reset()
    {
        _event.Set();
    }

    internal Boolean WaitForChanged(Int32 millisecondsTimeout)
    {
        if (_event.WaitOne(millisecondsTimeout, false))
        {
            // Event signaled. Either a change has been detected of the log
            // wather has been disposed. Return if a change has been
            // detected.
            return (_disposed == false);
        }
        // Timeout. Always return no change detected.
        return false;
    }

    private void OnChanged(Object sender, FileSystemEventArgs args)
    {
        if ((args.ChangeType & WatcherChangeTypes.Created) != 0)
        {
            _directory.NotifyFilesAddedToDirectory();
        }
        _event.Set();
    }
}
}
