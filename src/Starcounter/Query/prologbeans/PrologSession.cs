// Copyright (c) 2004 SICS AB. All rights reserved.
//


#undef JAVAXCONTEXT
#undef HTTPSESSION

namespace se.sics.prologbeans
{
    using Starcounter;
    using Starcounter.Internal;
    using Starcounter.Logging;
    using System;
#if JAVAXCONTEXT
using Context = javax.naming.Context;
using InitialContext = javax.naming.InitialContext;
#endif
/// <summary> <c>PrologSession</c> handles the connection with the Prolog
/// Server. Currently only synchronous connections with the server are
/// supported.
/// </summary>
// [PM] FIXME: when Java is closed it should do whatever it takes to
// prevent the Prolog side from blocking forever. Presumably it would
// be enough to close the Java-side socket.
// [JE] Java always close sockets and streams when it closes down.
// * What exactly does "Java is closed" mean?
// [PM] When I close the window for the evaluate demo SICStus will
//      hang (in WriteFile() !!). At this point the Java process has
//      exited so this appears to be more of an OS bug.


public class PrologSession
{
    static readonly LogSource logSource = LogSources.Sql;

    private void  InitBlock()
    {
        //    flags = AUTO_CONNECT;
        flags = 0;    /* [PD] No autoconnect until further notice */
    }
    /// <summary> Returns the timeout in milliseconds before the connection to the
    /// Prolog server is reset (when a query is not answered).
    /// </summary>
    /// <summary> Sets the timeout in milliseconds before the connection to the
    /// Prolog server is reset (when a query is not answered). Setting
    /// the timeout to <c>0</c> will disable timeouts for this
    /// prolog session. Default is 2000 milliseconds.
    /// </summary>
    virtual public int Timeout
    {
        get
        {
            if (parentSession != null)
            {
                return parentSession.Timeout;
            }
            return timeout;
        }
        set
        {
            if (parentSession != null)
            {
                parentSession.Timeout = value;
            }
            else
            {
                this.timeout = value;
            }
        }
    }
    /// <summary> Returns the port of the Prolog server.
    /// </summary>
    /// <summary> Sets the port of the Prolog server (default <c>StarcounterEnvironment.DefaultPorts.SQLProlog</c>).
    /// </summary>
    virtual public int Port
    {
        get
        {
            if (parentSession != null)
            {
                return parentSession.Port;
            }
            return port;
        }
        set
        {
            if (parentSession != null)
            {
                parentSession.Port = value;
            }
            else
            {
                this.port = value;
            }
        }
    }
    /// <summary> Returns the host of the Prolog server (exactly as registered in
    /// <c>setHost</c>).
    /// </summary>
    /// <summary> Sets the host of the Prolog server (default is
    /// <c>localhost</c>). The host can be specified as either
    /// IP-address or host name.
    /// </summary>
    virtual public System.String Host
    {
        get
        {
            if (parentSession != null)
            {
                return parentSession.Host;
            }
            return host;
        }
        set
        {
            if (parentSession != null)
            {
                parentSession.Host = value;
            }
            else
            {
                this.host = value;
            }
        }
    }
    /// <summary>
    /// </summary>
    virtual public bool AlwaysClosing
    {
        get
        {
            if (parentSession != null)
            {
                return parentSession.AlwaysClosing;
            }
            return (flags & ALWAYS_CLOSE) != 0;
        }
    }
    /// <summary>
    /// </summary>
    virtual public bool AlwaysClose
    {
        set
        {
            if (parentSession != null)
            {
                parentSession.AlwaysClose = value;
            }
            else if (value)
            {
                flags = flags | ALWAYS_CLOSE;
            }
            else
            {
                flags = flags & ~ ALWAYS_CLOSE;
            }
        }
    }
    // [PD] No autoconnect until further notice
    //      /// <summary> Sets the connection mode of this <c>PrologSession</c>. If
    //      /// set to <c>true</c> it will ensure that it is connected to
    //      /// the Prolog server as soon as a call to <c>executeQuery</c>
    //      /// or anything else causing a need for communication happens. This
    //      /// is by default set to <c>true</c>.
    //      /// </summary>
    //      virtual public bool AutoConnect
    //  {
    //    set
    //      {
    //        if (parentSession != null)
    //    {
    //      parentSession.AutoConnect = value;
    //    }
    //        else if (value)
    //    {
    //      flags = flags | AUTO_CONNECT;
    //    }
    //        else
    //    {
    //      flags = flags & ~ AUTO_CONNECT;
    //    }
    //      }
    //
    //  }
    /// <summary> Returns the state of the auto connect mode.
    /// </summary>
    /*
    /// <seealso cref="se.sics.prologbeans.PrologSession.AutoConnect"/>
    */
    virtual public bool AutoConnecting
    {
        get
        {
            if (parentSession != null)
            {
                return parentSession.AutoConnecting;
            }
            return (flags & AUTO_CONNECT) != 0;
        }
    }
    /// <summary> Returns <c>true</c> if a connection with the Prolog server
    /// is open and <c>false</c> otherwise.
    /// </summary>
    virtual public bool Connected
    {
        get
        {
            if (parentSession != null)
            {
                return parentSession.Connected;
            }
            return connection != null;
        }
    }
    virtual internal long QueryStartTime
    {
        // -------------------------------------------------------------------
        // API towards PBMonitor.
        // The monitor is used to supervise and cancel prolog queries that
        // takes too long time.
        // -------------------------------------------------------------------
        get
        {
            return sendTime;
        }
    }

