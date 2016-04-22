// ***********************************************************************
// <copyright file="SessionDictionary.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Starcounter.Advanced;
using Starcounter.Internal;
using Starcounter.Logging;
using Starcounter.XSON;

namespace Starcounter {
    [Flags]
    public enum SessionOptions : int {
        Default = 0,
        IncludeSchema = 1,
        PatchVersioning = 2,
        StrictPatchRejection = 4,
//        DisableProtocolOT = 8,
        IncludeNamespaces = 16
    }

    /// <summary>
    /// Session destroy delegate.
    /// </summary>
    class SessionDestroyInfo {
        internal String AppName;
        internal Action<Session> DestroyDelegate;
    }

    /// <summary>
    /// Class representing session.
    /// </summary>
    public sealed class Session : IAppsSession, IDisposable {
        private class TransactionRef {
            internal int Refs;
            internal TransactionHandle Handle;
        }

        [ThreadStatic]
        private static Session _current;

        private static JsonPatch jsonPatch_ = new JsonPatch();

        private static LogSource log = new LogSource("Starcounter");

        /// <summary>
        /// To which scheduler this session belongs.
        /// </summary>
        Byte schedulerId_;

        /// <summary>
        /// List of destroy delegates for this session.
        /// </summary>
        List<SessionDestroyInfo> destroyDelegates_;

        private bool _isInUse;
        private Dictionary<string, int> _indexPerApplication;
       
        private List<Json> _stateList;
        private SessionOptions sessionOptions;
        private int publicViewModelIndex;

        private AutoResetEvent waitObj;

        /// <summary>
        /// Array of transactions that is managed by this session. All transaction here 
        /// will be cleaned up when either a json with transaction attached is removed 
        /// or when the session is cleaned up.
        /// </summary>
        private List<TransactionRef> transactions;

        /// <summary>
        /// Namespaces should only be added when the public viewmodel is serialized
        /// and when patches are sent AND if the option is set. Otherwise no namespaces
        /// and no siblings should be serialized.
        /// </summary>
        internal bool enableNamespaces = false;

        public Session() : this(SessionOptions.Default) {
        }

        public Session(SessionOptions options, bool? includeNamespaces = null) {
            _indexPerApplication = new Dictionary<string, int>();
            _stateList = new List<Json>();
            sessionOptions = options;
            transactions = new List<TransactionRef>();
            publicViewModelIndex = -1;

            bool b = StarcounterEnvironment.WrapJsonInNamespaces;
            if (includeNamespaces.HasValue)
                b = includeNamespaces.Value;

            if (b)
                sessionOptions |= SessionOptions.IncludeNamespaces;
            
            UInt32 errCode = 0;
           
            ScSessionStruct sss = new ScSessionStruct(true);
            errCode = GlobalSessions.AllGlobalSessions.CreateNewSession(ref sss, this);
           
            if (errCode != 0)
                throw ErrorCode.ToException(errCode);

            waitObj = new AutoResetEvent(true);

            // Getting current scheduler on which the session was created.
            schedulerId_ = StarcounterEnvironment.CurrentSchedulerId;
        }

        /// <summary>
        /// Checks if session is used on the owning scheduler.
        /// </summary>
        void CheckCorrectScheduler() {

            // Checking if on the owning scheduler.
            if (schedulerId_ != StarcounterEnvironment.CurrentSchedulerId) {
                throw new InvalidOperationException("You are trying to use the session on different scheduler.");
            }
        }

        /// <summary>
        /// Event which is called when session is being destroyed (timeout, manual, etc).
        /// </summary>
        public void AddDestroyDelegate(Action<Session> destroyDelegate) {

            SessionDestroyInfo sdi = new SessionDestroyInfo() {
                AppName = StarcounterEnvironment.AppName,
                DestroyDelegate = destroyDelegate
            };

            if (destroyDelegates_ == null) {
                destroyDelegates_ = new List<SessionDestroyInfo>();
            }

            destroyDelegates_.Add(sdi);
        }

        /// <summary>
        /// Runs session destruction delegates.
        /// </summary>
        void RunDestroyDelegates(Session s) {

            foreach (SessionDestroyInfo sdi in destroyDelegates_) {

                StarcounterEnvironment.RunWithinApplication(sdi.AppName, () => {
                    sdi.DestroyDelegate(s);
                });
            }
        }

        /// <summary>
        /// Configured options for this session.
        /// </summary>
        public SessionOptions Options {
            get { return sessionOptions; }
        }

