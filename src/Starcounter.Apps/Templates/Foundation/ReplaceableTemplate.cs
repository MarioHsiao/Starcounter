// ***********************************************************************
// <copyright file="ReplaceableTemplate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

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
        /// <summary>
        /// The _values
        /// </summary>
        private Dictionary<String, Object> _values;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplaceableTemplate" /> class.
        /// </summary>
        public ReplaceableTemplate()
        {
            _values = new Dictionary<String, Object>();
            ConvertTo = null;
        }

        /// <summary>
        /// Gets a value indicating whether this instance has instance value on client.
        /// </summary>
        /// <value><c>true</c> if this instance has instance value on client; otherwise, <c>false</c>.</value>
        public override bool HasInstanceValueOnClient
        {
            get { return false; }
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void SetValue(String name, object value)
        {
            if (_values.ContainsKey(name))
            {
                _values[name] = value;
                return;
            }
            _values.Add(name, value);
        }

        /// <summary>
        /// Gets or sets the convert to.
        /// </summary>
        /// <value>The convert to.</value>
        public TValue ConvertTo
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the values.
        /// </summary>
        /// <value>The values.</value>
        public IEnumerable<KeyValuePair<String, Object>> Values
        {
            get { return _values; }
        }
    }
}
