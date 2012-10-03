
using System;
using System.Collections.Generic;

namespace Starcounter.Templates
{
    /// <summary>
    /// A temporary template used to hold values until the correct
    /// template can be created. If for example metadata for a field
    /// is specified before the actual field in the json file, a temporary
    /// ReplaceableTemplate will be created, and later the values will be
    /// copied to the right template.
    /// </summary>
    public class ReplaceableTemplate : Template
    {
        private Dictionary<String, Object> _values;
        
        public ReplaceableTemplate()
        {
            _values = new Dictionary<String, Object>();
            ConvertTo = null;
        }

        public override bool HasInstanceValueOnClient
        {
            get { return false; }
        }

        public void SetValue(String name, object value)
        {
            if (_values.ContainsKey(name))
            {
                _values[name] = value;
                return;
            }
            _values.Add(name, value);
        }

        public Property ConvertTo
        {
            get;
            set;
        }

        public IEnumerable<KeyValuePair<String, Object>> Values
        {
            get { return _values; }
        }
    }
}
