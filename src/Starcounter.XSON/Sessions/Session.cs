 ﻿// ***********************************************************************
// <copyright file="SessionDictionary.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Starcounter.Internal;
using Starcounter.Internal.XSON;
using Starcounter.Templates;
using Starcounter.Advanced;
using Starcounter.XSON;
using System.Threading;

namespace Starcounter {
    [Flags]
    public enum SessionOptions : int {
        Default = 0,
        IncludeSchema = 1,
        PatchVersioning = 2,
        StrictPatchRejection = 4,
//        DisableProtocolOT = 8,
        IncludeNamespaces = 16,
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class Session : IAppsSession, IDisposable {
        private class TransactionRef {
            internal int Refs;
            internal TransactionHandle Handle;
        }

        [ThreadStatic]
        private static Session _current;

        private static JsonPatch jsonPatch_ = new JsonPatch();​

        /// <summary>
        /// Event which is called when session is being destroyed (timeout, manual, etc).
        /// </summary>
        public event EventHandler Destroyed;

        private bool _isInUse;
        private Dictionary<string, int> _indexPerApplication;
       
        private List<Json> _stateList;
        private SessionOptions sessionOptions;
        private int publicViewModelIndex;

        /// <summary>
        /// Array of transactions that is managed by this session. All transaction here 
        /// will be cleaned up when either a json with transaction attached is removed 
        /// or when the session is cleaned up.
        /// </summary>
        private List<TransactionRef> transactions;

        

        ///// <summary>
        ///// Versioning for local and remote version of the viewmodel
        ///// </summary>
        //private long clientVersion;
        //private long serverVersion;

        ///// <summary>
        ///// The clients current serverversion. Used to check if OT is needed.
        ///// </summary>
        //private long clientServerVersion;

        public Session() : this(SessionOptions.Default) {
        }

        public Session(SessionOptions options, bool? includeNamespaces = null) {
//            _brandNew = true;
//            _changes = new List<Change>();
            _indexPerApplication = new Dictionary<string, int>();
            _stateList = new List<Json>();
            sessionOptions = options;
//            clientServerVersion = serverVersion;
            transactions = new List<TransactionRef>();
            publicViewModelIndex = -1;

            bool b = StarcounterEnvironment.PolyjuiceAppsFlag;
            if (includeNamespaces.HasValue)
                b = includeNamespaces.Value;

            if (b)
                sessionOptions |= SessionOptions.IncludeNamespaces;
            
            UInt32 errCode = 0;
           
            ScSessionStruct sss = new ScSessionStruct(true);
            errCode = GlobalSessions.AllGlobalSessions.CreateNewSession(ref sss, this);
           
            if (errCode != 0)
                throw ErrorCode.ToException(errCode);
        }

        public SessionOptions Options {
            get { return sessionOptions; }
        }

        public bool CheckOption(SessionOptions option) {
            return (sessionOptions & option) == option;
        }

        ///// <summary>
        ///// Gets the versionnumber for the remote version of the viewmodel.
        ///// </summary>
        //public long ClientVersion {
        //    get { return clientVersion; }
        //    internal set { clientVersion = value; }
        //}

        ///// <summary>
        ///// Gets the versionnumber for the local version of the viewmodel.
        ///// </summary>
        //public long ServerVersion {
        //    get { return serverVersion; }
        //}

        //internal long ClientServerVersion {
        //    get { return clientServerVersion; }
        //    /*internal*/ set { clientServerVersion = value; }
        //}

        /// <summary>
        /// Runs a task asynchronously on current scheduler.
        /// </summary>
        public void RunSync(Action action, Byte schedId = Byte.MaxValue) {
            InternalSession.RunSync(action, schedId);
        }

        /// <summary>
        /// Calculates the patch and pushes it on WebSocket.
        /// </summary>
        public void CalculatePatchAndPushOnWebSocket() {

            // Checking if there is an active WebSocket.
            if (ActiveWebSocket == null)
                return;

            // Calculating the patch.
            Byte[] patch;
            Int32 sizeBytes = jsonPatch_.CreateJsonPatchBytes(
                PublicViewModel, 
                true, 
                CheckOption(SessionOptions.IncludeNamespaces), 
                out patch);

            // Sending the patch bytes to the client.
            ActiveWebSocket.Send(patch, sizeBytes, true);
        }

        /// <summary>
        /// Running asynchronously the given action on each active session on each owning scheduler.
        /// </summary>
        /// <param name="action">The action to be performed on each session.</param>
        public static void ForAll(Action<Session> action) {
            ForAll(UInt64.MaxValue, action);
        }

        /// <summary>
        /// Runs a given session for each task on current scheduler.
        /// </summary>
        static void ForEachSessionOnCurrentScheduler(UInt64 cargoId, Action<Session> action) {

            // Saving current session since we are going to set other.
            Session origCurrentSession = Session.Current;

            try {
                SchedulerSessions ss = 
                    GlobalSessions.AllGlobalSessions.GetSchedulerSessions(StarcounterEnvironment.CurrentSchedulerId);

                LinkedListNode<UInt32> used_session_index_node = ss.UsedSessionIndexes.First;
                while (used_session_index_node != null) {
                    LinkedListNode<UInt32> next_used_session_index_node = used_session_index_node.Next;

                    // Getting session instance.
                    ScSessionClass s = ss.GetAppsSessionIfAlive(used_session_index_node.Value);

                    // Checking if session is created at all.
                    if (s != null) {

                        // Checking if cargo ID is correct.
                        if ((cargoId == UInt64.MaxValue) || (cargoId == s.CargoId)) {

                            Session session = (Session)s.apps_session_int_;

                            // Setting new current session.
                            Session.Current = session;

                            // Running user delegate with session as parameter.
                            action(session);
                        }
                    }

                    // Getting next used session.
                    used_session_index_node = next_used_session_index_node;
                }
            } finally {
                // Restoring original current session.
                Session.Current = origCurrentSession;
            }

        }

        /// <summary>
        /// Running asynchronously the given action on each active session on each owning scheduler.
        /// </summary>
        /// <param name="action">The user procedure to be performed on each session.</param>
        /// <param name="cargoId">Cargo ID filter.</param>
        public static void ForAll(UInt64 cargoId, Action<Session> action) {

            String appName = StarcounterEnvironment.AppName;

            for (Byte i = 0; i < StarcounterEnvironment.SchedulerCount; i++) {
                
                Byte schedId = i;

                ScSessionClass.DbSession.RunAsync(
                    () => {

                        // We need to set application name when running on different schedulers.
                        StarcounterEnvironment.AppName = appName;

                        ForEachSessionOnCurrentScheduler(cargoId, action);
                    },
                    schedId);
            }
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
                _current = value;
            }
        }

