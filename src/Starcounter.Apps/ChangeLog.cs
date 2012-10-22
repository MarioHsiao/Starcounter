// ***********************************************************************
// <copyright file="ChangeLog.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using Starcounter.Templates;
using Starcounter.Templates.Interfaces;

namespace Starcounter
{
    /// <summary>
    /// Class ChangeLog
    /// </summary>
    internal class ChangeLog : IEnumerable<Change>
    {
        // TODO:
        // The session structure should be moved to App and 
        // the session should hold the changelog instance. We dont 
        // want several thread specific states (The log here and the current
        // session)
        /// <summary>
        /// The log
        /// </summary>
        [ThreadStatic]
        internal static ChangeLog Log;

        /// <summary>
        /// The _changes
        /// </summary>
        private List<Change> _changes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeLog" /> class.
        /// </summary>
        internal ChangeLog()
        {
            _changes = new List<Change>();
        }

        /// <summary>
        /// Begins the request.
        /// </summary>
        /// <param name="log">The log.</param>
        internal static void BeginRequest(ChangeLog log)
        {
            Log = log;
        }

        /// <summary>
        /// Ends the request.
        /// </summary>
        internal static void EndRequest()
        {
            Log.Clear();
            Log = null;
        }

        /// <summary>
        /// Updates the value.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="property">The property.</param>
        internal static void UpdateValue(App app, Property property)
        {
            if (!app.IsSerialized) return;
            if (!Log._changes.Exists((match) => { return match.IsChangeOf(app, property); }))
            {
                Log._changes.Add(Change.Update(app, property));
            }
        }

        /// <summary>
        /// Updates the value.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="valueTemplate">The value template.</param>
        internal static void UpdateValue(App app, IValueTemplate valueTemplate)
        {
            if (!app.IsSerialized) return;
            if (!Log._changes.Exists((match) => { return match.IsChangeOf(app, (Template)valueTemplate); }))
            {
                Log._changes.Add(Change.Update(app, valueTemplate));
            }
        }

        /// <summary>
        /// Adds the item in list.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="list">The list.</param>
        /// <param name="index">The index.</param>
        internal static void AddItemInList(App app, ListingProperty list, Int32 index)
        {
            if (!app.IsSerialized) return;
            Log._changes.Add(Change.Add(app, list, index));
        }

        /// <summary>
        /// Removes the item in list.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="list">The list.</param>
        /// <param name="index">The index.</param>
        internal static void RemoveItemInList(App app, ListingProperty list, Int32 index)
        {
            if (!app.IsSerialized) return;
            Log._changes.Add(Change.Remove(app, list, index));
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        internal void Clear()
        {
            _changes.Clear();
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>IEnumerator{Change}.</returns>
        public IEnumerator<Change> GetEnumerator()
        {
            return _changes.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _changes.GetEnumerator();
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count.</value>
        internal Int32 Count { get { return _changes.Count; } }
    }

    /// <summary>
    /// Struct Change
    /// </summary>
    internal struct Change
    {
        /// <summary>
        /// The UNDEFINED
        /// </summary>
        public const Int32 UNDEFINED = 0;
        /// <summary>
        /// The REMOVE
        /// </summary>
        public const Int32 REMOVE = 1;
        /// <summary>
        /// The REPLACE
        /// </summary>
        public const Int32 REPLACE = 2;
        /// <summary>
        /// The ADD
        /// </summary>
        public const Int32 ADD = 3;

        /// <summary>
        /// The null
        /// </summary>
        internal static Change Null = new Change(UNDEFINED, null, null, -1);

        /// <summary>
        /// The change type
        /// </summary>
        internal readonly Int32 ChangeType;
        /// <summary>
        /// The app
        /// </summary>
        internal readonly App App;
        /// <summary>
        /// The template
        /// </summary>
        internal readonly Template Template;
        /// <summary>
        /// The index
        /// </summary>
        internal readonly Int32 Index;

        /// <summary>
        /// Initializes a new instance of the <see cref="Change" /> struct.
        /// </summary>
        /// <param name="changeType">Type of the change.</param>
        /// <param name="app">The app.</param>
        /// <param name="template">The template.</param>
        /// <param name="index">The index.</param>
        private Change(Int32 changeType, App app, Template template, Int32 index)
        {
            ChangeType = changeType;
            App = app;
            Template = template;
            Index = index;
        }

        /// <summary>
        /// Determines whether [is change of] [the specified app].
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="template">The template.</param>
        internal Boolean IsChangeOf(App app, Template template)
        {
            return (App == app && Template == template);
        }

        /// <summary>
        /// Adds the specified app.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="list">The list.</param>
        /// <param name="index">The index.</param>
        /// <returns>Change.</returns>
        internal static Change Add(App app, ListingProperty list, Int32 index)
        {
            return new Change(Change.ADD, app, list, index);
        }

        /// <summary>
        /// Removes the specified app.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="list">The list.</param>
        /// <param name="index">The index.</param>
        /// <returns>Change.</returns>
        internal static Change Remove(App app, ListingProperty list, Int32 index)
        {
            return new Change(Change.REMOVE, app, list, index);
        }

        /// <summary>
        /// Updates the specified app.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="property">The property.</param>
        /// <returns>Change.</returns>
        internal static Change Update(App app, Property property)
        {
            return new Change(Change.REPLACE, app, property, -1);
        }

        /// <summary>
        /// Updates the specified app.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="valueTemplate">The value template.</param>
        /// <returns>Change.</returns>
        internal static Change Update(App app, IValueTemplate valueTemplate)
        {
            return new Change(Change.REPLACE, app, (Template)valueTemplate, -1);
        }

        //public override bool Equals(object obj)
        //{
        //    return Change.Equals(this, (Change)obj);
        //}

        //public bool Equals(Change change)
        //{
        //    return Change.Equals(this, change);
        //}

        //public static bool Equals(Change c1, Change c2)
        //{
        //    return Template.Equals(c1.Template, c2.Template);
        //}

        //public override int GetHashCode()
        //{
        //    return Template.GetHashCode();
        //}
    }
}
