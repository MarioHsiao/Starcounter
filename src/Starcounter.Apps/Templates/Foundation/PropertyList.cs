

using System;
using System.Collections.Generic;
using Starcounter.Templates.Interfaces;

#if CLIENT
using Starcounter.Client;
namespace Starcounter.Client.Template {
#else
using Starcounter;
using System.Collections;
namespace Starcounter.Templates {
#endif

    public class PropertyList
#if IAPP
        : IPropertyTemplates, IEnumerable<Template>
#endif
    {
        private AppTemplate _Parent;
        private readonly Dictionary<string, Template> _NameLookup = new Dictionary<string, Template>();
        private readonly Dictionary<string, Template> _PropertyNameLookup = new Dictionary<string, Template>();
        private List<Template> _List = new List<Template>();

        internal PropertyList(AppTemplate parent) {
            _Parent = parent;
        }

        public Template this[int index] {
            get { return _List[index]; }
            set {
                throw new NotImplementedException();

//                _List[index] = (Template)value; 
			}
        }

        public Template this[string id] {
            get {
                Template ret;
                if (!_PropertyNameLookup.TryGetValue(id, out ret))
                    return null;
                return ret;
            }
        }

        public Template GetTemplateByName(string name)
        {
            Template ret;
            _NameLookup.TryGetValue(name, out ret);
            return ret;
        }

        internal void ChildNameIsSet(ITemplate item) {
            _NameLookup[item.Name] = (Template)item;
        }

        internal void ChildPropertyNameIsSet(ITemplate item) {
            _PropertyNameLookup[item.PropertyName] = (Template)item;
        }

        public void CopyTo(ITemplate[] array, int arrayIndex) {
            _List.CopyTo((Template[])array, arrayIndex);
        }

        public int Count {
            get { return _List.Count; }
        }

        int IList<ITemplate>.IndexOf(ITemplate item) {
            return _List.IndexOf((Template)item);
        }

        void IList<ITemplate>.Insert(int index, ITemplate item) {
            throw new NotImplementedException();
//            _List.Insert(index, (Template)item);
        }

        void IList<ITemplate>.RemoveAt(int index) {
           throw new NotImplementedException();
//            _List.RemoveAt(index);
        }

        ITemplate IList<ITemplate>.this[int index] {
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
        /// <param name="item"></param>
        public void Replace(ITemplate item)
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
        }

        public void Add(ITemplate item) {
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
        }

        void ICollection<ITemplate>.Clear() {
                       throw new NotImplementedException();
//            _List.Clear();
        }

        bool ICollection<ITemplate>.Contains(ITemplate item) {
            return _List.Contains((Template)item);
        }

        void ICollection<ITemplate>.CopyTo(ITemplate[] array, int arrayIndex) {
            _List.CopyTo((Template[])array, arrayIndex);
        }

        int ICollection<ITemplate>.Count {
            get { return _List.Count; }
        }

        bool ICollection<ITemplate>.IsReadOnly {
            get { return false; }
        }

        bool ICollection<ITemplate>.Remove(ITemplate item) {
                       throw new NotImplementedException();
//            return _List.Remove((Template)item);
        }

        IEnumerator<ITemplate> IEnumerable<ITemplate>.GetEnumerator() {
            return (IEnumerator<ITemplate>)_List.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return _List.GetEnumerator();
        }

        IEnumerator<Template> IEnumerable<Template>.GetEnumerator() {
            return (IEnumerator<Template>)(_List.GetEnumerator());
        }

        ITemplate IPropertyTemplates.this[string id] {
            get { throw new NotImplementedException(); }
        }

        void ICollection<ITemplate>.Add(ITemplate item) {
            throw new NotImplementedException();
        }
    }
}