    //   private static int debugLevel = 0;

    //   static {
    //     try {
    //       debugLevel =
    //  Integer.getInteger("se.sics.prologbeans.debugLevel", 0).intValue();
    //     } catch (Exception e) {
    //       // Ignore security exceptions in app-servers, etc.
    //     }
    //   }

    //   static boolean debugging() {
    //     return debugLevel >= 1;
    //   }

    //   static boolean debugging(int level) {
    //     return debugLevel >= level;
    //   }

    private const int ALWAYS_CLOSE = 1;
    private const int AUTO_CONNECT = 2;

    private long sendTime = - 1L;
    private int timeout = 2000; // Wait for an answer max 2000 millis

    private FastParser parser;
    //      private System.String query;

    private System.IO.Stream input;
    private FastWriter output;
    private System.Net.Sockets.TcpClient connection;

    private int port = StarcounterEnvironment.DefaultPorts.SQLProlog;
    private System.String host = "127.0.0.1";
    //UPGRADE_NOTE: The initialization of  'flags' was moved to method 'InitBlock'. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1005"'
    private int flags;
    // ALWAYS_CLOSE;

    private PrologSession parentSession;
    private System.String prologSessionID;
#if JAVAXCONTEXT
    private static InitialContext initCtx;
#endif

    private bool isAddedToMonitor = false;

    /// <summary> Creates a new <c>PrologSession</c> instance with default
    /// Prolog server settings.
    /// </summary>
    public PrologSession()
    {
        InitBlock();
        parser = new FastParser();
        //      new Monitor(this);
    }
    // PrologSession constructor

    private PrologSession(PrologSession parent, System.String sessionID)
    {
        InitBlock();
        this.parentSession = parent;
        this.prologSessionID = sessionID;
    }
    // PrologSession constructor


    /*
     <summary> Sets the session id for this prolog session. If the id is set
     each query sent to the prolog will include this session id. Note:
     the session id can only be set once per session and can not be
     changed.
     *
     </summary>
     <param name="id">the id of this session

     </param>
    */
    //    public void setSessionID(String id) {
    //      if (this.id == null) {
    //        this.id = id;
    //      } else {
    //        throw new IllegalStateException("Can not set session id more than once");
    //      }
    //    }


    // [PD] FIXME: This method refers to javax.naming.InitialContext, and
    //             javax.naming.Context. We need to adapt this to something
    //             corresponding in .NET.
#if JAVAXCONTEXT
    /// <summary> Returns the <c>PrologSession</c> registered in JNDI with
    /// the given name. Use this method in application servers where
    /// services are registered using JNDI. Please note: the application
    /// server must be configured to register the
    /// <c>PrologSession</c> with the given name for this method to
    /// work.
    /// </summary>
    /// <param name="name">the name of the prolog session
    /// </param>
    /// <returns> the named prolog session or <c>null</c> if no such
    /// session could be found
    ///
    /// </returns>
    public static PrologSession getPrologSession(System.String name)
    {
        try
        {
            if (initCtx == null)
            {
                initCtx = new InitialContext();
            }
            Context envCtx = (Context) initCtx.lookup("java:comp/env");
            //        System.out.println("Looking up session: " + name +  " in " + envCtx);
            return (PrologSession) envCtx.lookup(name);
        }
        catch (System.Exception e)
        {
            SupportClass.WriteStackTrace(e, Console.Error);
        }
        return null;
    }
#endif