        /// <summary>
        /// Checks if given option exists in session options.
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public bool CheckOption(SessionOptions option) {
            return (sessionOptions & option) == option;
        }

        /// <summary>
        /// Calculates the patch and pushes it on WebSocket.
        /// </summary>
        public void CalculatePatchAndPushOnWebSocket() {

            // Checking if on the owning scheduler.
            CheckCorrectScheduler();

            // Checking if there is an active WebSocket.
            if (ActiveWebSocket == null)
                return;

            // Calculating the patch.
            Byte[] patch;
            Int32 sizeBytes = jsonPatch_.Generate(
                PublicViewModel, 
                true, 
                CheckOption(SessionOptions.IncludeNamespaces), 
                out patch);

            // Sending the patch bytes to the client.
            ActiveWebSocket.Send(patch, sizeBytes, true);
        }

        /// <summary>
        /// Resembles session task to be used in Session.ScheduleTask and Session.ForAll.
        /// </summary>
        /// <param name="session">Session on which this task is to be called. Note that Session value can still be null (if session was destroyed in the meantime).</param>
        /// <param name="sessionId">Session ASCII representation (useful in case if Session value is null).</param>
        public delegate void SessionTask(Session session, String sessionId);

        /// <summary>
        /// Runs a given session for each task on current scheduler.
        /// </summary>
        static void RunForSessionsOnCurrentScheduler(SessionTask task) {

            SchedulerSessions ss =
                GlobalSessions.AllGlobalSessions.GetSchedulerSessions(StarcounterEnvironment.CurrentSchedulerId);

            LinkedListNode<UInt32> usedSessionIndexNode = ss.UsedSessionIndexes.First;

            while (usedSessionIndexNode != null) {

                LinkedListNode<UInt32> nextUsedSessionIndexNode = usedSessionIndexNode.Next;

                // Getting session instance.
                ScSessionClass sessionClass = ss.GetAppsSessionIfAlive(usedSessionIndexNode.Value);

                // Checking if session is created at all.
                if (null != sessionClass) {

                    Session s = (Session) sessionClass.apps_session_int_;
                    if (null != s) {
                        s.Use(task, s.ToAsciiString());
                    }
                }

                // Getting next used session.
                usedSessionIndexNode = nextUsedSessionIndexNode;
            }
        }

        /// <summary>
        /// Running given task on each active session on each owning scheduler.
        /// </summary>
        /// <param name="task">Task to run on session. Second string parameter is the session ASCII representation.</param>
        /// <param name="waitForCompletion">Should we wait for the task to be completed.</param>
        public static void ForAll(SessionTask task, Boolean waitForCompletion = false) {

            for (Byte i = 0; i < StarcounterEnvironment.SchedulerCount; i++) {

                Byte schedId = i;

                Scheduling.ScheduleTask(
                    () => { RunForSessionsOnCurrentScheduler(task); },
                    waitForCompletion,
                    schedId);
            }
        }

        /// <summary>
        /// Schedule a task on specific session.
        /// </summary>
        /// <param name="sessionId">String representing the session (string is obtained from Session.ToAsciiString()).</param>
        /// <param name="task">Task to run on session. Note that Session value can still be null (if session was destroyed in the meantime).
        /// Second string parameter is the session ASCII representation (useful in case if Session value is null).</param>
        /// <param name="waitForCompletion">Should we wait for the task to be completed.</param>
        public static void ScheduleTask(String sessionId, SessionTask task, Boolean waitForCompletion = false) {

            Session savedCurrentSession = Session.Current;
            try {
                if (savedCurrentSession != null && savedCurrentSession.ToAsciiString().Equals(sessionId)) {
                    // Trying to schedule a task on the current session. Execute the task directly.
                    task(savedCurrentSession, sessionId);
                    return;
                }

                // Checking if we already have a current session.
                if (null != savedCurrentSession) {
                    savedCurrentSession.StopUsing();
                }

                // Getting session structure from string.
                ScSessionStruct ss = new ScSessionStruct();
                ss.ParseFromString(sessionId);

                // Checking if we are on the same scheduler.
                if (ss.schedulerId_ == StarcounterEnvironment.CurrentSchedulerId) {
                    TransactionHandle oldTrans = TransactionHandle.Invalid;

                    if (StarcounterBase._DB != null && StarcounterBase._DB.HasDatabase) {
                        // Since we are running the task on the same scheduler (i.e no scheduling, just 
                        // executing the task) we need to set the current active transaction to avoid 
                        // sharing transactions between sessions.
                        oldTrans = StarcounterBase.TransactionManager.CurrentTransaction;
                        StarcounterBase.TransactionManager.CreateImplicitAndSetCurrent();
                    }

                    try {
                        Session s = (Session)GlobalSessions.AllGlobalSessions.GetAppsSessionInterface(ref ss);

                        // Checking if session is dead.
                        if (null != s) {
                            s.Use(task, sessionId);
                        } else {
                            task(null, sessionId);
                        }
                    } finally {
                        if (StarcounterBase._DB != null && StarcounterBase._DB.HasDatabase) {
                            StarcounterBase.TransactionManager.CurrentTransaction = oldTrans;
                        }
                    }

                } else {

                    Scheduling.ScheduleTask(() => {

                        Session s = (Session)GlobalSessions.AllGlobalSessions.GetAppsSessionInterface(ref ss);

                        // Checking if session is dead.
                        if (null != s) {
                            s.Use(task, sessionId);
                        } else {
                            task(null, sessionId);
                        }

                    }, waitForCompletion, ss.schedulerId_);
                }

            } finally {

                // Checking if we need to restore current session.
                if (null != savedCurrentSession) {
                    savedCurrentSession.StartUsing();
                }
            }
        }