        /// <summary>
        /// Gets or sets session data for one specific application. 
        /// </summary>
        public Json Data {
            get {
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
                        value.OnSessionSet();

                        if (publicViewModelIndex == -1)
                            publicViewModelIndex = stateIndex;
                    }
                    // Setting current session.
                    Current = this;
                }
            }
        }

        /// <summary>
        /// Specific saved user object ID.
        /// </summary>
        public UInt64 CargoId {
            get {
                return InternalSession.CargoId;
            }
            set {
                InternalSession.CargoId = value;
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
        public String SessionIdString {
            get { return InternalSession.ToAsciiString(); }
        }

        /// <summary>
        /// Returns True if session is being used now.
        /// </summary>
        /// <returns></returns>
        public bool IsBeingUsed() {
            return _isInUse;
        }

        /// <summary>
        /// Start using specific session.
        /// </summary>
        public void StartUsing() {
            _isInUse = true;
        }

        /// <summary>
        /// Stop using specific session.
        /// </summary>
        public void StopUsing() {
            _isInUse = false;
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
        internal Json PublicViewModel {
            get {
                if (publicViewModelIndex != -1)
                    return _stateList[publicViewModelIndex];
                return null;
            }
        }

        /// <summary>
        /// Start usage of given session.
        /// </summary>
        /// <param name="session"></param>
        internal static void Start(Session session) {
            Debug.Assert(_current == null);
            Debug.Assert(session != null);

            Session._current = session;
        }

        /// <summary>
        /// Finish usage of current session.
        /// </summary>
        internal static void End() {
            if (_current != null) {
                _current.DisposeUnreferencedTransactions();
                Session._current = null;
            }
        }

        /// <summary>
        /// Executes the specified action inside the Session
        /// </summary>
        /// <param name="session"></param>
        /// <param name="action"></param>
        internal static void Execute(Session session, Action action) {
            try {
                Start(session);
                action();
            } finally {
                End();
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

            if (InternalSession != null) {
                // NOTE: Preventing recursive destroy call.
                InternalSession.apps_session_int_ = null;
                InternalSession.Destroy();
                InternalSession = null;
            }

            // Checking if destroy callback is supplied.
            if (null != Destroyed) {
                Destroyed(this, null);
                Destroyed = null;
            }

            // Checking if there is an active WebSocket.
            if (ActiveWebSocket != null) {

                ActiveWebSocket.Session = null;
                ActiveWebSocket = null;
            }

            Session._current = null;
        }

        void IDisposable.Dispose() {
            Destroy();
        }
    }
}