    // [PD] FIXME: This method refers to javax.servlet.http.HttpSession. We need
    //             to adapt this to something corresponding to servlets in .NET.
#if HTTPSESSION
    //UPGRADE_TODO: Interface javax.servlet.http.HttpSession was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1095"'
    /// <summary> Returns the <c>PrologSession</c> registered in JNDI with
    /// the given name. The <c>PrologSession</c> will make use of
    /// sessions and the session id will be the same as in the
    /// <c>HTTPSession</c>. Use this method in web application
    /// servers with support for servlets and <c>HTTPSession</c>
    /// (and when support for sessions is desired).
    /// Note: This will cause the <c>PrologSession</c> to include
    /// the session id in its queries.
    /// </summary>
    /// <param name="name">the name of the prolog session
    /// </param>
    /// <param name="httpSession">the http session
    /// </param>
    /// <returns> the named prolog session
    ///
    /// </returns>
    public static PrologSession getPrologSession(System.String name, HttpSession httpSession)
    {
        //UPGRADE_TODO: Method javax.servlet.http.HttpSession.getAttribute was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1095"'
        System.Object object_Renamed = httpSession.getAttribute("prologbeans.session");
        if (object_Renamed == null)
        {
            //System.out.println("Creating new session!!!");
            //UPGRADE_TODO: Method javax.servlet.http.HttpSession.getId was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1095"'
            PrologSession session = new PrologSession(getPrologSession(name), httpSession.getId());
            //UPGRADE_TODO: Method javax.servlet.http.HttpSession.setAttribute was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1095"'
            httpSession.setAttribute("prologbeans.session", new AppSession(session));
            return session;
        }
        else
        {
            //System.out.println("Reusing old session:" + object);
            return ((AppSession) object_Renamed).PrologSession;
        }
    }
#endif

    internal virtual void  endSession()
    {
        if (prologSessionID != null)
        {
            sendAtom("end_session", prologSessionID);
        }
    }









    /// <summary> Sends a query to the Prolog server and waits for the answer
    /// before returning the <c>QueryAnswer</c>. Anonymous
    /// variables (underscore, <c>_</c>), will be ignored, and thus
    /// not accessible in the <c>QueryAnswer</c>.
    /// <c>executeQuery</c> throws <c>IOException</c> if
    /// communication problems with the server occurs. Please note:
    /// <c>executeQuery</c> will only return one answer.
    /// </summary>
    /// <param name="query">the query to send to the prolog server
    ///             The characters in the query are restricted to ISO-8859-1.
    /// </param>
    /// <returns> the answer from the prolog server
    /// </returns>
    /// <exception cref="System.IO.IOException">Thrown if an error occurs. A possible cause is a timeout.
    /// </exception>
    /// <seealso cref="se.sics.prologbeans.PrologSession.Timeout"/>
    public virtual QueryAnswer executeQuery(System.String query)
    {
        return new QueryAnswer(send(query, null, prologSessionID), null);
    }

    /// <summary> Sends a query to the Prolog server and waits for the answer
    /// before returning the
    /// <c>QueryAnswer</c>. <c>Bindings</c> are variable
    /// bindings for the given query and will ensure that the values are
    /// stuffed correctly.
    /// <para>
    /// An example:
    /// <example>
    /// <code>QueryAnswer answer = executeQuery("evaluate(In,Out)",
    /// new Bindings().bind("In","4*9."));
    /// </code>
    /// </example>
    /// </para>
    /// </summary>
    /// <param name="query">the query to send to the prolog server
    ///             The characters in the query are restricted to ISO-8859-1.
    /// </param>
    /// <param name="bindings">the variable bindings to use in the query
    /// </param>
    /// <returns> the answer from the prolog server
    /// </returns>
    /// <exception cref="System.IO.IOException"> if an error occurs. A possible cause is a timeout.
    /// </exception>
    /// <seealso cref="Timeout"/>
    public virtual QueryAnswer executeQuery(System.String query, Bindings bindings)
    {
        return new QueryAnswer(send(query, bindings, prologSessionID), bindings);
    }

    /// <summary> Sends a query to the Prolog server and waits for the answer
    /// before returning the
    /// <c>QueryAnswer</c>. <c>Bindings</c> are variable
    /// bindings for the given query and will ensure that the values are
    /// stuffed correctly.
    /// </summary>
    /// <param name="query">the query to send to the prolog server
    ///             The characters in the query are restricted to ISO-8859-1.
    /// </param>
    /// <param name="bindings">the variable bindings to use in the query
    /// </param>
    /// <param name="sessionID">the session id to give to the prolog server
    /// </param>
    /// <returns> the answer from the prolog server
    /// </returns>
    /// <exception cref="System.IO.IOException"> if an error occurs. A possible cause is a timeout.
    /// </exception>
    /// <seealso cref="se.sics.prologbeans.PrologSession.Timeout"/>
    public virtual QueryAnswer executeQuery(System.String query, Bindings bindings, System.String sessionID)
    {
        return new QueryAnswer(send(query, bindings, sessionID), bindings);
    }

    private bool is_valid_latin1(String str)
    {
        for (int i = 0, len = str.Length; i < len ; i++)
        {
            if (str[i] > 255)
            {
                return false;
            }
        }
        return true;
    }

    //       class IllegalCharacterSetException:Exception
    //  {
    //    IllegalCharacterSetException(String str):base(str)
    //      {
    //      }
    //  }