        /// <summary>
        /// Schedule a task on given sessions.
        /// </summary>
        /// <param name="sessionId">String representing the session (string is obtained from Session.ToAsciiString()).</param>
        /// <param name="task">Task to run on session. Note that Session value can still be null (if session was destroyed in the meantime).
        /// Second string parameter is the session ASCII representation (useful in case if Session value is null).</param>
        /// <param name="waitForCompletion">Should we wait for the task to be completed.</param>
        public static void ScheduleTask(IEnumerable<String> sessionIds, SessionTask task, Boolean waitForCompletion = false) {

            List<String> sessionIdsList = new List<string>();
            foreach (String s in sessionIds) {
                sessionIdsList.Add(s);
            }

            for (Int32 i = 0; i < sessionIdsList.Count; i++) {
                String s = sessionIdsList[i];
                ScheduleTask(s, task, waitForCompletion);
            }
        }

        /// <summary>
        /// Returns ASCII string representing the session.
        /// </summary>
        /// <returns></returns>
        public String ToAsciiString() {
            return InternalSession.ToAsciiString();
        }

        /// <summary>
        /// Indicates if user wants to use session cookie.
        /// </summary>
        public Boolean UseSessionCookie {
            get { return InternalSession.use_session_cookie_; }
            set { InternalSession.use_session_cookie_ = value; }
        }

        /// <summary>
        /// Getting internal session.
        /// </summary>
        public ScSessionClass InternalSession { get; set; }

        /// <summary>
        /// Currently active WebSocket.
        /// </summary>
        public WebSocket ActiveWebSocket { get; set; }

        /// <summary>
        /// Current static session object.
        /// </summary>
        public static Session Current {
            get {
                return _current;
            }
            set {
                if (value != null)
                    value.StartUsing();
                else if (_current != null)
                    _current.StopUsing();
            }
        }

        /// <summary>
        /// Gets or sets session data for one specific application. 
        /// </summary>
        public Json Data {
            get {

                // Checking if on the owning scheduler.
                CheckCorrectScheduler();

                int stateIndex;
                string appName;

                appName = StarcounterEnvironment.AppName;
                if (appName == null)
                    return null;

                if (!_indexPerApplication.TryGetValue(appName, out stateIndex))
                    return null;

                return _stateList[stateIndex];
            }
            set {

                // Checking if on the owning scheduler.
                CheckCorrectScheduler();

                int stateIndex;
                string appName;

                if (value != null) {
                    if (value.Parent != null)
                        throw ErrorCode.ToException(Error.SCERRSESSIONJSONNOTROOT);

                    if (value._Session != null && value._Session != this)
                        throw ErrorCode.ToException(Error.SCERRJSONSETONOTHERSESSION);
                }

                appName = StarcounterEnvironment.AppName;
                if (appName != null) {
                    if (!_indexPerApplication.TryGetValue(appName, out stateIndex)) {
                        stateIndex = _stateList.Count;
                        _stateList.Add(value);
                        _indexPerApplication.Add(appName, stateIndex);
                    } else {
                        _stateList[stateIndex] = value;
                    }

                    if (value != null) {
                        value._Session = this;

                        if (publicViewModelIndex == -1)
                            publicViewModelIndex = stateIndex;

                        if (stateIndex == publicViewModelIndex) {
                            ViewModelVersion version = null;
                            if (CheckOption(SessionOptions.PatchVersioning)) {
                                version = new ViewModelVersion();
                            }
                            value.ChangeLog = new ChangeLog(value, version);
                        }

                        value.OnSessionSet();
                    }

                    if (_current == null) {
                        StartUsing();
                    }
                }
            }
        }

