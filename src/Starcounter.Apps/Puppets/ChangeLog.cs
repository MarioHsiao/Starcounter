// ***********************************************************************
// <copyright file="ChangeLog.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using Starcounter.Templates;
using Starcounter.Apps;
using Starcounter.Advanced;

namespace Starcounter {
    /// <summary>
    /// Class keeping track of all outgoing changes to the json-tree. In the end
    /// of each patch-request, all changes will be converted to jsonpatches.
    /// </summary>
    internal class PuppetChangeLog : IEnumerable<Change> {
        /// <summary>
        /// 
        /// </summary>
        private List<Change> _changes;

        /// <summary>
        /// Initializes a new instance of the <see cref="Log" /> class.
        /// </summary>
        internal PuppetChangeLog() {
            _changes = new List<Change>();
        }

        /// <summary>
        /// 
        /// </summary>
        internal Puppet RootPuppet { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private static PuppetChangeLog Log {
            get {
                Session session = Session.Current;
                if (session != null)
                    return session.ChangeLog;
                return null;
            }
        }

        /// <summary>
        /// Adds an valueupdate change.
        /// </summary>
        /// <param name="obj">The Obj.</param>
        /// <param name="property">The property.</param>
        internal static void UpdateValue<T>(Puppet<T> obj, TValue property) where T : IBindable {
            PuppetChangeLog log = Log;
            if (log != null && obj.IsSentExternally) {
                if (!log._changes.Exists((match) => { return match.IsChangeOf(obj, property); })) {
                    log._changes.Add(Change.Update(obj, property));
                }
            }
        }

        /// <summary>
        /// Adds an valueupdate change.
        /// </summary>
        /// <param name="obj">The Obj.</param>
        /// <param name="valueTemplate">The value template.</param>
        internal static void UpdateValue<T>(Puppet<T> obj, Template valueTemplate) where T : IBindable {
            PuppetChangeLog log = Log;
            if (log != null && obj.IsSentExternally) {
                if (!log._changes.Exists((match) => { return match.IsChangeOf(obj, (Template)valueTemplate); })) {
                    log._changes.Add(Change.Update(obj, valueTemplate));
                }
            }
        }

        /// <summary>
        /// Adds an add item change.
        /// </summary>
        /// <param name="obj">The Obj.</param>
        /// <param name="list">The property of the list that the item was added to.</param>
        /// <param name="index">The index in the list where the item was added.</param>
        internal static void AddItemInList<T>(Obj<T> obj, TObjArr list, Int32 index) where T : IBindable {
            PuppetChangeLog log = Log;
            if (log != null)
                log._changes.Add(Change.Add(obj, list, index));
        }

        /// <summary>
        /// Adds an remove item change.
        /// </summary>
        /// <param name="obj">The app.</param>
        /// <param name="list">The property of the list the item was removed from.</param>
        /// <param name="index">The index in the list of the removed item.</param>
        internal static void RemoveItemInList<T>(Obj<T> obj, TObjArr list, Int32 index) where T : IBindable  {
            PuppetChangeLog log = Log;
            if (log != null && ((Puppet<T>)obj).IsSentExternally)
                log._changes.Add(Change.Remove(obj, list, index));
        }

        /// <summary>
        /// Clears all changes.
        /// </summary>
        internal void Clear() {
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
        internal Int32 Count { get { return _changes.Count; } }
    }

    /// <summary>
    /// A change of either a value, added or removed item in an json-tree.
    /// </summary>
    internal struct Change {
        internal const Int32 UNDEFINED = 0;
        internal const Int32 REMOVE = 1;
        internal const Int32 REPLACE = 2;
        internal const Int32 ADD = 3;

        internal static Change Null = new Change(UNDEFINED, null, null, -1);

        /// <summary>
        /// The type of change.
        /// </summary>
        internal readonly Int32 ChangeType;

        /// <summary>
        /// The app that was changed.
        /// </summary>
        internal readonly Obj App;

        /// <summary>
        /// The template of the property that was changed.
        /// </summary>
        internal readonly Template Template;

        /// <summary>
        /// The index if the change is add or remove. Will be
        /// -1 in other cases.
        /// </summary>
        internal readonly Int32 Index;

        /// <summary>
        /// Initializes a new instance of the <see cref="Change" /> struct.
        /// </summary>
        /// <param name="changeType">The change type.</param>
        /// <param name="app">The app that was changed.</param>
        /// <param name="template">The template of the property that was changed.</param>
        /// <param name="index">The index.</param>
        private Change(Int32 changeType, Obj app, Template template, Int32 index) {
            ChangeType = changeType;
            App = app;
            Template = template;
            Index = index;
        }

        /// <summary>
        /// Returns true if this change is a change of the same app and template.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="template">The template.</param>
        internal Boolean IsChangeOf(Obj app, Template template) {
            return (App == app && Template == template);
        }

        /// <summary>
        /// Creates and returns an instance of an Add change.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="list">The property of the list where an item was added.</param>
        /// <param name="index">The index in the list of the added item.</param>
        /// <returns></returns>
        internal static Change Add(Obj app, TObjArr list, Int32 index) {
            return new Change(Change.ADD, app, list, index);
        }

        /// <summary>
        /// Creates and returns an instance of a Remove change.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="list">The property of the list where an item was removed.</param>
        /// <param name="index">The index in the list of the removed item.</param>
        /// <returns></returns>
        internal static Change Remove(Obj app, TObjArr list, Int32 index) {
            return new Change(Change.REMOVE, app, list, index);
        }

        /// <summary>
        /// Creates and returns an instance of an Update change.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="property">The template of the property that was updated.</param>
        /// <returns></returns>
        internal static Change Update(Obj app, TValue property) {
            return new Change(Change.REPLACE, app, property, -1);
        }

        /// <summary>
        /// Creates and returns an instance of an Update change.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="valueTemplate">The IValueTemplate of the property that was updated.</param>
        /// <returns></returns>
        internal static Change Update(Obj app, Template valueTemplate) {
            return new Change(Change.REPLACE, app, (Template)valueTemplate, -1);
        }
    }
}
