﻿ ﻿// ***********************************************************************
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

namespace Starcounter {
    [Flags]
    public enum SessionOptions : int {
        Default = 0,
        IncludeSchema = 1,
        PatchVersioning = 2,
        StrictPatchRejection = 4,
//        DisableProtocolOT = 8,
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class Session : IAppsSession, IDisposable {
        private class DataAndCache {
            internal Json Data;
        }

        private class TransactionRef {
            internal int Refs;
            internal TransactionHandle Handle;
        }

        [ThreadStatic]
        private static Session _current;

        [ThreadStatic]
        private static Request _request;

        private Action<Session> _sessionDestroyUserDelegate;
        private bool _brandNew;
        private bool _isInUse;
        private Dictionary<string, int> _indexPerApplication;
        private List<Change> _changes; // The log of Json tree changes pertaining to the session data
        private List<DataAndCache> _stateList;
        private SessionOptions sessionOptions;

        /// <summary>
        /// Array of transactions that is managed by this session. All transaction here 
        /// will be cleaned up when either a json with transaction attached is removed 
        /// or when the session is cleaned up.
        /// </summary>
        private List<TransactionRef> transactions;

        /// <summary>
        /// Array of queued patches in order from the current clientversion.
        /// </summary>
        private List<Byte[]> patchQueue;

        /// <summary>
        /// Versioning for local and remote version of the viewmodel
        /// </summary>
        private long clientVersion;
        private long serverVersion;

        /// <summary>
        /// The clients current serverversion. Used to check if OT is needed.
        /// </summary>
        private long clientServerVersion;

        public Session() : this(SessionOptions.Default) {
        }

        public Session(SessionOptions options) {
            _brandNew = true;
            _changes = new List<Change>();
            _indexPerApplication = new Dictionary<string, int>();
            _stateList = new List<DataAndCache>();
            sessionOptions = options;
            clientServerVersion = serverVersion;
            transactions = new List<TransactionRef>();
            
            UInt32 errCode = 0;
            if (_request != null) {
                errCode = _request.GenerateNewSession(this);
            } else {
                // Simply generating new session.
                ScSessionStruct sss = new ScSessionStruct(true);
                errCode = GlobalSessions.AllGlobalSessions.CreateNewSession(ref sss, this);
            }

            if (errCode != 0)
                throw ErrorCode.ToException(errCode);
        }

        public SessionOptions Options {
            get { return sessionOptions; }
        }

        public bool CheckOption(SessionOptions option) {
            return (sessionOptions & option) == option;
        }

        internal void EnqueuePatch(byte[] patchArray, int index) {
            if (patchQueue == null)
                patchQueue = new List<byte[]>(8);

            for (int i = patchQueue.Count; i < index + 1; i++)
                patchQueue.Add(null);
            
            patchQueue[index] = patchArray;
        }

        internal byte[] GetNextEnqueuedPatch() {
            byte[] ret = null;
            if (patchQueue != null && patchQueue.Count > 0) {
                ret = patchQueue[0];
                patchQueue.RemoveAt(0);
            }
            return ret;
        }

        /// <summary>
        /// Gets the versionnumber for the remote version of the viewmodel.
        /// </summary>
        public long ClientVersion {
            get { return clientVersion; }
            internal set { clientVersion = value; }
        }

        /// <summary>
        /// Gets the versionnumber for the local version of the viewmodel.
        /// </summary>
        public long ServerVersion {
            get { return serverVersion; }
        }

        internal long ClientServerVersion {
            get { return clientServerVersion; }
            /*internal*/ set { clientServerVersion = value; }
        }

        /// <summary>
        /// Runs a task asynchronously on current scheduler.
        /// </summary>
        public void RunSync(Action action, Byte schedId = Byte.MaxValue) {
            InternalSession.RunSync(action, schedId);
        }

        /// <summary>
        /// Running the given action on each active session.
        /// </summary>
        /// <param name="action">The user procedure to be performed on each session.</param>
        public static void ForEach(Action<Session> action) {
            ForEach(UInt64.MaxValue, action);
        }

        /// <summary>
        /// Running the given action on each active session.
        /// </summary>
        /// <param name="action">The user procedure to be performed on each session.</param>
        /// <param name="cargoId">Cargo ID filter.</param>
        public static void ForEach(UInt64 cargoId, Action<Session> action) {

            for (Byte i = 0; i < StarcounterEnvironment.SchedulerCount; i++) {
                Byte schedId = i;

                ScSessionClass.DbSession.RunAsync(() => {
                    // Saving current session since we are going to set other.
                    Session origCurrentSession = Session.Current;

                    try {
                        SchedulerSessions ss = GlobalSessions.AllGlobalSessions.GetSchedulerSessions(schedId);

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

                }, schedId);
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
        /// Returns the original request for session.
        /// </summary>
        /// <value></value>
        public static Request InitialRequest {
            get { return _request; }
            set { _request = value; }
        }

        /// <summary>
        /// Getting internal session.
        /// </summary>
        public ScSessionClass InternalSession { get; set; }

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
                var dac = GetStateObject();
                if (dac != null)
                    return dac.Data;
                return null;
            }
            set {
                if (value != null && value.Parent != null)
                    throw ErrorCode.ToException(Error.SCERRSESSIONJSONNOTROOT);

                DataAndCache dac = AssureStateObject();
                dac.Data = value;
                if (value != null) {
                    if (value._Session != null)
                        value._Session.Data = null;

                    value._Session = this;
                    value.OnSessionSet();
                }

                // Setting current session.
                Current = this;

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

        // Last active WebSocket connection.
        public WebSocket ActiveWebsocket {
            get;
            internal set;
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
        /// Set user destroy callback.  
        /// </summary>
        /// <param name="destroy_user_delegate"></param>
        public void SetSessionDestroyCallback(Action<Session> userDestroyMethod) {
            _sessionDestroyUserDelegate = userDestroyMethod;
        }

        /// <summary>
        /// Gets destroy callback if it was supplied before.
        /// </summary>
        /// <returns></returns>
        public Action<Session> GetDestroyCallback() {
            return _sessionDestroyUserDelegate;
        }

        /// <summary>
        /// Return the stateobject for the first item in the statelist. Used internally when 
        /// application is unknown or only single app is used.
        /// </summary>
        /// <returns></returns>
        internal Json GetFirstData() {
            if (_stateList.Count > 0)
                return _stateList[0].Data;
            return null;
        }

        private DataAndCache GetStateObject() {
            int stateIndex;
            string appName;

            appName = StarcounterEnvironment.AppName;
            if (appName == null)
                return null;

            if (!_indexPerApplication.TryGetValue(appName, out stateIndex))
                return null;

            return _stateList[stateIndex];
        }

        private DataAndCache AssureStateObject() {
            DataAndCache dac;
            int stateIndex;
            string appName;

            appName = StarcounterEnvironment.AppName;
            if (appName == null) {
                // TODO: 
                // Should appname always be set and we treat this as an error?
                return null;
            }

            if (!_indexPerApplication.TryGetValue(appName, out stateIndex)) {
                dac = new DataAndCache();
                stateIndex = _stateList.Count;
                _stateList.Add(dac);
                _indexPerApplication.Add(appName, stateIndex);
            } else {
                dac = _stateList[stateIndex];
            }

            return dac;
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
                _current.ClearChangeLog();
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
        /// Adds an value update change.
        /// </summary>
        /// <param name="obj">The json containing the value.</param>
        /// <param name="property">The property to update</param>
        internal void UpdateValue(Json obj, TValue property) {
            _changes.Add(Change.Update(obj, property));
            property.Checkpoint(obj);
        }

        /// <summary>
        /// Adds a list of changes to the log
        /// </summary>
        /// <param name="toAdd"></param>
        internal void AddChange(Change change) {
            _changes.Add(change);
        }

        internal List<Change> GetChanges() {
            return _changes;
        }

        /// <summary>
        /// Clears all changes.
        /// </summary>
        internal void ClearChangeLog() {
            _changes.Clear();
        }

        /// <summary>
        /// Returns the number of changes in the log.
        /// </summary>
        /// <value></value>
        public Int32 ChangeCount { get { return _changes.Count; } }

        /// <summary>
        /// Logs all changes since the last JSON-Patch update. This method generates the log
        /// for the dirty flags and the added/removed logs of the JSON tree in the session data.
        /// </summary>
        public void GenerateChangeLog() {
            serverVersion++;
            if (_brandNew) {
                // TODO: 
                // might be array.
                foreach (var dac in _stateList) {
                    if (dac.Data != null) {
                        _changes.Add(Change.Add(dac.Data));
                        dac.Data.CheckpointChangeLog();
                    }
                }
                _brandNew = false;
            } else {
                foreach (var dac in _stateList) {
                    if (dac.Data != null) {
                        if (DatabaseHasBeenUpdatedInCurrentTask) {
                            dac.Data.LogValueChangesWithDatabase(this);
                        } else {
                            dac.Data.LogValueChangesWithoutDatabase(this);
                        }
                    }
                }
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

        /// <summary>
        /// 
        /// </summary>
        public void CheckpointChangeLog() {
            foreach (var dac in _stateList) {
                if (dac.Data != null)
                    dac.Data.CheckpointChangeLog();
            }
            _brandNew = false;
        }

        public bool BrandNew {
            get {
                return _brandNew;
            }
        }

        internal void CleanupOldVersionLogs() {
            var root = this.GetFirstData();
            if (root != null)
                root.CleanupOldVersionLogs(ClientServerVersion);
        }

        internal void RegisterTransaction(TransactionHandle handle) {
            TransactionRef tref = null;

            for (int i = 0; i < transactions.Count; i++) {
                tref = transactions[i];
                if (tref.Handle == handle) {
                    tref.Refs++;
                    break;
                }
            }

            if (tref == null) {
                // transaction not registered before. 
                tref = new TransactionRef();
                tref.Handle = StarcounterBase.TransactionManager.ClaimOwnership(handle);
                tref.Refs = 1;
            }
        }

        internal void DeregisterTransaction(TransactionHandle handle) {
            TransactionRef tref = null;

            for (int i = 0; i < transactions.Count; i++) {
                tref = transactions[i];
                if (tref.Handle == handle) {
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
            if (null != _sessionDestroyUserDelegate) {
                _sessionDestroyUserDelegate(this);
                _sessionDestroyUserDelegate = null;
            }

            Session._current = null;
        }

        void IDisposable.Dispose() {
            Destroy();
        }
    }
}
