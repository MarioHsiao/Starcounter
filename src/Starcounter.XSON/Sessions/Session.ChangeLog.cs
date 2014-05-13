using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Internal;
using Starcounter.Internal.XSON;
using Starcounter.Templates;

namespace Starcounter {
    partial class Session : IEnumerable<Change> {
        private bool _brandNew = true;

        /// <summary>
        /// The log of Json tree changes pertaining to the session data
        /// </summary>
        private List<Change> _changes;

        /// <summary>
        /// Initializes a new instance of the <see cref="Session" /> class.
        /// </summary>
        public Session() {
            _changes = new List<Change>();
            _indexPerApplication = new Dictionary<string, int>();
            _stateList = new List<DataAndCache>();

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

        /// <summary>
        /// Adds an value update change.
        /// </summary>
        /// <param name="obj">The json containing the value.</param>
        /// <param name="property">The property to update</param>
        internal void UpdateValue(Json obj, TValue property) {
            _changes.Add(Change.Update(obj, property));
        }

        /// <summary>
        /// Adds a value update for an array.
        /// </summary>
        /// <param name="obj">The json containing the value.</param>
        /// <param name="property">The property to update</param>
        /// <param name="index">The index in the array that should be updated.</param>
        internal void UpdateValue(Json obj, TObjArr property, int index) {
            _changes.Add(Change.Update(obj, property, index));
        }

        /// <summary>
        /// Adds a list of changes to the log
        /// </summary>
        /// <param name="toAdd"></param>
        internal void AddRangeOfChanges(List<Change> toAdd) {
            _changes.AddRange(toAdd);
        }

        internal List<Change> GetChanges() {
            return _changes;
        }

        /// <summary>
        /// Clears all changes.
        /// </summary>
        public void Clear() {
            _changes.Clear();
        }

        /// <summary>
        /// Returns a typed enumerator of all changes.
        /// </summary>
        /// <returns>IEnumerator{Change}.</returns>
        public IEnumerator<Change> GetEnumerator() {
            return _changes.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator of all changes
        /// </summary>
        /// <returns><see cref="T:System.Collections.IEnumerator" /></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return _changes.GetEnumerator();
        }

        /// <summary>
        /// Returns the number of changes in the log.
        /// </summary>
        /// <value></value>
        public Int32 Count { get { return _changes.Count; } }

        /// <summary>
        /// Logs all changes since the last JSON-Patch update. This method generates the log
        /// for the dirty flags and the added/removed logs of the JSON tree in the session data.
        /// </summary>
        public void GenerateChangeLog() {
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
    }
}
