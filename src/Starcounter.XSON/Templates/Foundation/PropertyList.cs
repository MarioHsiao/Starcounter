// ***********************************************************************
// <copyright file="PropertyList.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;

using Starcounter;
using System.Collections;
namespace Starcounter.Templates {

    /// <summary>
    /// The collection of properties (Templates) in an Obj template. I.e. for the template PersonTemplate, the
    /// list might contain two elements such as TString "FirstName" and TString "LastName".
    /// </summary>
    public class PropertyList : IEnumerable<Template>, IList<Template>
    {
        /// <summary>
        /// The Obj having the properties in this collection
        /// </summary>
        private TObj _Parent;

        /// <summary>
        /// The full name dictionary. These names can contain characters that are not valid for C# properties,
        /// such as the $ character often found in Javascript identifiers.
        /// </summary>
        private readonly Dictionary<string, Template> _NameLookup = new Dictionary<string, Template>();

        /// <summary>
        /// The property name dictionary contains property names that are legal to use in C#.
        /// </summary>
        private readonly Dictionary<string, Template> _PropertyNameLookup = new Dictionary<string, Template>();
        /// <summary>
        /// The _ list
        /// </summary>
        private List<Template> _List = new List<Template>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyList" /> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        internal PropertyList(TObj parent) {
            _Parent = parent;
        }

        /// <summary>
        /// Gets the <see cref="Template" /> with the specified id.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>Template.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Template this[int index] {
            get { return _List[index]; }
            set {
                throw new NotImplementedException();

//                _List[index] = (Template)value; 
			}
        }

        /// <summary>
        /// Gets the <see cref="Template" /> with the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>Template.</returns>
        public Template this[string id] {
            get {
                Template ret;
                if (!_PropertyNameLookup.TryGetValue(id, out ret))
                    return null;
                return ret;
            }
        }

        /// <summary>
        /// Gets the name of the template by.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Template.</returns>
        public Template GetTemplateByName(string name)
        {
            Template ret;
            _NameLookup.TryGetValue(name, out ret);
            return ret;
        }

        /// <summary>
        /// Get the template with the specified propertyname.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public Template GetTemplateByPropertyName(string propertyName) {
            Template ret;
            _PropertyNameLookup.TryGetValue(propertyName, out ret);
            return ret;
        }

        /// <summary>
        /// Childs the name is set.
        /// </summary>
        /// <param name="item">The item.</param>
        internal void ChildNameIsSet(Template item) {
            _NameLookup[item.Name] = (Template)item;
        }

        /// <summary>
        /// Childs the property name is set.
        /// </summary>
        /// <param name="item">The item.</param>
        internal void ChildPropertyNameIsSet(Template item) {
            _PropertyNameLookup[item.PropertyName] = (Template)item;
        }

        /// <summary>
        /// Copies to.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">Index of the array.</param>
        public void CopyTo(Template[] array, int arrayIndex) {
            _List.CopyTo((Template[])array, arrayIndex);
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <value>The count.</value>
        /// <returns>The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
        public int Count {
            get { return _List.Count; }
        }

        /// <summary>
        /// Indexes the of.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.Int32.</returns>
        int IList<Template>.IndexOf(Template item) {
            return _List.IndexOf((Template)item);
        }

        /// <summary>
        /// Inserts the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        void IList<Template>.Insert(int index, Template item) {
            throw new NotImplementedException();
//            _List.Insert(index, (Template)item);
        }

        /// <summary>
        /// Removes at.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        void IList<Template>.RemoveAt(int index) {
           throw new NotImplementedException();
//            _List.RemoveAt(index);
        }

        /// <summary>
        /// Gets the <see cref="Template" /> with the specified id.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>ITemplate.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        Template IList<Template>.this[int index] {
            get {
                return _List[index];
            }
            set {
                throw new NotImplementedException();
//               _List[index] = (Template)value;
            }
        }

        /// <summary>
        /// Replaces an existing template with the specified template
        /// using the same name.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <exception cref="System.Exception">This template is already used by an App. Cannot add more properties.</exception>
        public void Replace(Template item)
        {
            Template existing;
            Template newTemplate = (Template)item;
            PropertyList props;

            if (_Parent.Sealed)
                throw new Exception("This template is already used by an App. Cannot add more properties.");
            if (newTemplate.Parent != null)
                throw new Exception("Item already has a parent");

            existing = _NameLookup[newTemplate.Name];
            if (existing == null)
                throw new Exception("No item to replace found.");

            props = _Parent.Properties;
            props._NameLookup.Remove(newTemplate.Name);
            props.ChildNameIsSet(newTemplate);

            if (newTemplate.PropertyName != null)
            {
                props._PropertyNameLookup.Remove(existing.PropertyName);
                props.ChildPropertyNameIsSet(newTemplate);
            }
            newTemplate._Parent = _Parent;
            newTemplate.Index = existing.Index;
            _List[newTemplate.Index] = newTemplate;
            _Parent.OnPropertyAdded(newTemplate);
        }

        /// <summary>
        /// Adds the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <exception cref="System.Exception">This template is already used by an App. Cannot add more properties.</exception>
        public void Add(Template item) {
            Template t = (Template)item;
            if (_Parent.Sealed)
                throw new Exception("This template is already used by an App. Cannot add more properties.");
            if (t.Parent != null)
                throw new Exception("Item already has a parent");
            if (t.Name != null) {
                var props = (PropertyList)(_Parent.Properties);
                props.ChildNameIsSet(t);
            }
            if (item.PropertyName != null) {
                var props = (PropertyList)(_Parent.Properties);
                props.ChildPropertyNameIsSet(t);
            }
            t._Parent = this._Parent;
            t.Index = this.Count; // Last one in list (added below)
            _List.Add(t);
            _Parent.OnPropertyAdded(t);
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        void ICollection<Template>.Clear() {
                       throw new NotImplementedException();
//            _List.Clear();
        }

        /// <summary>
        /// Determines whether [contains] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if [contains] [the specified item]; otherwise, <c>false</c>.</returns>
        bool ICollection<Template>.Contains(Template item) {
            return _List.Contains((Template)item);
        }

        /// <summary>
        /// Copies to.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">Index of the array.</param>
        void ICollection<Template>.CopyTo(Template[] array, int arrayIndex) {
            _List.CopyTo((Template[])array, arrayIndex);
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <value>The count.</value>
        /// <returns>The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
        int ICollection<Template>.Count {
            get { return _List.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </summary>
        /// <value><c>true</c> if this instance is read only; otherwise, <c>false</c>.</value>
        /// <returns>true if the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only; otherwise, false.</returns>
        bool ICollection<Template>.IsReadOnly {
            get { return false; }
        }

        /// <summary>
        /// Removes the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        bool ICollection<Template>.Remove(Template item) {
                       throw new NotImplementedException();
//            return _List.Remove((Template)item);
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>IEnumerator{ITemplate}.</returns>
        IEnumerator<Template> IEnumerable<Template>.GetEnumerator() {
            return (IEnumerator<Template>)_List.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return _List.GetEnumerator();
        }

        /// <summary>
        /// Adds the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        void ICollection<Template>.Add(Template item) {
            throw new NotImplementedException();
        }
    }
}
