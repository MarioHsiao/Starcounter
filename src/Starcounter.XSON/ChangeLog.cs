// ***********************************************************************
// <copyright file="ChangeLog.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using Starcounter.Templates;
using Starcounter.Advanced;

namespace Starcounter {
    /// <summary>
    /// Class keeping track of all outgoing changes to the json-tree. In the end
    /// of each patch-request, all changes will be converted to jsonpatches.
    /// </summary>
    public class ChangeLog : IEnumerable<Change> {
        private List<Change> changes;
        
//        [ThreadStatic]
        private static ChangeLog log;

        /// <summary>
        /// Initializes a new instance of the <see cref="Log" /> class.
        /// </summary>
        public ChangeLog() {
            changes = new List<Change>();
        }

        /// <summary>
        /// 
        /// </summary>
        public static ChangeLog CurrentOnThread {
            get { return log; } set { log = value; }
        }

        /// <summary>
        /// Adds an valueupdate change.
        /// </summary>
        /// <param name="obj">The Obj.</param>
        /// <param name="property">The property.</param>
        public static void UpdateValue(Obj obj, TValue property) {
            if (obj.LogChanges && log != null) {
                if (!log.changes.Exists((match) => { return match.IsChangeOf(obj, property); })) {
                    log.changes.Add(Change.Update(obj, property));
                }
            }
        }

        /// <summary>
        /// Adds an add item change.
        /// </summary>
        /// <param name="obj">The Obj.</param>
        /// <param name="list">The property of the list that the item was added to.</param>
        /// <param name="index">The index in the list where the item was added.</param>
        public static void AddItemInList(Obj obj, TObjArr list, Int32 index) {
            if (obj.LogChanges && log != null)
                log.changes.Add(Change.Add(obj, list, index));
        }

        /// <summary>
        /// Adds an remove item change.
        /// </summary>
        /// <param name="obj">The app.</param>
        /// <param name="list">The property of the list the item was removed from.</param>
        /// <param name="index">The index in the list of the removed item.</param>
        public static void RemoveItemInList(Obj obj, TObjArr list, Int32 index) {
            if (obj.LogChanges && log != null)
                log.changes.Add(Change.Remove(obj, list, index));
        }

        /// <summary>
        /// Clears all changes.
        /// </summary>
        public void Clear() {
            changes.Clear();
        }

        /// <summary>
        /// Returns a typed enumerator of all changes.
        /// </summary>
        /// <returns>IEnumerator{Change}.</returns>
        public IEnumerator<Change> GetEnumerator() {
            return changes.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator of all changes
        /// </summary>
        /// <returns><see cref="T:System.Collections.IEnumerator" /></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return changes.GetEnumerator();
        }

        /// <summary>
        /// Returns the number of changes in the log.
        /// </summary>
        /// <value></value>
        public Int32 Count { get { return changes.Count; } }
    }

    /// <summary>
    /// A change of either a value, added or removed item in an json-tree.
    /// </summary>
    public struct Change {
        public const Int32 UNDEFINED = 0;
        public const Int32 REMOVE = 1;
        public const Int32 REPLACE = 2;
        public const Int32 ADD = 3;

        internal static Change Null = new Change(UNDEFINED, null, null, -1);

        /// <summary>
        /// The type of change.
        /// </summary>
        public readonly Int32 ChangeType;

        /// <summary>
        /// The object that was changed.
        /// </summary>
        public readonly Obj Obj;

        /// <summary>
        /// The template of the property that was changed.
        /// </summary>
        public readonly TValue Template;

        /// <summary>
        /// The index if the change is add or remove. Will be
        /// -1 in other cases.
        /// </summary>
        public readonly Int32 Index;

        /// <summary>
        /// Initializes a new instance of the <see cref="Change" /> struct.
        /// </summary>
        /// <param name="changeType">The change type.</param>
        /// <param name="app">The app that was changed.</param>
        /// <param name="template">The template of the property that was changed.</param>
        /// <param name="index">The index.</param>
        private Change(Int32 changeType, Obj obj, TValue template, Int32 index) {
            ChangeType = changeType;
            Obj = obj;
            Template = template;
            Index = index;
        }

        /// <summary>
        /// Returns true if this change is a change of the same app and template.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="template">The template.</param>
        internal Boolean IsChangeOf(Obj obj, TValue template) {
            return (Obj == obj && Template == template);
        }

        /// <summary>
        /// Creates and returns an instance of an Add change.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="list">The property of the list where an item was added.</param>
        /// <param name="index">The index in the list of the added item.</param>
        /// <returns></returns>
        internal static Change Add(Obj obj, TObjArr list, Int32 index) {
            return new Change(Change.ADD, obj, list, index);
        }

        /// <summary>
        /// Creates and returns an instance of a Remove change.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="list">The property of the list where an item was removed.</param>
        /// <param name="index">The index in the list of the removed item.</param>
        /// <returns></returns>
        internal static Change Remove(Obj obj, TObjArr list, Int32 index) {
            return new Change(Change.REMOVE, obj, list, index);
        }

        /// <summary>
        /// Creates and returns an instance of an Update change.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="property">The template of the property that was updated.</param>
        /// <returns></returns>
        internal static Change Update(Obj obj, TValue property) {
            return new Change(Change.REPLACE, obj, property, -1);
        }
    }
}
