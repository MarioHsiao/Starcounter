using System;
using System.Collections.Generic;
using System.Diagnostics;
using Starcounter.Internal.XSON;
using Starcounter.Templates;

namespace Starcounter.XSON {
    public class ChangeLog {
        private List<Change> changes;
        private bool brandNew;
        private Json employer;
        private ViewModelVersion version;

        public ChangeLog(Json employer) {
            this.changes = new List<Change>();
            this.employer = employer;
            this.brandNew = true;
        }

        public ChangeLog(Json employer, ViewModelVersion version) : this(employer) {
            this.version = version;
        }

        [Conditional("DEBUG")]
        private void VerifyChange(Json json, TValue property) {

        }

        [Conditional("DEBUG")]
        private void VerifyChange(Change change) {

        }

        /// <summary>
        /// Adds an value update change.
        /// </summary>
        /// <param name="obj">The json containing the value.</param>
        /// <param name="property">The property to update</param>
        internal void UpdateValue(Json obj, TValue property) {
            VerifyChange(obj, property);
            changes.Add(Change.Update(obj, property));
        }

        /// <summary>
        /// Adds a list of changes to the log
        /// </summary>
        /// <param name="toAdd"></param>
        internal void Add(Change change) {
            VerifyChange(change);
            changes.Add(change);
        }

        /// <summary>
        /// Clears all changes.
        /// </summary>
        internal void Clear() {
            changes.Clear();
        }

        public ViewModelVersion Version {
            get { return version; }
        }

        /// <summary>
        /// Logs all changes since the last JSON-Patch update. This method generates the log
        /// for the dirty flags and the added/removed logs of the JSON tree in the session data.
        /// </summary>
        public Change[] Generate(bool flushLog) {
            if (version != null)
                version.LocalVersion++;

            if (brandNew) {
                changes.Add(Change.Add(employer));
                brandNew = false;
            } else {
                employer.LogValueChangesWithDatabase(this, true);
            }

            // TODO:
            // Temorary workaround until jsonpatch generation supports move operation.
            SplitMoves(changes);

            var arr = changes.ToArray();

            if (flushLog) {
                changes.Clear();
            }
            return arr;
        }

        private static void SplitMoves(List<Change> changes) {
            Change current;
            Change toAdd;

            if (changes == null)
                return;

            for (int i = 0; i < changes.Count; i++) {
                current = changes[i];
                if (current.ChangeType == Change.MOVE) {
                    toAdd = Change.Remove(current.Parent, (TObjArr)current.Property, current.FromIndex, current.Item);
                    changes[i] = toAdd;
                    toAdd = Change.Add(current.Parent, (TObjArr)current.Property, current.Index, current.Item);
                    changes.Insert(i + 1, toAdd);
                    i++;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Checkpoint() {
            employer.CheckpointChangeLog();
            brandNew = false;
        }

        public bool BrandNew {
            get { return brandNew; }
        }

        internal void CleanupOldVersionLogs() {
            if (version != null)
                employer.CleanupOldVersionLogs(version, version.RemoteLocalVersion);
        }
    }

    public class ViewModelVersion {
        private List<Byte[]> patchQueue;
        private string localPropertyName;
        private string remotePropertyName;
        
        private long localVersion;
        private long remoteVersion;

        private long remoteLocalVersion;

        public ViewModelVersion() {
            localPropertyName = "_ver#s";
            remotePropertyName = "_ver#c$";
        }

        public ViewModelVersion(string localVersionPropertyName, string remoteVersionPropertyName) {
            localPropertyName = localVersionPropertyName;
            remotePropertyName = remoteVersionPropertyName;
        }

        public string LocalVersionPropertyName { 
            get { return localPropertyName; } 
        }

        public string RemoteVersionPropertyName { 
            get { return remotePropertyName; } 
        }

        public long LocalVersion {
            get { return localVersion; }
            set { localVersion = value; }
        }

        public long RemoteVersion {
            get { return remoteVersion; }
            set { remoteVersion = value; }
        }

        public long RemoteLocalVersion {
            get { return remoteLocalVersion; }
            set { remoteLocalVersion = value; }
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
    }

    internal struct ArrayVersionLog {
        internal long Version;
        internal List<Change> Changes;

        internal ArrayVersionLog(long version, List<Change> changes) {
            Version = version;
            Changes = changes;
        }
    }
}
