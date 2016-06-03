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
    /// <summary>
    /// Class representing session.
    /// </summary>
    public sealed class Session : IAppsSession, IDisposable {
        /// <summary>
        /// Resembles session task to be used in Session.ScheduleTask and Session.ForAll.
        /// </summary>
        /// <param name="session">Session on which this task is to be called. Note that Session value can still be null (if session was destroyed in the meantime).</param>
        /// <param name="sessionId">Session ASCII representation (useful in case if Session value is null).</param>
        public delegate void SessionTask(Session session, String sessionId);

        /// <summary>
        /// 
        /// </summary>
        private class TransactionRef {
            internal int Refs;
            internal TransactionHandle Handle;
        }

        /// <summary>
        /// Session destroy delegate.
        /// </summary>
        private class SessionDestroyInfo {
            internal String AppName;
            internal Action<Session> DestroyDelegate;
        }

        [ThreadStatic]
        private static Session current;

        private static JsonPatch jsonPatch = new JsonPatch();
        private static LogSource log = new LogSource("Starcounter");

        /// <summary>
        /// To which scheduler this session belongs.
        /// </summary>
        private byte schedulerId;

        /// <summary>
        /// List of destroy delegates for this session.
        /// </summary>
        private List<SessionDestroyInfo> destroyDelegates;

        private bool isInUse;
        private Dictionary<string, int> indexPerApplication;
        private List<Json> stateList;
        private SessionOptions sessionOptions;
        private int publicViewModelIndex;
        private AutoResetEvent waitObj;
        private WebSocket activeWebSocket;

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

        /// <summary>
        /// If set to true values that are bound will not be read several times when generating
        /// changes from changelog and creating patches.
        /// </summary>
        internal bool enableCachedReads = false;

        public Session() : this(SessionOptions.Default) {

        }

        public Session(SessionOptions options, bool? includeNamespaces = null) {
            indexPerApplication = new Dictionary<string, int>();
            stateList = new List<Json>();
            sessionOptions = options;
            transactions = new List<TransactionRef>();
            publicViewModelIndex = -1;
            
            bool b = StarcounterEnvironment.WrapJsonInNamespaces;
            if (includeNamespaces.HasValue)
                b = includeNamespaces.Value;

            if (b)
                sessionOptions |= SessionOptions.IncludeNamespaces;
            
            uint errCode = 0;
           
            ScSessionStruct sss = new ScSessionStruct(true);
            errCode = GlobalSessions.AllGlobalSessions.CreateNewSession(ref sss, this);
           
            if (errCode != 0)
                throw ErrorCode.ToException(errCode);

            waitObj = new AutoResetEvent(true);

            // Getting current scheduler on which the session was created.
            schedulerId = StarcounterEnvironment.CurrentSchedulerId;

            StartUsing();
        }

        /// <summary>
        /// Checks if session is used on the owning scheduler.
        /// </summary>
        private void CheckCorrectScheduler() {
            if (schedulerId == StarcounterEnvironment.CurrentSchedulerId)
                return;

            throw ErrorCode.ToException(Error.SCERRSESSIONINCORRECTSCHEDULER);
        }

        /// <summary>
        /// Verify that the caller have access to the session.
        /// </summary>
        private void VerifyAccess() {
            CheckCorrectScheduler();

            if (this == current)
                return;

            throw ErrorCode.ToException(Error.SCERRACCESSTOSESSIONNOTACQUIRED);
        }

        /// <summary>
        /// Event which is called when session is being destroyed (timeout, manual, etc).
        /// </summary>
        public void AddDestroyDelegate(Action<Session> destroyDelegate) {
            SessionDestroyInfo sdi = new SessionDestroyInfo() {
                AppName = StarcounterEnvironment.AppName,
                DestroyDelegate = destroyDelegate
            };

            if (destroyDelegates == null) {
                destroyDelegates = new List<SessionDestroyInfo>();
            }

            destroyDelegates.Add(sdi);
        }

        /// <summary>
        /// Runs session destruction delegates.
        /// </summary>
        void RunDestroyDelegates(Session s) {
            foreach (SessionDestroyInfo sdi in destroyDelegates) {
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
        /// Checks if given option is set in sessionoptions.
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
            VerifyAccess();

            // Checking if there is an active WebSocket.
            if (ActiveWebSocket == null)
                return;

            // Calculating the patch.
            byte[] patch;
            int sizeBytes = jsonPatch.Generate(
                PublicViewModel, 
                true, 
                CheckOption(SessionOptions.IncludeNamespaces), 
                out patch);

            // Sending the patch bytes to the client.
            ActiveWebSocket.Send(patch, sizeBytes, true);
        }
        
        /// <summary>
        /// Runs a given session for each task on current scheduler.
        /// </summary>
        private static void RunForSessionsOnCurrentScheduler(SessionTask task) {
            SchedulerSessions ss =
                GlobalSessions.AllGlobalSessions.GetSchedulerSessions(StarcounterEnvironment.CurrentSchedulerId);

            LinkedListNode<uint> usedSessionIndexNode = ss.UsedSessionIndexes.First;

            while (usedSessionIndexNode != null) {
                LinkedListNode<uint> nextUsedSessionIndexNode = usedSessionIndexNode.Next;

                // Getting session instance.
                ScSessionClass sessionClass = ss.GetAppsSessionIfAlive(usedSessionIndexNode.Value);

                // Checking if session is created at all.
                if (null != sessionClass) {
                    Session s = (Session) sessionClass.apps_session_int_;
                    if (null != s) {
                        s.Use(task, s.SessionId);
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
            for (byte i = 0; i < StarcounterEnvironment.SchedulerCount; i++) {
                byte schedId = i;

                Scheduling.ScheduleTask(() => { RunForSessionsOnCurrentScheduler(task); },
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
            ScSessionStruct ss = new ScSessionStruct();
            ss.ParseFromString(sessionId);

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
        [System.Obsolete("Please use SessionId property instead.")]
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
        public WebSocket ActiveWebSocket {
            get {
                VerifyAccess();
                return activeWebSocket;
            }
            set {
                VerifyAccess();
                activeWebSocket = value;
            }
        }

        /// <summary>
        /// Current static session object.
        /// </summary>
        public static Session Current {
            get {
                return current;
            }
            set {
                if (value != null)
                    value.StartUsing();
                else if (current != null)
                    current.StopUsing(true);
            }
        }

        /// <summary>
        /// Gets or sets session data for one specific application. 
        /// </summary>
        public Json Data {
            get {
                VerifyAccess();

                int stateIndex;
                string appName = StarcounterEnvironment.AppName;
                if (appName == null)
                    return null;

                if (!indexPerApplication.TryGetValue(appName, out stateIndex))
                    return null;

                return stateList[stateIndex];
            }
            set {
                VerifyAccess();

                Json oldJson = null;
                int stateIndex;
                string appName;

                if (value != null) {
                    if (value.Parent != null)
                        throw ErrorCode.ToException(Error.SCERRSESSIONJSONNOTROOT);

                    if (value.session != null && value.session != this)
                        throw ErrorCode.ToException(Error.SCERRJSONSETONOTHERSESSION);
                }

                appName = StarcounterEnvironment.AppName;
                if (appName != null) {
                    if (!indexPerApplication.TryGetValue(appName, out stateIndex)) {
                        stateIndex = stateList.Count;
                        stateList.Add(value);
                        indexPerApplication.Add(appName, stateIndex);
                    } else {
                        oldJson = stateList[stateIndex];
                        stateList[stateIndex] = value;
                    }

                    if (value != null) {
                        value.session = this;

                        if (publicViewModelIndex == -1)
                            publicViewModelIndex = stateIndex;

                        if (stateIndex == publicViewModelIndex) {
                            ViewModelVersion version = null;

                            if (oldJson != null) {
                                // Existing public viewmodel exists. ChangeLog should be reused.
                                oldJson.ChangeLog.ChangeEmployer(value);
                            } else {
                                if (CheckOption(SessionOptions.PatchVersioning)) {
                                    version = new ViewModelVersion();
                                }
                                value.ChangeLog = new ChangeLog(value, version);
                            }
                        }

                        value.OnSessionSet();
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
        [System.Obsolete("Please use SessionId property instead.")]
        public String SessionIdString {
            get { return InternalSession.ToAsciiString(); }
        }

        /// <summary>
        /// String representation of the session object.
        /// Used, for example, for storing session and then using it as parameter for Session.ScheduleTask.
        /// </summary>
        public String SessionId
        {
            get
            {
                return InternalSession.ToAsciiString();
            }
        }

        /// Returns True if session is being used now.
        /// </summary>
        /// <returns></returns>
        public bool IsBeingUsed() {
            return isInUse;
        }

        void IAppsSession.StartUsing() {
            this.StartUsing();
        }

        void IAppsSession.StopUsing() {
            this.StopUsing(true);
        }

        /// <summary>
        /// Start using specific session.
        /// </summary>
        /// <returns>
        /// true if switched over to this session, false if the session 
        /// already was current.
        /// </returns>
        private bool StartUsing() {
            if (current == this) 
                return false;

            if (current != null) {
                throw ErrorCode.ToException(Error.SCERRANOTHERSESSIONACTIVE);
            }

            int count = 0;
            int noRetries = 3;
            while (true) {
                if (!waitObj.WaitOne(60 * 1000)) {
                    if (count++ < noRetries) {
                        log.LogWarning("Exclusive access to the session with id {0} could "
                                       +"not be obtained within the allotted time. Trying again ({1}/{2}).",
                                       this.SessionId,
                                       count,
                                       noRetries);
                        continue;
                    }
                    throw ErrorCode.ToException(Error.SCERRACQUIRESESSIONTIMEOUT, "Id: " + this.SessionId);
                }
                break;
            }
            
            isInUse = true;
            Session.current = this;
            return true;
        }

        /// <summary>
        /// Stop using specific session.
        /// </summary>
        private void StopUsing(bool disposeUnrefTrans) {
            try {
                Debug.Assert(current == this);

                if (disposeUnrefTrans)
                    DisposeUnreferencedTransactions();

                Session.current = null;
                isInUse = false;
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
                VerifyAccess();

                if (publicViewModelIndex != -1)
                    return stateList[publicViewModelIndex];
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
            VerifyAccess();

            indexPerApplication.Clear();
            DisposeAllTransactions();

            // Checking if there is an active WebSocket.
            if (activeWebSocket != null) {
                activeWebSocket.Disconnect("Session is expired.", WebSocket.WebSocketCloseCodes.WS_CLOSE_GOING_DOWN);
                activeWebSocket.Session = null;
                activeWebSocket = null;
            }

            // Checking if destroy callback is supplied.
            if (null != destroyDelegates) {
                RunDestroyDelegates(this);
                destroyDelegates = null;
            }

            // NOTE: Preventing recursive destroy call.
            if (InternalSession != null) {
                InternalSession.apps_session_int_ = null;
                InternalSession.Destroy();
                InternalSession = null;
            }
            Session.current = null;
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
            Session oldCurrent = Session.current;
            if (oldCurrent != null && oldCurrent != this)
                oldCurrent.StopUsing(false);
            
            bool started = this.StartUsing();
            try {
                action();
            } finally {
                if (started)
                    this.StopUsing(true);

                if (oldCurrent != null)
                    oldCurrent.StartUsing();
            }
        }

        /// <summary>
        /// Executes the specified delegate inside the scope of the session,
        /// ensuring that only the caller have access for the duration of the call       
        /// </summary>
        /// <param name="action"></param>
        void Use(SessionTask action, String sessionId) {
            Session oldCurrent = Session.current;
            if (oldCurrent != null && oldCurrent != this)
                oldCurrent.StopUsing(false);

            bool started = this.StartUsing();
            try {
                action(this, sessionId);
            } finally {
                if (started)
                    this.StopUsing(true);

                if (oldCurrent != null)
                    oldCurrent.StartUsing();
            }
        }

        /// <summary>
        /// Executes the specified delegate inside the scope of the session,
        /// ensuring that only the caller have access for the duration of the call
        /// </summary>
        /// <param name="action"></param>
        public void Use<T>(Action<T> action, T arg) {
            Session oldCurrent = Session.current;
            if (oldCurrent != null && oldCurrent != this)
                oldCurrent.StopUsing(false);

            bool started = this.StartUsing();
            try {
                action(arg);
            } finally {
                if (started)
                    this.StopUsing(true);

                if (oldCurrent != null)
                    oldCurrent.StartUsing();
            }
        }

        /// <summary>
        /// Executes the specified delegate inside the scope of the session,
        /// ensuring that only the caller have access for the duration of the call
        /// </summary>
        /// <param name="action"></param>
        public void Use<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2) {
            Session oldCurrent = Session.current;
            if (oldCurrent != null && oldCurrent != this)
                oldCurrent.StopUsing(false);

            bool started = this.StartUsing();
            try {
                action(arg1, arg2);
            } finally {
                if (started)
                    this.StopUsing(true);

                if (oldCurrent != null)
                    oldCurrent.StartUsing();
            }
        }

        /// <summary>
        /// Executes the specified delegate inside the scope of the session,
        /// ensuring that only the caller have access for the duration of the call
        /// </summary>
        /// <param name="action"></param>
        public void Use<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) {
            Session oldCurrent = Session.current;
            if (oldCurrent != null && oldCurrent != this)
                oldCurrent.StopUsing(false);

            bool started = this.StartUsing();
            try {
                action(arg1, arg2, arg3);
            } finally {
                if (started)
                    this.StopUsing(true);

                if (oldCurrent != null)
                    oldCurrent.StartUsing();
            }
        }

        /// <summary>
        /// Executes the specified delegate inside the scope of the session,
        /// ensuring that only the caller have access for the duration of the call
        /// </summary>
        /// <param name="func"></param>
        public T Use<T>(Func<T> func) {
            Session oldCurrent = Session.current;
            if (oldCurrent != null && oldCurrent != this)
                oldCurrent.StopUsing(false);

            bool started = this.StartUsing();
            try {
                return func();
            } finally {
                if (started)
                    this.StopUsing(true);

                if (oldCurrent != null)
                    oldCurrent.StartUsing();
            }
        }

        /// <summary>
        /// Executes the specified delegate inside the scope of the session,
        /// ensuring that only the caller have access for the duration of the call
        /// </summary>
        /// <param name="func"></param>
        public TRet Use<T, TRet>(Func<T, TRet> func, T arg) {
            Session oldCurrent = Session.current;
            if (oldCurrent != null && oldCurrent != this)
                oldCurrent.StopUsing(false);

            bool started = this.StartUsing();
            try {
                return func(arg);
            } finally {
                if (started)
                    this.StopUsing(true);

                if (oldCurrent != null)
                    oldCurrent.StartUsing();
            }
        }

        /// <summary>
        /// Executes the specified delegate inside the scope of the session,
        /// ensuring that only the caller have access for the duration of the call
        /// </summary>
        /// <param name="func"></param>
        public TRet Use<T1, T2, TRet>(Func<T1, T2, TRet> func, T1 arg1, T2 arg2) {
            Session oldCurrent = Session.current;
            if (oldCurrent != null && oldCurrent != this)
                oldCurrent.StopUsing(false);

            bool started = this.StartUsing();
            try {
                return func(arg1, arg2);
            } finally {
                if (started)
                    this.StopUsing(true);

                if (oldCurrent != null)
                    oldCurrent.StartUsing();
            }
        }
    }
}