        /// <summary>
        /// Getting session creation time. 
        /// </summary>
        public DateTime Created {
            get {
                return InternalSession.Created;
            }
        }

        /// <summary>
        /// Getting last active session time. 
        /// </summary>
        public DateTime LastActive {
            get {
                return InternalSession.LastActive;
            }
        }

        /// <summary>
        /// Session timeout.
        /// </summary>
        public UInt64 TimeoutMinutes {
            get {
                return InternalSession.TimeoutMinutes;
            }
            set {
                InternalSession.TimeoutMinutes = value;
            }
        }

        /// <summary>
        /// Internal session string.
        /// </summary>
        [System.Obsolete("Please use ToAsciiString() instead.")]
        public String SessionIdString {
            get { return InternalSession.ToAsciiString(); }
        }

        /// Returns True if session is being used now.
        /// </summary>
        /// <returns></returns>
        public bool IsBeingUsed() {
            return _isInUse;
        }

        void IAppsSession.StartUsing() {
            this.StartUsing();
        }

        void IAppsSession.StopUsing() {
            this.StopUsing();
        }

        /// <summary>
        /// Start using specific session.
        /// </summary>
        /// <returns>
        /// true if switched over to this session, false if the session 
        /// already was current.
        /// </returns>
        private bool StartUsing() {
            if (_current == this) 
                return false;

            if (_current != null) {
                throw ErrorCode.ToException(Error.SCERRANOTHERSESSIONACTIVE);
            }

            int count = 0;
            int noRetries = 3;
            while (true) {
                if (!waitObj.WaitOne(60 * 1000)) {
                    if (count++ < noRetries) {
                        log.LogWarning("Exclusive access to the session with id {0} could "
                                       +"not be obtained within the allotted time. Trying again ({1}/{2}).",
                                       this.ToAsciiString(),
                                       count,
                                       noRetries);
                        continue;
                    }
                    throw ErrorCode.ToException(Error.SCERRACQUIRESESSIONTIMEOUT, "Id: " + this.ToAsciiString());
                }
                break;
            }
            
            _isInUse = true;
            Session._current = this;
            return true;
        }

        /// <summary>
        /// Stop using specific session.
        /// </summary>
        private void StopUsing() {
            try {
                Debug.Assert(_current == this);

                DisposeUnreferencedTransactions();
                Session._current = null;
                _isInUse = false;
            } finally {
                waitObj.Set();
            }
        }
        
        /// <summary>
        /// Checks if session is active.
        /// </summary>
        /// <returns></returns>
        public Boolean IsAlive() {
            return (InternalSession != null) && (InternalSession.IsAlive());
        }

