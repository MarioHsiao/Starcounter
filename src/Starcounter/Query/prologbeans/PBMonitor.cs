// Copyright (c) 2004 SICS AB. All rights reserved.
//
using Starcounter;
using Starcounter.Logging;

namespace se.sics.prologbeans
{
using System;

/// <summary> <c>PBMonitor</c> is used to supervise and cancel queries that
/// takes too long time.
/// </summary>
class PBMonitor: SupportClass.ThreadClass
{
    static readonly LogSource logSource = LogSources.Sql;

    private void  InitBlock()
    {
        cancelList = new PrologSession[10];
        sessions = new PrologSession[10];
    }
    internal static PBMonitor Default
    {
        get
        {
            return defaultMonitor;
        }
    }

    //UPGRADE_NOTE: The initialization of  'defaultMonitor' was moved to static method 'se.sics.prologbeans.PBMonitor'. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1005"'
    private static PBMonitor defaultMonitor;


    //UPGRADE_NOTE: The initialization of  'sessions' was moved to method 'InitBlock'. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1005"'
    private PrologSession[] sessions;
    private int activeCount = 0;

    //UPGRADE_NOTE: The initialization of  'cancelList' was moved to method 'InitBlock'. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1005"'
    private PrologSession[] cancelList;
    private int cancelCount = 0;

    private PBMonitor(): base()
    {
        InitBlock();
        Name = "PBMonitor";
        IsBackground = true;
        Start();
    }
    //UPGRADE_NOTE: Synchronized keyword was removed from method 'queryStarted'. Lock expression was added. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1027"'
    // PBMonitor constructor

    internal virtual void  queryStarted(PrologSession session)
    {
        lock (this)
        {
            if (activeCount == sessions.Length)
            {
                PrologSession[] tmp = new PrologSession[activeCount + 10];
                Array.Copy(sessions, 0, tmp, 0, activeCount);
                sessions = tmp;
            }
            sessions[activeCount++] = session;
        }
    }

    //UPGRADE_NOTE: Synchronized keyword was removed from method 'queryFinished'. Lock expression was added. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1027"'
    internal virtual void  queryFinished(PrologSession session)
    {
        lock (this)
        {
            for (int i = 0; i < activeCount; i++)
            {
                if (sessions[i] == session)
                {
                    activeCount--;
                    sessions[i] = sessions[activeCount];
                    sessions[activeCount] = null;
                    break;
                }
            }
        }
    }

    //UPGRADE_TODO: The equivalent of method 'java.lang.Thread.run' is not an override method. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1143"'
    override public void  Run()
    {
        try
        {
            do
            {
                try
                {
                    System.Threading.Thread.Sleep(new System.TimeSpan(10000 * 1000));
                    checkQueries();
                }
                catch (System.Threading.ThreadAbortException)
                {
                    throw;
                }
                catch (System.Exception e)
                {
                    logSource.LogException(e);
                }
            }
            while (true);
        }
        finally
        {
            //System.Console.Error.WriteLine("PBMonitor: monitor died!");
        }
    }

    // Note: may only be called with the timeout thread
    private void  checkQueries()
    {
        long currentTime = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
        lock (this)
        {
            if (cancelList.Length < activeCount)
            {
                cancelList = new PrologSession[activeCount];
            }
            for (int i = 0; i < activeCount; i++)
            {
                PrologSession sess = sessions[i];
                int timeout = sess.Timeout;
                long startTime = sess.QueryStartTime;
                if (currentTime > (startTime + timeout))
                {
                    activeCount--;
                    sessions[i] = sessions[activeCount];
                    sessions[activeCount] = null;
                    if (startTime > 0L && timeout > 0)
                    {
                        // The query has taken too long and need to be cancelled
                        cancelList[cancelCount++] = sess;
                    }
                    // Since we might have moved one session to this index it
                    // will need to be rechecked again.
                    i--;
                }
            }
        }
        if (cancelCount > 0)
        {
            // Notify all sessions that need to be cancelled. This should
            // not be done synchronized in case the cancellation takes time
            // and we do not want new queries to be blocked.
            for (int i = 0; i < cancelCount; i++)
            {
                logSource.LogError("PBMonitor: need to interrupt read/write!");
                cancelList[i].cancelQuery();
                cancelList[i] = null;
            }
            cancelCount = 0;
        }
    }
    static PBMonitor()
    {
        defaultMonitor = new PBMonitor();
    }
}
// PBMonitor
}