    //UPGRADE_NOTE: Synchronized keyword was removed from method 'send'. Lock expression was added. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1027"'
    private Term send(System.String query, Bindings bindings, System.String sessionID)
    {
        lock (this)
        {
            if (parentSession != null)
            {
                return parentSession.send(query, bindings, sessionID);
            }
            try
            {
                initSend();
                int len = sessionID == null ? 2 : 3;
                // Write a fastrw term
                //       output.write(FastParser.VERSION);
                //       output.write(FastParser.COMPOUND);
                //       output.write("query".getBytes());
                //       output.write(0);
                //       output.write(len);
                output.writeCompound("query", len);
                //       PBString.fastWrite(output, query + ".");
                // [PD] Quintus 3.5; read in Quintus can be confused unless there
                //                   is whitespace after the full stop.
                //output.writeString(query + ".");
                // [PD] 3.12.3 Do not allow characters outside Latin1 in query names.
                if (! is_valid_latin1(query))
                {
                    throw new Exception("Non ISO-8895-1 character in query: " + query);
                }
                output.writeString(query + ". ");
                if (bindings == null)
                {
                    PBList.NIL.fastWrite(output);
                }
                else
                {
                    bindings.fastWrite(output);
                }
                if (sessionID != null)
                {
                    //  PBAtomic.fastrwWrite(output, PBAtomic.ATOM, sessionID);
                    output.writeAtom(sessionID);
                }
                output.commit();
                return parser.parseProlog(input);
            }
            catch (System.IO.IOException e)
            {
                close();
                throw e;
            }
            finally
            {
                finishSend();
            }
        }
    }

    //UPGRADE_NOTE: Synchronized keyword was removed from method 'sendAtom'. Lock expression was added. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1027"'
    private Term sendAtom(System.String commandName, System.String argument)
    {
        lock (this)
        {
            if (parentSession != null)
            {
                return parentSession.sendAtom(commandName, argument);
            }
            try
            {
                initSend();
                // Write a fastrw term
                //       output.write(FastParser.VERSION);
                //       output.write(FastParser.COMPOUND);
                //       output.write(commandName.getBytes());
                //       output.write(0);
                //       output.write(1);
                //       PBAtomic.fastrwWrite(output, PBAtomic.ATOM, argument);
                //       output.flush();
                output.writeCompound(commandName, 1);
                output.writeAtom(argument);
                output.commit();
                return parser.parseProlog(input);
            }
            catch (System.IO.IOException e)
            {
                //SupportClass.WriteStackTrace(e, Console.Error);
                //Starcounter.LogManager.Error("Starcounter", null, "prologbeans", e);
                logSource.LogException(e);
                close();
                return null;
            }
            finally
            {
                finishSend();
            }
        }
    }

    private void  initSend()
    {
        if ((flags & AUTO_CONNECT) != 0)
        {
            connectToServer();
        }
        if (output == null)
        {
            throw new System.IO.IOException("no connection to Prolog Server");
        }
        sendTime = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
        if (timeout > 0)
        {
            PBMonitor.Default.queryStarted(this);
            isAddedToMonitor = true;
        }
    }

    private void  finishSend()
    {
        sendTime = - 1L;
        if (isAddedToMonitor)
        {
            PBMonitor.Default.queryFinished(this);
            isAddedToMonitor = false;
        }
        if ((flags & ALWAYS_CLOSE) > 0)
        {
            close();
        }
    }

    /// <summary> Connects to the Prolog server. By default
    /// <c>executeQuery</c> will automatically connect to the
    /// server when called.
    /// </summary>
    public virtual void  connect()
    {
        if (parentSession != null)
        {
            parentSession.connect();
        }
        else
        {
            connectToServer();
        }
    }

    private void  connectToServer()
    {
        if (this.connection == null)
        {
            System.Net.Sockets.TcpClient connection = new System.Net.Sockets.TcpClient(host, port);
            output = new FastWriter((System.IO.Stream) connection.GetStream());
            input = new System.IO.BufferedStream((System.IO.Stream) connection.GetStream());
            this.connection = connection;
        }
    }


    /// <summary> Closes the connection with the Prolog server. The connection can
    /// be opened again with <c>connect</c>.
    /// </summary>
    public virtual void  disconnect()
    {
        if (parentSession != null)
        {
            parentSession.close();
        }
        else
        {
            close();
        }
    }

    // Close connection when things have gone wrong...
    private void  close()
    {
        // System.out.println("Closing Connection...");
        try
        {
            sendTime = - 1L;
            if (output != null)
            {
                output.close();
                output = null;
            }
            if (input != null)
            {
                input.Close();
                input = null;
            }
            if (connection != null)
            {
                connection.Close();
                connection = null;
            }
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



    internal virtual void  cancelQuery()
    {
        isAddedToMonitor = false;
        if (sendTime != - 1L)
        {
            close();
        }
    }
}
// PrologSession
}