        /// <summary>
        /// Gets the public viewmodel
        /// </summary>
        /// <returns></returns>
        public Json PublicViewModel {
            get {
                if (publicViewModelIndex != -1)
                    return _stateList[publicViewModelIndex];
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool DatabaseHasBeenUpdatedInCurrentTask {
            get {
                return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HaveAddedOrRemovedObjects { get; set; }

        internal TransactionHandle RegisterTransaction(TransactionHandle handle) {
            TransactionRef tref = null;

            for (int i = 0; i < transactions.Count; i++) {
                if (transactions[i].Handle == handle) {
                    tref = transactions[i];
                    tref.Refs++;
                    break;
                }
            }

            if (tref == null) {
                // transaction not registered before. 
                tref = new TransactionRef();
                StarcounterBase.TransactionManager.ClaimOwnership(handle);
                handle.index = -1;
                tref.Handle = handle;
                tref.Refs = 1;
                transactions.Add(tref);
            }
            return handle;
        }

        internal void DeregisterTransaction(TransactionHandle handle) {
            TransactionRef tref = null;

            for (int i = 0; i < transactions.Count; i++) {
                if (transactions[i].Handle == handle) {
                    tref = transactions[i];
                    tref.Refs--;
                    break;
                }
            }

            Debug.Assert(tref != null);
        }

        private void DisposeUnreferencedTransactions() {
            TransactionRef tref;

            for (int i = (transactions.Count - 1); i >= 0; i--) {
                tref = transactions[i];
                Debug.Assert(tref.Refs >= 0);

                if (transactions[i].Refs == 0) {
                    // TODO:
                    // How do we handle exception here?
                    try {
                        StarcounterBase.TransactionManager.Dispose(tref.Handle);
                    } catch (Exception) {
                        throw;
                    }
                    transactions.RemoveAt(i);
                }
            }
        }

        private void DisposeAllTransactions() {
            for (int i = 0; i < transactions.Count; i++) {
                // TODO:
                // How do we handle exception here?
                try {
                    StarcounterBase.TransactionManager.Dispose(transactions[i].Handle);
                } catch (Exception) {
                    throw;
                }
            }
            transactions.Clear();
        }

        /// <summary>
        /// Destroys the session.
        /// </summary>
        public void Destroy() {

            _indexPerApplication.Clear();
            DisposeAllTransactions();

            // Checking if there is an active WebSocket.
            if (ActiveWebSocket != null) {

                ActiveWebSocket.Disconnect("Session is expired.", WebSocket.WebSocketCloseCodes.WS_CLOSE_GOING_DOWN);
                ActiveWebSocket.Session = null;
                ActiveWebSocket = null;
            }

            // Checking if destroy callback is supplied.
            if (null != destroyDelegates_) {
                RunDestroyDelegates(this);
                destroyDelegates_ = null;
            }

            // NOTE: Preventing recursive destroy call.
            if (InternalSession != null) {
                InternalSession.apps_session_int_ = null;
                InternalSession.Destroy();
                InternalSession = null;
            }

            Session._current = null;
        }

        /// <summary>
        /// Dispose functionality for session.
        /// </summary>
        void IDisposable.Dispose() {
            Destroy();
        }

        /// <summary>
        /// Executes the specified delegate inside the scope of the session,
        /// ensuring that only the caller have access for the duration of the call       
        /// </summary>
        /// <param name="action"></param>
        public void Use(Action action) {
            bool started = this.StartUsing();
            try {
                action();
            } finally {
                if (started)
                    this.StopUsing();
            }
        }

        /// <summary>
        /// Executes the specified delegate inside the scope of the session,
        /// ensuring that only the caller have access for the duration of the call       
        /// </summary>
        /// <param name="action"></param>
        void Use(SessionTask action, String sessionId) {
            bool started = this.StartUsing();
            try {
                action(this, sessionId);
            } finally {
                if (started)
                    this.StopUsing();
            }
        }

        /// <summary>
        /// Executes the specified delegate inside the scope of the session,
        /// ensuring that only the caller have access for the duration of the call
        /// </summary>
        /// <param name="action"></param>
        public void Use<T>(Action<T> action, T arg) {
            bool started = this.StartUsing();
            try {
                action(arg);
            } finally {
                if (started)
                    this.StopUsing();
            }
        }

        /// <summary>
        /// Executes the specified delegate inside the scope of the session,
        /// ensuring that only the caller have access for the duration of the call
        /// </summary>
        /// <param name="action"></param>
        public void Use<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2) {
            bool started = this.StartUsing();
            try {
                action(arg1, arg2);
            } finally {
                if (started)
                    this.StopUsing();
            }
        }

        /// <summary>
        /// Executes the specified delegate inside the scope of the session,
        /// ensuring that only the caller have access for the duration of the call
        /// </summary>
        /// <param name="action"></param>
        public void Use<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) {
            bool started = this.StartUsing();
            try {
                action(arg1, arg2, arg3);
            } finally {
                if (started)
                    this.StopUsing();
            }
        }

        /// <summary>
        /// Executes the specified delegate inside the scope of the session,
        /// ensuring that only the caller have access for the duration of the call
        /// </summary>
        /// <param name="func"></param>
        public T Use<T>(Func<T> func) {
            bool started = this.StartUsing();
            try {
                return func();
            } finally {
                if (started)
                    this.StopUsing();
            }
        }

        /// <summary>
        /// Executes the specified delegate inside the scope of the session,
        /// ensuring that only the caller have access for the duration of the call
        /// </summary>
        /// <param name="func"></param>
        public TRet Use<T, TRet>(Func<T, TRet> func, T arg) {
            bool started = this.StartUsing();
            try {
                return func(arg);
            } finally {
                if (started)
                    this.StopUsing();
            }
        }

        /// <summary>
        /// Executes the specified delegate inside the scope of the session,
        /// ensuring that only the caller have access for the duration of the call
        /// </summary>
        /// <param name="func"></param>
        public TRet Use<T1, T2, TRet>(Func<T1, T2, TRet> func, T1 arg1, T2 arg2) {
            bool started = this.StartUsing();
            try {
                return func(arg1, arg2);
            } finally {
                if (started)
                    this.StopUsing();
            }
        }
    }
}
